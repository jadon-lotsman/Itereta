using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Itereta.Common;
using Itereta.Contracts.Dtos.Iteration;
using Itereta.Data;
using Itereta.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itereta.Services
{
    public class VocabularyIterationService
    {
        private AppDbContext _context;
        private AccountService _accountService;
        private VocabularyManagementService _vocabularyService;

        private static Random _random = new Random();


        public VocabularyIterationService(AppDbContext context, AccountService userService, VocabularyManagementService vocabularyService)
        {
            _context = context;
            _accountService = userService;
            _vocabularyService = vocabularyService;
        }


        public async Task<RequestResult<Iteration>> GetIterationStatusAsync(int userId)
        {
            var iteration = await GetIterationAsync(userId);
            if (iteration == null) return RequestResult<Iteration>.Failure("ITERATION_NOT_FOUND");
            if (iteration.IsFinished) return RequestResult<Iteration>.Failure("ITERATION_WAS_FINISHED");
            else return RequestResult<Iteration>.Failure("ITERATION_IN_PROCESS");
        }

        public async Task<Iteration?> GetIterationAsync(int userId)
        {
            return await _context.Iterations
                .Include(i => i.Iterettes)
                .FirstOrDefaultAsync(e => e.User.Id == userId);
        }

        public async Task<List<Iterette>> GetAllIterettesAsync(int userId)
        {
            return await _context.Iterettes
                .Where(i => i.Iteration.UserId == userId)
                .ToListAsync();
        }

        public async Task<Iterette?> GetIteretteByIdAsync(int userId, int iteretteId)
        {
            return await _context.Iterettes
                .Include(i => i.Iteration)
                .FirstOrDefaultAsync(s => s.Id == iteretteId && s.Iteration.UserId == userId);
        }

        public async Task<RepetitionState?> GetRepetitionStateByEntryIdAsync(int userId, int repetitionId)
        {
            return await _context.RepetitionStates
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Id == repetitionId);
        }


        public async Task<RequestResult<Iteration>> StartIterationAsync(int userId)
        {
            var user = await _accountService.GetByIdAsync(userId);

            if (user == null)
                return RequestResult<Iteration>.Failure("USER_NOT_FOUND");


            var iterationById = await GetIterationAsync(userId);

            if (iterationById != null && !iterationById.IsFinished)
                return RequestResult<Iteration>.Failure("ITERATION_NOT_FINISHED");

            else if (iterationById != null && iterationById.IsFinished)
                _context.Iterations.Remove(iterationById);


            var entriesWithoutState = await _vocabularyService.GetAllEntriesWithoutStateAsync(userId);

            List<RepetitionState> states = entriesWithoutState
                .Select(e => new RepetitionState(user, e)).ToList();

            await _context.RepetitionStates.AddRangeAsync(states);


            var targetEntries = _vocabularyService.GetListOfRandomEntries(userId);
            List<Iterette> iterettes = targetEntries
                .Select(e => new Iterette(e, _random.Next(2) == 0)).ToList();


            var iteration = new Iteration(user, iterettes);

            await _context.Iterations.AddAsync(iteration);
            await _context.SaveChangesAsync();

            return RequestResult<Iteration>.Success(iteration);
        }

        public async Task<RequestResult<IterationResultDto>> FinishIterationAsync(int userId)
        {
            var iteration = await GetIterationAsync(userId);

            if (iteration == null)
                return RequestResult<IterationResultDto>.Failure("ITERATION_NOT_FOUND");

            if (iteration.Iterettes == null)
                return RequestResult<IterationResultDto>.Failure("ITERATION_HAS_NO_ITERETTES");


            if (!iteration.IsFinished)
            {
                iteration.FinishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }


            var entriesIds = iteration.Iterettes
                .Select(i => i.BaseVocabularyEntryId).ToList();
            var entriesDict = await _vocabularyService.GetEntriesDictByIdsAsync(userId, entriesIds);

            int missedCount = 0;
            var failedEntries = new List<VocabularyEntry>();

            foreach (var iterette in iteration.Iterettes)
            {
                if (entriesDict.TryGetValue(iterette.BaseVocabularyEntryId, out var baseEntry) && baseEntry != null)
                {
                    var state = await AutoAssessmentAsync(userId, iterette, baseEntry);

                    //if (state.Value.IterationCounter!=0)
                    //    failedEntries.Add(baseEntry);
                }
                else
                {
                    missedCount++;
                }
            }


            int totalCount = iteration.Iterettes.Count - missedCount;
            int correctCount = totalCount - failedEntries.Count;

            var result = new IterationResultDto(
                correctCount,
                totalCount,
                Mapper.MapToDto(failedEntries),
                iteration.StartedAt,
                iteration.FinishedAt!.Value);

            return RequestResult<IterationResultDto>.Success(result);
        }

        public async Task<RequestResult<Iterette>> SubmitIteretteAnswerAsync(int userId, int iteretteId, string answer)
        {
            var iterette = await GetIteretteByIdAsync(userId, iteretteId);

            if (iterette == null)
                return RequestResult<Iterette>.Failure("ITERETTE_NOT_FOUND");

            if (iterette.Iteration.IsFinished)
                return RequestResult<Iterette>.Failure("ITERATION_WAS_FINISHED");


            var currentTime     = DateTime.UtcNow;
            var lastActionTime  = iterette.Iteration.LastActionAt;

            iterette.ActionCounter++;
            iterette.UserAnswer             = answer;
            iterette.ActionTimeSpan         = currentTime - lastActionTime;
            iterette.Iteration.LastActionAt = currentTime;

            await _context.SaveChangesAsync();

            return RequestResult<Iterette>.Success(iterette);
        }

        public async Task<RequestResult<RepetitionState>> SelfAssessmentAsync(int userId, int entryId, double quality)
        {
            var state = await GetRepetitionStateByEntryIdAsync(userId, entryId);

            if (state == null)
                return RequestResult<RepetitionState>.Failure("REPETITION_STATE_NOT_FOUND");

            if (!state.CanSelfAssess)
                return RequestResult<RepetitionState>.Failure("REPETITION_STATE_ASSESS_NOT_ALLOWED");

            // Self features
            state.CanSelfAssess    = false;

            (int interval, double easinessFactor)
                = SM2Helper.NextIntervalAndEf(state.EasinessFactor, state.IterationInterval, state.IterationCounter, quality);

            state.IterationInterval = interval;
            state.EasinessFactor    = easinessFactor;

            await _context.SaveChangesAsync();

            return RequestResult<RepetitionState>.Success(state);
        }

        private async Task<RequestResult<RepetitionState>> AutoAssessmentAsync(int userId, Iterette iterette, VocabularyEntry entry)
        {
            double similarity = GetMaxAnswerSimilarity(iterette, entry);
            double quality = SM2Helper.ComputeQuality(iterette.Iteration.AverageActionTime, iterette.ActionTimeSpan, iterette.ActionCounter, similarity);

            var state = await GetRepetitionStateByEntryIdAsync(userId, entry.Id);

            if (state == null)
                return RequestResult<RepetitionState>.Failure("REPETITION_STATE_NOT_FOUND");

            // Auto features
            state.IterationCounter  = SM2Helper.IsPassingQuality(quality) ? state.IterationCounter + 1 : 0;
            state.CanSelfAssess     = SM2Helper.IsPassingQuality(quality);
            state.LastIterationAt   = DateTime.UtcNow;

            (int interval, double easinessFactor) 
                = SM2Helper.NextIntervalAndEf(state.EasinessFactor, state.IterationInterval, state.IterationCounter, quality);

            state.IterationInterval = interval;
            state.EasinessFactor    = easinessFactor;

            await _context.SaveChangesAsync();

            return RequestResult<RepetitionState>.Success(state);
        }

        private double GetMaxAnswerSimilarity(Iterette iterette, VocabularyEntry entry)
        {
            string userAnswer = iterette.UserAnswer;

            if (iterette.IsForwardQuestion)
                return entry.Translations.Max(userAnswer.ComputeLevenshteinSimilarity);
            else
                return userAnswer.ComputeLevenshteinSimilarity(entry.Foreign);
        }
    }
}
