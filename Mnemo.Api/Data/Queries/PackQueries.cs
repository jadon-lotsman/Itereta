using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Data.Queries
{
    public class PackQueries
    {
        private AppDbContext _context;


        public PackQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<VocabularyPack> GetByUserIdQuery(int userId)
            => _context.Packs.Where(p => p.AuthorId == userId);

        public IQueryable<VocabularyPack> GetSecuredByGuidQuery(int userId, Guid packGuid)
            => _context.Packs.Where(p => p.Guid == packGuid && (p.Visibility != Visibility.Private || p.AuthorId == userId));


        // Getters
        public async Task<bool> ExistsByGuidAsync(int userId, Guid packGuid)
            => await GetSecuredByGuidQuery(userId, packGuid).AnyAsync();

        public async Task<VocabularyPack?> GetSecuredByGuidAsync(int userId, Guid packGuid)
            => await GetSecuredByGuidQuery(userId, packGuid).FirstOrDefaultAsync();

        public async Task<List<VocabularyPack>> GetAllPublicAsync()
            => await _context.Packs.Where(p => p.Visibility == Visibility.Public).ToListAsync();
    }
}
