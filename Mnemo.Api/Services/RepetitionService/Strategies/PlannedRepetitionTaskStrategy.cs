using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService.Factories;
using Mnemo.Services.RepetitionService.Providers.TaskTypeProviders;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.RepetitionService.Strategies
{
    public class PlannedRepetitionTaskStrategy : RepetitionTaskStrategy
    {
        private readonly VocabularyEntryQueries _vocabularyQueries;

        public PlannedRepetitionTaskStrategy(
            IOptions<RepetitionOptions> options,
            IOptions<SM2Options> sm2,
            ILogger<PlannedRepetitionTaskStrategy> logger,
            RepetitionTaskFactory factory,
            ITaskTypeProvider typeProvider,
            VocabularyEntryQueries vocabularyQueries) : base(options, sm2, logger, factory, typeProvider)
        {
            _vocabularyQueries = vocabularyQueries;
        }


        protected override async Task<IQueryable<VocabularyEntry>> GetTargetEntriesQuery(int userId, int vocabId, int take)
        {
            var query = _vocabularyQueries
                .GetEntriesByVocabularyIdQuery(userId, vocabId)
                .Include(e => e.RepetitionState)
                .Include(e => e.Vocabulary)
                .DueEntries()
                .GetRandomEntries(take);

            return query;
        }
    }
}
