using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.RepetitionService.Providers.DistractorProviders
{
    public class ByPartOfSpeechDistractorProvider : IDistractorProvider
    {
        private VocabularyEntryQueries _entryQueries;

        public ByPartOfSpeechDistractorProvider(VocabularyEntryQueries entryQueries)
        {
            _entryQueries = entryQueries;
        }


        public async Task<List<string>> GetDistractorsAsync(bool isForward, VocabularyEntry baseEntry, int take, params int[] excludeIds)
        {
            var entries = await _entryQueries
                    .GetEntriesByVocabularyIdQuery(baseEntry.Vocabulary.OwnerId, baseEntry.VocabularyId)
                    .Where(e => baseEntry.PartOfSpeech != null && e.PartOfSpeech == baseEntry.PartOfSpeech)
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
