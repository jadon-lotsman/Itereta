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
        public IQueryable<VocabularyEntry> GetEntriesByVocabularyIdQuery(int ownerId, int vocabId)
            => _context.VocabularyEntries.Where(e => e.Vocabulary.OwnerId == ownerId && e.Vocabulary.Id == vocabId);

        public IQueryable<VocabularyEntry> GetEntriesByVocabularyGuidQuery(int ownerId, Guid guid)
            => _context.VocabularyEntries.Where(p => p.Vocabulary.Guid == guid && (p.Vocabulary.Visibility != Visibility.Private || p.Vocabulary.OwnerId == ownerId));


        // Getters
        public async Task<bool> ExistsByKeysAsync(int ownerId, int vocabId, string foreign, PartOfSpeech? partOfSpeech)
            => await GetEntriesByVocabularyIdQuery(ownerId, vocabId).AnyAsync(e => e.Foreign == foreign && e.PartOfSpeech == partOfSpeech);

        public async Task<bool> HasAlternativePartOfSpeechAsync(int ownerId, int vocabId, string foreign, PartOfSpeech? partOfSpeech)
            => await GetEntriesByVocabularyIdQuery(ownerId, vocabId).AnyAsync(e => e.Foreign == foreign && e.PartOfSpeech != partOfSpeech);

        public async Task<VocabularyEntry?> GetByIdAsync(int userId, int vocabId, int id)
            => await GetEntriesByVocabularyIdQuery(userId, vocabId).FirstOrDefaultAsync(e => e.Id == id);


        public async Task<HashSet<(string Foreign, PartOfSpeech? PartOfSpeech)>> GetExistingKeysAsync(int userId, int vocabId, IEnumerable<string> foreigns)
        {
            foreigns = foreigns.Distinct().ToList();

            if (!foreigns.Any())
                return new HashSet<(string, PartOfSpeech?)>();

            var existing = await GetEntriesByVocabularyIdQuery(userId, vocabId)
                .Where(e => foreigns.Contains(e.Foreign))
                .Select(e => new { e.Foreign, e.PartOfSpeech })
                .ToListAsync();

            return existing
                .Select(e => (e.Foreign, e.PartOfSpeech))
                .ToHashSet();
        }

        public async Task<List<VocabularyEntry>> GetByQueryAsync(int userId, int vocabId, string query, int limit = 20)
        {
            query = query.ToLower();

            return await GetEntriesByVocabularyIdQuery(userId, vocabId)
                .Where(e => e.Foreign.Contains(query) || e.Translations.Any(t => t.Contains(query)))
                .OrderBy(e => e.Id)
                .Take(limit)
                .ToListAsync();
        }
    }
}
