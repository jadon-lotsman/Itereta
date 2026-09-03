using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.RepetitionService.Providers.DistractorProviders
{
    public class PrefixDistractorProvider : IDistractorProvider
    {
        private VocabularyEntryQueries _vocabularyQueries;

        public PrefixDistractorProvider(VocabularyEntryQueries vocabularyQueries)
        {
            _vocabularyQueries = vocabularyQueries;
        }


        public async Task<List<string>> GetDistractorsAsync(bool isForward, VocabularyEntry baseEntry, int take, params int[] excludeIds)
        {
            var query = _vocabularyQueries
                    .GetEntriesByVocabularyIdQuery(baseEntry.Vocabulary.OwnerId, baseEntry.VocabularyId);

            if (isForward)
            {
                string prefix = GetPrefix(baseEntry.Translations[0], 2);

                if (string.IsNullOrWhiteSpace(prefix))
                    return [];

                query = query.Where(e => e.Translations[0].StartsWith(prefix)
                                         && !e.Foreign.Equals(baseEntry.Foreign));
            }
            else
            {
                string prefix = GetPrefix(baseEntry.Foreign, 2);

                if (string.IsNullOrWhiteSpace(prefix))
                    return [];

                query = query.Where(e => e.Foreign.StartsWith(prefix)
                                         && !e.Foreign.Equals(baseEntry.Foreign));
            }

            var entries = await query
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

        private string GetPrefix(string str, int prefixLength)
        {
            str = str.Trim();

            if (string.IsNullOrEmpty(str)) return string.Empty;
            int len = Math.Min(prefixLength, str.Length);
            return str.Substring(0, len);
        }
    }
}
