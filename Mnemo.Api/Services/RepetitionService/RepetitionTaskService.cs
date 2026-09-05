using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Contracts.Repetition.Results;
using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService.Strategies;
using Mnemo.Shared;
using Mnemo.Shared.Enums;

namespace Mnemo.Services.RepetitionService
{
    public class RepetitionTaskService
    {
        private readonly ILogger<RepetitionTaskService> _logger;
        private readonly IOptions<SM2Options> _sm2;
        private readonly AppDbContext _context;

        private readonly AccountQueries _accountQueries;
        private readonly VocabularyQueries _vocabularyQueries;

        private readonly TaskQueries _taskQueries;
        private readonly StateManagementService _stateService;
        private readonly QualityCalculationService _qualityService;

        private readonly FastRepetitionTaskStrategy _fastStrategy;
        private readonly PlannedRepetitionTaskStrategy _plannedStrategy;


        public RepetitionTaskService(
            ILogger<RepetitionTaskService> logger,
            IOptions<SM2Options> sm2,
            AppDbContext context,
            AccountQueries accountQueries,
            VocabularyQueries vocabularyQueries,
            TaskQueries taskQueries,
            StateManagementService stateService,
            QualityCalculationService qualityService,
            FastRepetitionTaskStrategy fastStrategy,
            PlannedRepetitionTaskStrategy plannedStrategy)
        {
            _logger = logger;
            _sm2 = sm2;

            _context = context;

            _accountQueries = accountQueries;
            _vocabularyQueries = vocabularyQueries;

            _taskQueries = taskQueries;
            _stateService = stateService;
            _qualityService = qualityService;

            _fastStrategy = fastStrategy;
            _plannedStrategy = plannedStrategy;
        }



        public async Task<RequestResult<bool>> ExistsRepetitionAsync(int userId)
        {
            if (!await _accountQueries.ExistsByIdAsync(userId))
                return RequestResult<bool>.Failure(ErrorCode.UserNotFound);

            bool inProcess = await _taskQueries.ExistsByUserId(userId);

            if (inProcess) return RequestResult<bool>.Success(true);
            else return RequestResult<bool>.Success(false);
        }



        public async Task<RequestResult<List<RepetitionTask>>> StartRepetitionAsync(int userId, string mode)
        {
            _logger.LogInformation("Attempting to start repetition for user (UserId:{UserId})", userId);

            if (!await _accountQueries.ExistsByIdAsync(userId))
            {
                _logger.LogWarning("User (UserId:{UserId}) not found", userId);
                return RequestResult<List<RepetitionTask>>.Failure(ErrorCode.UserNotFound);
            }

            if (await _taskQueries.ExistsByUserId(userId))
            {
                _logger.LogWarning("Repetition already exists for user (UserId:{UserId})", userId);
                return RequestResult<List<RepetitionTask>>.Success(await _taskQueries.GetByUserIdQuery(userId).ToListAsync());
            }


            IRepetitionTaskStrategy? strategy = mode switch
            {
                "fast" => _fastStrategy,
                "planned" => _plannedStrategy,
                _ => null
            };

            if (strategy == null)
            {
                _logger.LogWarning("Request from user (UserId:{UserId}) contains an unknown repetition mode '{mode}'", userId, mode);
                return RequestResult<List<RepetitionTask>>.Failure(ErrorCode.InvalidData);
            }

            var tasks = await strategy.GetTasksAsync(userId);
            if (!tasks.Any())
            {
                _logger.LogWarning("Repetition strategy returns an empty result for user (UserId:{UserId})", userId);
                return RequestResult<List<RepetitionTask>>.Failure(ErrorCode.TaskGenerationFailed);
            }

            await _context.RepetitionTasks.AddRangeAsync(tasks);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully started repetition for user (UserId:{UserId})", userId);

            return RequestResult<List<RepetitionTask>>.Success(tasks);
        }

        public async Task<RequestResult<RepetitionResultResponse>> FinishRepetitionAsync(int userId)
        {
            _logger.LogInformation("Attempting to finish repetition for user (UserId:{UserId})", userId);

            if (!await _taskQueries.ExistsByUserId(userId))
            {
                _logger.LogWarning("No repetition tasks found for user (UserId:{UserId})", userId);
                return RequestResult<RepetitionResultResponse>.Failure(ErrorCode.RepetitionNotFound);
            }

            var tasks = await _taskQueries.GetByUserIdQuery(userId).ToListAsync();
            _logger.LogDebug("Retrieved {TaskCount} tasks for user (UserId:{UserId})", tasks.Count, userId);


            var statsByType = tasks
                .GroupBy(t => t.GetType())
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    TotalSeconds = g.Sum(t => t.ElapsedTime.TotalSeconds),
                    AvgSeconds = g.Average(t => t.ElapsedTime.TotalSeconds)
                })
                .ToList();

            foreach (var stat in statsByType)
            {
                _logger.LogDebug(
                    "Time groups for tasks from user (UserId:{UserId, 2}): TaskType:{TaskType, -30} [{Total, -4:F1} sec|Count:{Count, 2}|~AverageTime:{Avg:F1} sec]",
                    userId,
                    stat.Type.Name,
                    stat.TotalSeconds,
                    stat.Count,
                    stat.AvgSeconds
                );
            }

            var averageByType = statsByType
                .ToDictionary(
                    s => s.Type,
                    s => s.AvgSeconds
                );


            var entryIdToQuality = new Dictionary<int, double>();
            var taskResults = new List<TaskResultResponse>();

            int totalTasks = tasks.Count;
            int correctCount = 0;
            int unansweredCount = 0;

            foreach (var task in tasks)
            {
                var taskType = task.GetType();
                var avgForType = averageByType.TryGetValue(taskType, out var avg)
                    ? TimeSpan.FromSeconds(avg)
                    : TimeSpan.Zero;

                var taskResult = _qualityService.ComputeTaskQuality(avgForType, task.ElapsedTime, task.ActionCounter, task.GetSimilarity(), task.GetDifficultFactor());
                bool isCorrect = _sm2.Value.IsPassingQuality(taskResult.Quality);

                if (isCorrect)
                    correctCount++;

                if (!task.HasUserAnswer)
                    unansweredCount++;

                if (!entryIdToQuality.ContainsKey(task.VocabularyEntryId))
                    entryIdToQuality.Add(task.VocabularyEntryId, taskResult.Quality);

                taskResults.Add(new TaskResultResponse()
                {
                    Id = task.Id,
                    Quality = taskResult.Quality,
                    IsCorrect = isCorrect,
                    CorrectAnswer = task.GetCorrect()
                });

                _logger.LogDebug("Log for task with OrderIndex:{OrderIndex, 2} from user (UserId:{UserId, 2}) TaskType:{TaskType, -30} " +
                    "[{D:F2}|{A:F1}|{S:F1}|{R:F1}|{Quality:F2}]{IsGiven}",
                    task.OrderIndex + 1,
                    userId,
                    task.GetType().Name,
                    taskResult.Difficulty,
                    taskResult.Accuracy,
                    taskResult.Stability,
                    taskResult.Reaction,
                    taskResult.Quality,
                    task.HasUserAnswer ? "" : " (answer not given)"
                );
            }

            _logger.LogDebug("Removing {count} tasks...", totalTasks);

            _context.RepetitionTasks.RemoveRange(tasks);
            await _context.SaveChangesAsync();


            int percent = totalTasks > 0 ? (int)(double)(correctCount * 100d / totalTasks) : 0;

            var response = new RepetitionResultResponse()
            {
                Correct = correctCount,
                Total = totalTasks,
                Percent = percent,
                TotalTimeMilliseconds = (int)tasks.Sum(t => t.ElapsedTime.TotalMilliseconds),
                TaskResults = taskResults.ToArray()
            };

            _logger.LogInformation(
                "Tasks was successfully removed. User (UserId:{UserId}) completed {TotalTasks} tasks: " +
                "(Correct:{CorrectCount}, Unanswered:{Unanswered}, Percent:{CorrectPercent:F1}%)",
                userId,
                totalTasks,
                correctCount,
                unansweredCount,
                percent
            );


            await _stateService.RecordQualityRepetitionStateAsync(userId, entryIdToQuality);
            await _stateService.BalanceRepetitionStateAsync(userId);

            return RequestResult<RepetitionResultResponse>.Success(response);
        }

        public async Task<RequestResult<RepetitionTask>> SubmitRepetitionTaskAnswerAsync(int userId, int taskId, string answer, TimeSpan elapsedTime)
        {
            _logger.LogInformation("Attempting to submit task (TaskId:{TaskId}) answer for user (UserId:{UserId})", taskId, userId);

            var task = await _taskQueries.GetTaskByIdAsync(userId, taskId);
            if (task == null)
            {
                _logger.LogWarning("Task (TaskId:{TaskId}) not found for user (UserId:{UserId})", taskId, userId);
                return RequestResult<RepetitionTask>.Failure(ErrorCode.TaskNotFound);
            }


            task.SubmitAnswer(answer, elapsedTime);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully submitted answer (TaskId:{TaskId}, OrderIndex:{Order}) for user (UserId:{UserId})", taskId, task.OrderIndex, userId);

            return RequestResult<RepetitionTask>.Success(task);
        }
    }
}
