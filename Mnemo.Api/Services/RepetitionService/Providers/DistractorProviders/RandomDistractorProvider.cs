using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.RepetitionService.Providers.DistractorProviders
{
    public class RandomDistractorProvider : IDistractorProvider
    {
        private VocabularyEntryQueries _vocabularyQueries;

        public RandomDistractorProvider(VocabularyEntryQueries vocabularyQueries)
        {
            _vocabularyQueries = vocabularyQueries;
        }

        public async Task<List<string>> GetDistractorsAsync(bool isForward, VocabularyEntry baseEntry, int take, params int[] excludeIds)
        {
            var entries = await _vocabularyQueries
                    .GetEntriesByOwnerIdQuery(baseEntry.Vocabulary.OwnerId)
                    .GetRandomEntries(take, excludeIds)
                    .ToListAsync();

            var result = new List<string>();
            foreach (var entry in entries)
            {
                string distractor = isForward ? entry.Translations[0] : entry.Foreign;
                if (!result.Contains(distractor))
                    result.Add(distractor);
            }

            return result;
        }
    }
}
