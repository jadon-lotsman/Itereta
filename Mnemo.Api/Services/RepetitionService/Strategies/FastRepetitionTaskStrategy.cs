using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService.Factories;
using Mnemo.Services.RepetitionService.Providers.TaskTypeProviders;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.RepetitionService.Strategies
{
    public class FastRepetitionTaskStrategy : RepetitionTaskStrategy
    {
        private readonly VocabularyEntryQueries _entryQueries;

        public FastRepetitionTaskStrategy(
            IOptions<RepetitionOptions> options,
            IOptions<SM2Options> sm2,
            ILogger<FastRepetitionTaskStrategy> logger,
            RepetitionTaskFactory factory,
            ITaskTypeProvider typeProvider,
            VocabularyEntryQueries entryQueries) : base(options, sm2, logger, factory, typeProvider)
        {
            _entryQueries = entryQueries;
        }


        protected override async Task<IQueryable<VocabularyEntry>> GetEntriesQuery(int userId, Guid vocabGuid, int take)
        {
            var priorityEntriesQuery = _entryQueries
                .GetVocabEntriesByGuidSecuredQuery(userId, vocabGuid)
                .Include(e => e.RepetitionState)
                .Include(e => e.Vocabulary)
                .NotDueEntries()
                .NotRepeatedTodayEntries()
                .GetRandomEntries(take);

            var mixQuery = priorityEntriesQuery;

            if (priorityEntriesQuery.Count() < take)
            {
                var existingIds = priorityEntriesQuery.Select(e => e.Id).ToArray();

                var randomEntries = _entryQueries
                    .GetVocabEntriesByGuidSecuredQuery(userId, vocabGuid)
                    .Include(e => e.RepetitionState)
                    .NotDueEntries()
                    .GetRandomEntries(take - existingIds.Length, existingIds);

                mixQuery = mixQuery.Concat(randomEntries);
            }


            return mixQuery;
        }
    }
}
