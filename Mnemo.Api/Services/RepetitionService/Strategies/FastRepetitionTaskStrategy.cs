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
        private readonly VocabularyQueries _vocabularyQueries;

        public FastRepetitionTaskStrategy(
            IOptions<RepetitionOptions> options,
            IOptions<SM2Options> sm2,
            ILogger<FastRepetitionTaskStrategy> logger,
            RepetitionTaskFactory factory,
            ITaskTypeProvider typeProvider,
            VocabularyQueries vocabularyQueries) : base(options, sm2, logger, factory, typeProvider)
        {
            _vocabularyQueries = vocabularyQueries;
        }


        protected override async Task<IQueryable<VocabularyEntry>> GetEntriesQuery(int userId, int take)
        {
            var priorityEntriesQuery = _vocabularyQueries
                .GetByUserIdQuery(userId)
                .Include(e => e.RepetitionState)
                .NotDueEntries()
                .NotRepeatedTodayEntries()
                .GetRandomEntries(take);

            var mixQuery = priorityEntriesQuery;

            if (priorityEntriesQuery.Count() < take)
            {
                var existingIds = priorityEntriesQuery.Select(e => e.Id).ToArray();

                var randomEntries = _vocabularyQueries
                    .GetByUserIdQuery(userId)
                    .Include(e => e.RepetitionState)
                    .NotDueEntries()
                    .GetRandomEntries(take - existingIds.Length, existingIds);

                mixQuery = mixQuery.Concat(randomEntries);
            }


            return mixQuery;
        }
    }
}
