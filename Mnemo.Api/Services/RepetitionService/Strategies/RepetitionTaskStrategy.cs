using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Data.Entities;
using Mnemo.Services.RepetitionService.Factories;
using Mnemo.Services.RepetitionService.Providers.TaskTypeProviders;
using System.Diagnostics;

namespace Mnemo.Services.RepetitionService.Strategies
{
    public abstract class RepetitionTaskStrategy : IRepetitionTaskStrategy
    {
        private readonly IOptions<RepetitionOptions> _options;
        private readonly IOptions<SM2Options> _sm2;
        private readonly ILogger<IRepetitionTaskStrategy> _logger;
        private readonly RepetitionTaskFactory _factory;
        private readonly ITaskTypeProvider _typeProvider;

        public RepetitionTaskStrategy(
            IOptions<RepetitionOptions> options,
            IOptions<SM2Options> sm2,
            ILogger<IRepetitionTaskStrategy> logger,
            RepetitionTaskFactory factory,
            ITaskTypeProvider typeProvider)
        {
            _options = options;
            _sm2 = sm2;
            _logger = logger;
            _factory = factory;
            _typeProvider = typeProvider;
        }


        public async Task<List<RepetitionTask>> GetTasksAsync(int userId)
        {
            _logger.LogInformation("Starting generate {Count} tasks for user (UserId:{UserId})...", take, userId);

            var stopwatch = Stopwatch.StartNew();
            int take = _options.Value.RepetitionTaskCount;

            var query 
                = await GetTargetEntriesQuery(userId, take);

            var targetEntries = await query
                .Include(e => e.RepetitionState)
                .Include(e => e.Vocabulary)
                .ToListAsync();


            if (targetEntries.Any())
            {
                _logger.LogDebug("Retrieved {Count} entries for user (UserId:{UserId})", targetEntries.Count, userId);
            }
            else
            {
                _logger.LogWarning("No entries found for user (UserId:{UserId})", userId);
                return new List<RepetitionTask>();
            }

            var tasks = new List<RepetitionTask>();
            var index = 0;
            var excludeIds = targetEntries.Select(e => e.Id).ToArray();

            foreach (var entry in targetEntries)
            {
                double easeFactor = entry.RepetitionState?.EasinessFactor ?? _sm2.Value.MinEF;

                (Type taskType, bool isForward) = _typeProvider.GetType(easeFactor);
                var task = await _factory.CreateByTypeAsync(isForward, taskType, entry, excludeIds);

                task.OrderIndex = index;
                index++;

                tasks.Add(task);
            }

            stopwatch.Stop();
            _logger.LogInformation("Successfully generated {TaskCount} tasks in {DurationMs} ms for user (UserId:{UserId})!", tasks.Count, stopwatch.ElapsedMilliseconds, userId);

            return tasks;
        }


        protected abstract Task<IQueryable<VocabularyEntry>> GetTargetEntriesQuery(int userId, int take);
    }
}
