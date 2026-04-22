using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnemo.Common;
using Mnemo.Contracts.Dtos.Repetition;
using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Services.Queries;

namespace Mnemo.Services
{
    public class RepetitionSessionService
    {
        private AppDbContext _context;
        private AccountQueries _accountQueries;
        private SessionQueries _sessionQueries;
        private VocabularyQueries _vocabularyQueries;

        private RepetitionStateService _stateService;
        private static Random _random = new Random();


        public RepetitionSessionService(AppDbContext context, AccountQueries accountQueries, SessionQueries sessionQueries, VocabularyQueries vocabularyQueries, RepetitionStateService stateService)
        {
            _context = context;
            _accountQueries = accountQueries;
            _sessionQueries = sessionQueries;
            _vocabularyQueries = vocabularyQueries;
            _stateService = stateService;
        }



        public async Task<RequestResult<RepetitionSession>> GetRepetitionSessionStatusAsync(int userId)
        {
            var session = await _sessionQueries.GetByUserIdAsync(userId);

            if (session == null)    return RequestResult<RepetitionSession>.Failure("SESSION_NOT_FOUND");
            if (session.IsFinished) return RequestResult<RepetitionSession>.Failure("SESSION_WAS_FINISHED");
            else return RequestResult<RepetitionSession>.Failure("SESSION_IN_PROCESS");
        }



        public async Task<RequestResult<RepetitionSession>> StartRepetitionSessionAsync(int userId)
        {
            var user = await _accountQueries.GetByIdAsync(userId);

            if (user == null)
                return RequestResult<RepetitionSession>.Failure("USER_NOT_FOUND");


            if (user.RepetitionSession != null && user.RepetitionSession.InProccess)
                return RequestResult<RepetitionSession>.Failure("SESSION_NOT_FINISHED");

            else if (user.RepetitionSession != null && user.RepetitionSession.IsFinished)
                _context.RepetitionSessions.Remove(user.RepetitionSession);


            await _stateService.RefreshRepetitionStatesAsync(userId);


            var targetEntries = _vocabularyQueries.GetRandomByUserId(userId);
            var tasks = targetEntries.Select(e => new RepetitionTask(e, _random.Next(2) == 0)).ToList();

            var session = new RepetitionSession(userId, tasks);

            await _context.RepetitionSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            return RequestResult<RepetitionSession>.Success(session);
        }

        public async Task<RequestResult<RepetitionSessionResultDto>> FinishRepetitionSessionAsync(int userId)
        {
            var session = await _sessionQueries.GetByUserIdAsync(userId);

            if (session == null)
                return RequestResult<RepetitionSessionResultDto>.Failure("SESSION_NOT_FOUND");

            if (session.Tasks == null || session.Tasks.Count == 0)
                return RequestResult<RepetitionSessionResultDto>.Failure("SESSION_HAS_NO_TASKS");


            if (!session.IsFinished)
            {
                session.FinishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }


            var entriesIds = session.Tasks.Select(t => t.BaseVocabularyEntryId).ToList();
            var entriesDict = await _vocabularyQueries.GetDictByIdsAsync(userId, entriesIds);

            int missedCount = 0;
            var failedEntries = new List<VocabularyEntry>();

            foreach (var task in session.Tasks)
            {
                if (entriesDict.TryGetValue(task.BaseVocabularyEntryId, out var entry) && entry != null)
                {
                    double similarity = GetMaxAnswerSimilarity(task, entry);
                    double quality = SM2Helper.ComputeQuality(task.RepetitionSession.AverageActionTime, task.ActionTimeSpan, task.ActionCounter, similarity);

                    var state = await _stateService.UpdateRepetitionStateAsync(userId, entry.Id, quality, shouldIncrementCounter: true);

                    if (SM2Helper.IsPassingQuality(quality))
                        failedEntries.Add(entry);
                }
                else
                {
                    missedCount++;
                }
            }


            int totalCount = session.Tasks.Count - missedCount;
            int correctCount = totalCount - failedEntries.Count;

            var result = new RepetitionSessionResultDto(
                correctCount,
                totalCount,
                Mapper.MapToDto(failedEntries),
                session.StartedAt,
                session.FinishedAt!.Value);

            return RequestResult<RepetitionSessionResultDto>.Success(result);
        }

        public async Task<RequestResult<RepetitionTask>> SubmitRepetitionTaskAnswerAsync(int userId, int taskId, string answer)
        {
            var task = await _sessionQueries.GetRepetitionTaskByIdAsync(userId, taskId);

            if (task == null)
                return RequestResult<RepetitionTask>.Failure("TASK_NOT_FOUND");

            if (task.RepetitionSession.IsFinished)
                return RequestResult<RepetitionTask>.Failure("SESSION_WAS_FINISHED");


            var currentTime     = DateTime.UtcNow;
            var lastActionTime  = task.RepetitionSession.LastActionAt;

            task.ActionCounter++;
            task.UserAnswer             = answer;
            task.ActionTimeSpan         = currentTime - lastActionTime;
            task.RepetitionSession.LastActionAt = currentTime;

            await _context.SaveChangesAsync();

            return RequestResult<RepetitionTask>.Success(task);
        }


        private double GetMaxAnswerSimilarity(RepetitionTask task, VocabularyEntry entry)
        {
            string userAnswer = task.UserAnswer;

            if (task.IsForwardQuestion)
                return entry.Translations.Max(userAnswer.ComputeLevenshteinSimilarity);
            else
                return userAnswer.ComputeLevenshteinSimilarity(entry.Foreign);
        }
    }
}
