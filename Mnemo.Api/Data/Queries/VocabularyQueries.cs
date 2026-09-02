using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Data.Queries
{
    public class VocabularyQueries
    {
        private AppDbContext _context;


        public VocabularyQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<Vocabulary> GetVocabByGuidSecuredQuery(int ownerId, Guid vocabGuid)
            => _context.Vocabularies.Where(p => p.Guid == vocabGuid && (p.Visibility != Visibility.Private || p.OwnerId == ownerId));


        // Getters
        public async Task<bool> ExistsByGuidAsync(int ownerId, Guid vocabGuid)
            => await GetVocabByGuidSecuredQuery(ownerId, vocabGuid).AnyAsync();

        public async Task<int?> GetIdByGuidAsync(int ownerId, Guid vocabGuid)
            => await GetVocabByGuidSecuredQuery(ownerId, vocabGuid).Select(v => v.Id).FirstOrDefaultAsync();

        public async Task<Vocabulary?> GetByGuidAsync(int ownerId, Guid vocabGuid)
            => await GetVocabByGuidSecuredQuery(ownerId, vocabGuid).FirstOrDefaultAsync();

        public async Task<List<Vocabulary>> GetPublishedAsync()
            => await _context.Vocabularies.Where(p => p.Visibility == Visibility.Public).ToListAsync();
    }
}
