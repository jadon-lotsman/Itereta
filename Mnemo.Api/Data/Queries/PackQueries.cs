using Mnemo.Data.Entities;

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


        // Getters

    }
}
