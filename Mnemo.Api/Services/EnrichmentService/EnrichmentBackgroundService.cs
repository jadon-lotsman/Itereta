using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Data;
using Mnemo.Services.EnrichmentService.ExternalDictionaries;
using Mnemo.Shared.Enums;
using System.Diagnostics;

namespace Mnemo.Services.EnrichmentService
{
    public class EnrichmentBackgroundService : BackgroundService
    {
        private readonly IOptions<EnrichmentOptions> _options;
        private readonly ILogger<EnrichmentBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public EnrichmentBackgroundService(
            IOptions<EnrichmentOptions> options,
            ILogger<EnrichmentBackgroundService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _options = options;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Value.IsEnabled)
            {
                _logger.LogWarning("Enrichment service is disabled by configuration");
                return;
            }

            var batchDelay = TimeSpan.FromSeconds(_options.Value.BatchDelaySeconds);
            _logger.LogInformation("Enrichment service started. Batch delay is {Delay} sec", batchDelay.TotalSeconds);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ResetStuckProcessingAsync(stoppingToken);
                    await ProcessBatchAsync(stoppingToken);
                    await Task.Delay(batchDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stopping gracefully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in enrichment service");
                throw;
            }
        }

        private async Task ResetStuckProcessingAsync(CancellationToken stoppingToken)
        {
            const int pending = (int)EnrichmentStatus.Pending;
            const int processing = (int)EnrichmentStatus.Processing;
            const int failed = (int)EnrichmentStatus.Failed;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var stuckTimeout = _options.Value.StuckProcessingTimeoutMinutes;

            try
            {
                DateTime threshold = DateTime.UtcNow.AddMinutes(-stuckTimeout);

                int processingResetCount = await context.Database.ExecuteSqlRawAsync(
                    $"UPDATE \"VocabularyEntries\" SET \"EnrichmentStatus\" = {pending} " +
                    $"WHERE \"EnrichmentStatus\" = {processing} AND \"LastEnrichmentAt\" < {{0}}", threshold);

                int failedResetCount = await context.Database.ExecuteSqlRawAsync(
                    $"UPDATE \"VocabularyEntries\" SET \"EnrichmentStatus\" = {pending} " +
                    $"WHERE \"EnrichmentStatus\" = {failed}");

                if (processingResetCount > 0 || failedResetCount > 0)
                    _logger.LogInformation("Reset stuck entries (Processing:{Processing}, Failed:{Failed})", processingResetCount, failedResetCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset stuck entries");
            }
        }

        private async Task ProcessBatchAsync(CancellationToken stoppingToken)
        {
            List<long>? capturedIds = null;
            string? idsForCapture = null;

            int completedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;

            const int processing = (int)EnrichmentStatus.Processing;
            const int pending = (int)EnrichmentStatus.Pending;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var externalDictionary = scope.ServiceProvider.GetRequiredService<IExternalDictionary>();

            var batchSize = _options.Value.BatchSize;
            var requestDelay = TimeSpan.FromMilliseconds(_options.Value.RequestDelayMilliseconds);

            try
            {
                List<int> pendingIds = await context.VocabularyEntries
                    .Where(e => e.EnrichmentStatus == EnrichmentStatus.Pending)
                    .OrderBy(e => e.Id)
                    .Select(e => e.Id)
                    .Take(batchSize)
                    .ToListAsync(stoppingToken);

                if (!pendingIds.Any())
                    return;


                DateTime now = DateTime.UtcNow;

                idsForCapture = string.Join(",", pendingIds);
                capturedIds = await context.Database
                    .SqlQueryRaw<long>(
                        $"UPDATE \"VocabularyEntries\" SET \"EnrichmentStatus\" = {processing}, \"LastEnrichmentAt\" = {{0}} " +
                        $"WHERE \"Id\" IN ({idsForCapture}) AND \"EnrichmentStatus\" = {pending} " +
                        $"RETURNING \"Id\"", now)
                    .ToListAsync(stoppingToken);

                if (capturedIds.Count < pendingIds.Count)
                    _logger.LogWarning("Captured only {CapturedCount} out of {PendingCount} entries. Others were already taken or status changed.", capturedIds.Count, pendingIds.Count);


                if (!capturedIds.Any())
                    return;


                var entries = await context.VocabularyEntries
                    .Where(e => capturedIds.Contains(e.Id))
                    .ToListAsync(stoppingToken);


                foreach (var entry in entries)
                {
                    var stopwatch = Stopwatch.StartNew();

                    var result = await externalDictionary.GetEnrichAsync(entry.Foreign, entry.PartOfSpeech);
                    stopwatch.Stop();

                    _logger.LogInformation("External call for entry (EntryId:{EntryId}) took {ElapsedMs} ms", entry.Id, stopwatch.ElapsedMilliseconds);

                    if (!result.IsSuccess)
                    {
                        failedCount++;
                        entry.EnrichmentStatus = EnrichmentStatus.Failed;
                    }
                    else
                    {
                        var enrichResponse = result.Value;

                        if (enrichResponse == null)
                        {
                            notFoundCount++;
                        }
                        else
                        {
                            entry.SetMeta(enrichResponse);
                            completedCount++;
                        }

                        entry.EnrichmentStatus = EnrichmentStatus.Completed;
                    }


                    if (entries.IndexOf(entry) < entries.Count - 1)
                        await Task.Delay(requestDelay, stoppingToken);
                }

                _logger.LogInformation("Batch is ending (Completed:{Completed}, Failed:{Failed}, NotFound:{NotFound}). Saving changes...", completedCount, failedCount, notFoundCount);

                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully processed batch (BatchSize:{Count})", entries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in batch processing");
            }
        }
    }
}
