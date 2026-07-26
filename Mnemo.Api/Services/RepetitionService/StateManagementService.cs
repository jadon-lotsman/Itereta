using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Contracts.Repetition;
using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared.Extensions;
using System.Diagnostics;

namespace Mnemo.Services.RepetitionService
{
    public class StateManagementService
    {
        private readonly IOptions<SM2Options> _sm2;
        private readonly IOptions<RepetitionOptions> _options;
        private readonly ILogger<StateManagementService> _logger;

        private readonly AppDbContext _context;
        private readonly StateQueries _stateQueries;


        public StateManagementService(
            IOptions<SM2Options> sm2,
            IOptions<RepetitionOptions> options,
            ILogger<StateManagementService> logger,
            AppDbContext context,
            StateQueries stateQueries)
        {
            _sm2 = sm2;
            _options = options;
            _logger = logger;
            _context = context;
            _stateQueries = stateQueries;
        }



        public async Task<List<RepetitionDateResponse>> GetRepetitionScheduleAsync(int userId)
        {
            var dateToday = DateOnly.FromDateTime(DateTime.UtcNow);
            int daysUntilNext = DateTime.UtcNow.DaysUntilNext(DayOfWeek.Monday);

            var states = _stateQueries
                .GetByUserIdQuery(userId)
                .Include(s => s.VocabularyEntry);

            var grouped = states
                .Where(s => s.NextRepetitionAt <= dateToday.AddDays(7 + daysUntilNext))
                .GroupBy(s => s.NextRepetitionAt)
                .Select(d => new RepetitionDateResponse
                {
                    Date = d.Key,
                    IsImportantDay = d.Count() > _options.Value.RepetitionTaskCount,
                    VocabularyForeigns = d.Select(s => s.VocabularyEntry.Foreign).ToArray()
                })
                .OrderBy(d => d.Date)
                .ToList();

            return grouped;
        }


        public async Task RecordQualityRepetitionStateAsync(int userId, Dictionary<int, double> entryIdToQuality)
        {
            _logger.LogInformation("Attempting to record quality for {Count} entries from user (UserId:{UserId})", entryIdToQuality.Count, userId);

            if (entryIdToQuality == null || !entryIdToQuality.Any())
            {
                _logger.LogWarning("Entry-to-quality dictionary is empty from user (UserId:{UserId})", userId);
            }


            var stateDict = await _stateQueries.GetDictByEntryIdsAsync(userId, entryIdToQuality.Keys);

            if (stateDict.Count == 0)
            {
                _logger.LogWarning("States pull is empty from user (UserId:{UserId})", userId);
            }


            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var errors = new List<string>();

            if (entryIdToQuality.Count != stateDict.Count)
                errors.Add($"Not all states was found (Found:{stateDict.Count}, Total:{entryIdToQuality.Count})");

            foreach (var state in stateDict.Values)
            {
                if (!entryIdToQuality.TryGetValue(state.VocabularyEntryId, out double quality))
                    continue;

                if (quality < _sm2.Value.MinQuality || quality > _sm2.Value.MaxQuality)
                {
                    errors.Add($"Quality {Math.Round(quality, 1)} out of range {_sm2.Value.MinQuality}...{_sm2.Value.MaxQuality}");
                    continue;
                }


                bool isPassing = _sm2.Value.IsPassingQuality(quality);

                if (state.IsDueAt(today))
                {
                    (int interval, double ef) = ComputeNextState(
                        state.EasinessFactor,
                        state.RepetitionInterval,
                        state.RepetitionCounter,
                        quality);

                    state.SetState(interval, ef, isPassing, today);
                }
                else
                {
                    double efBonus = quality * 0.01d;

                    state.SetBonus(efBonus, isPassing, today);
                }
            }

            _logger.LogDebug("Quality recording entries is ending from user (UserId:{UserId}). Saving changes...", userId);
            await _context.SaveChangesAsync();

            if (errors.Any())
            {
                var joined = string.Join("; ", errors);
                _logger.LogWarning("Success, but quality recording failed for {Count} entries: {Errors}", errors.Count, joined);
            }


            _logger.LogInformation("Successfully recorded qualities for {Count} entries from user (UserId:{UserId})", stateDict.Count, userId);
        }

        public (int newInterval, double newEasinessFactor) ComputeNextState(double easinessFactor, int interval, int repetitionCounter, double quality)
        {
            double nextEasinessFactor;
            int nextInterval;

            if (!_sm2.Value.IsPassingQuality(quality))
            {
                nextInterval = _sm2.Value.FirstIntervalDays;
            }
            else
            {
                nextInterval = repetitionCounter switch
                {
                    0 => _sm2.Value.FirstIntervalDays,
                    1 => _sm2.Value.SecondIntervalDays,
                    _ => (int)Math.Ceiling((interval > 0 ? interval : 1) * easinessFactor)
                };

                nextInterval = Math.Clamp(nextInterval, _sm2.Value.MinInterval, _sm2.Value.MaxInterval);
            }

            nextEasinessFactor = easinessFactor + (0.1 - (_sm2.Value.MaxQuality - quality) * (0.08 + (_sm2.Value.MaxQuality - quality) * 0.02));
            nextEasinessFactor = Math.Clamp(nextEasinessFactor, _sm2.Value.MinEF, _sm2.Value.MaxEF);

            return (nextInterval, nextEasinessFactor);
        }


        public async Task BalanceRepetitionStateAsync(int userId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var groups = _stateQueries
                .GetByUserIdQuery(userId)
                .GroupBy(s => s.NextRepetitionAt)
                .ToDictionary(g => g.Key, g => g.ToList());


            int maxPerDay = _options.Value.RepetitionTaskCount;

            int totalMoved = 0;
            int totalStates = groups.Values.Sum(list => list.Count);
            int overfilledBefore, overfilledAfter;

            _logger.LogInformation("Starting balance {Count} states from user (UserId:{UserId})", totalStates, userId);

            Stopwatch stopwatch = Stopwatch.StartNew();
            overfilledBefore = groups
                .Count(g => g.Value.Count() > maxPerDay);

            bool changed;
            do
            {
                changed = false;

                var overfilled = groups
                    .Where(g => g.Value.Count() > maxPerDay)
                    .OrderByDescending(g => g.Value.Count())
                    .ThenBy(g => g.Key)
                    .ToList();

                if (!overfilled.Any())
                {
                    _logger.LogDebug("No overfilled days found for user (UserId:{UserId})", userId);
                    break;
                }


                foreach (var day in overfilled)
                {
                    var moveTo = day.Value
                        .OrderBy(s => s.EasinessFactor)
                        .Take(day.Value.Count - maxPerDay)
                        .ToList();

                    var succesfullyMoved = new List<RepetitionState>();

                    foreach (var state in moveTo)
                    {
                        DateOnly? nearest = FindNearestFreeDay(today, state.NextRepetitionAt, groups, maxPerDay);

                        if (nearest.HasValue)
                        {
                            state.NextRepetitionAt = nearest.Value;

                            if (!groups.ContainsKey(nearest.Value))
                                groups[nearest.Value] = new List<RepetitionState>();

                            groups[nearest.Value].Add(state);
                            succesfullyMoved.Add(state);
                            changed = true;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var toRemove = new HashSet<RepetitionState>(succesfullyMoved);
                    day.Value.RemoveAll(toRemove.Contains);

                    totalMoved += toRemove.Count;
                }
            } while (changed);

            overfilledAfter = groups
                .Count(g => g.Value.Count() > maxPerDay);
            stopwatch.Stop();

            _logger.LogDebug("Balance finished with: TotalMoved:{TotalMoved}, OverfilledDays: {before} -> {after}. It took {ElapsedMs} ms for user (UserId:{UserId}). Saving...",
                 totalMoved, overfilledBefore, overfilledAfter, stopwatch.ElapsedMilliseconds, userId);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully balanced for user (UserId:{UserId})", userId);
        }

        private DateOnly? FindNearestFreeDay(DateOnly today, DateOnly original, Dictionary<DateOnly, List<RepetitionState>> groups, int maxCount, int maxOffset = 3)
        {
            for (int offset = 1; offset <= maxOffset; offset++)
            {
                var candidates = new[] { original.AddDays(offset), original.AddDays(-offset) };
                foreach (var date in candidates)
                {
                    if (date <= today) continue;
                    if (!groups.TryGetValue(date, out var list) || list.Count < maxCount)
                        return date;
                }
            }

            return null;
        }
    }
}
