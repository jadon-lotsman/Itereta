using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Data.Queries
{
    public class VocabularyEntryQueries
    {
        private AppDbContext _context;


        public VocabularyEntryQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<VocabularyEntry> GetVocabEntriesByGuidSecuredQuery(int ownerId, Guid vocabGuid)
            => _context.VocabularyEntries.Where(e => e.Vocabulary.Guid == vocabGuid && (e.Vocabulary.Visibility != Visibility.Private || e.Vocabulary.OwnerId == ownerId));


        // Getters
        public async Task<bool> ExistsByKeysAsync(int ownerId, Guid vocabGuid, string foreign, PartOfSpeech? partOfSpeech)
            => await GetVocabEntriesByGuidSecuredQuery(ownerId, vocabGuid).AnyAsync(e => e.Foreign == foreign && e.PartOfSpeech == partOfSpeech);

        public async Task<bool> HasAlternativePartOfSpeechAsync(int ownerId, Guid vocabId, string foreign, PartOfSpeech? partOfSpeech)
            => await GetVocabEntriesByGuidSecuredQuery(ownerId, vocabId).AnyAsync(e => e.Foreign == foreign && e.PartOfSpeech != partOfSpeech);

        public async Task<VocabularyEntry?> GetByIdAsync(int userId, Guid vocabId, int id)
            => await GetVocabEntriesByGuidSecuredQuery(userId, vocabId).FirstOrDefaultAsync(e => e.Id == id);


        public async Task<HashSet<(string Foreign, PartOfSpeech? PartOfSpeech)>> GetExistingKeysAsync(int userId, Guid vocabId, IEnumerable<string> foreigns)
        {
            foreigns = foreigns.Distinct().ToList();

            if (!foreigns.Any())
                return new HashSet<(string, PartOfSpeech?)>();

            var existing = await GetVocabEntriesByGuidSecuredQuery(userId, vocabId)
                .Where(e => foreigns.Contains(e.Foreign))
                .Select(e => new { e.Foreign, e.PartOfSpeech })
                .ToListAsync();

            return existing
                .Select(e => (e.Foreign, e.PartOfSpeech))
                .ToHashSet();
        }

        public async Task<List<VocabularyEntry>> GetByQueryAsync(int userId, Guid vocabId, string query, int limit = 20)
        {
            query = query.ToLower();

            return await GetVocabEntriesByGuidSecuredQuery(userId, vocabId)
                .Where(e => e.Foreign.Contains(query) || e.Translations.Any(t => t.Contains(query)))
                .OrderBy(e => e.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
