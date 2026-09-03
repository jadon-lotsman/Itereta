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
        public IQueryable<Vocabulary> GetVocabByIdQuery(int ownerId, int id)
            => _context.Vocabularies.Where(p => p.OwnerId == ownerId && p.Id == id);

        public IQueryable<Vocabulary> GetVocabByGuidSecuredQuery(int ownerId, Guid guid)
            => _context.Vocabularies.Where(p => p.Guid == guid && (p.Visibility != Visibility.Private || p.OwnerId == ownerId));


        // Getters
        public async Task<int?> GetOriginIdAsync(int ownerId)
            => await _context.Vocabularies.Where(p => p.OwnerId == ownerId && p.IsOrigin).Select(v => v.Id).FirstOrDefaultAsync();

        public async Task<Vocabulary?> GetByGuidAsync(int ownerId, Guid guid)
            => await GetVocabByGuidSecuredQuery(ownerId, guid).FirstOrDefaultAsync();

        public async Task<int?> GetIdByGuidAsync(int ownerId, Guid guid)
            => await GetVocabByGuidSecuredQuery(ownerId, guid).Select(v => v.Id).FirstOrDefaultAsync();


        public async Task<bool> ExistsByIdAsync(int ownerId, int id)
            => await GetVocabByIdQuery(ownerId, id).AnyAsync();

        public async Task<Vocabulary?> GetByIdAsync(int ownerId, int id)
            => await GetVocabByIdQuery(ownerId, id).FirstOrDefaultAsync();

        public async Task<List<Vocabulary>> GetPublishedAsync()
            => await _context.Vocabularies.Where(p => p.Visibility == Visibility.Public).ToListAsync();
    }
}
