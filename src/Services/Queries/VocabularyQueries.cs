using Microsoft.EntityFrameworkCore;
using Mnemo.Data;
using Mnemo.Data.Entities;

namespace Mnemo.Services.Queries
{
    public class VocabularyQueries
    {
        private AppDbContext _context;


        public VocabularyQueries(AppDbContext context)
        {
            _context = context;
        }



        private IQueryable<VocabularyEntry> GetByUserIdQuery(int userId)
            => _context.Entries.Where(e => e.User.Id == userId);


        public async Task<bool> ExistsByIdAsync(int userId, int id)
            => await GetByUserIdQuery(userId).AnyAsync(e => e.Id == id);

        public async Task<bool> ExistsByForeignAsync(int userId, string foreign)
            => await GetByUserIdQuery(userId).AnyAsync(e => e.Foreign == foreign);


        public async Task<VocabularyEntry?> GetByIdAsync(int userId, int id)
            => await GetByUserIdQuery(userId).FirstOrDefaultAsync(e => e.Id == id);

        public async Task<VocabularyEntry?> GetByForeignAsync(int userId, string foreign)
            => await GetByUserIdQuery(userId).FirstOrDefaultAsync(e => string.Equals(e.Foreign, foreign));

        public async Task<List<VocabularyEntry>> GetAllByUserIdAsync(int userId)
            => await GetByUserIdQuery(userId).ToListAsync();


        public async Task<Dictionary<int, VocabularyEntry>> GetDictByIdsAsync(int userId, IEnumerable<int> ids)
        {
            var list = await GetByUserIdQuery(userId)
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();

            return list.ToDictionary(e => e.Id);
        }

        public List<VocabularyEntry> GetRandomByUserId(int userId, int count = 5)
        {
            return _context.Entries.Where(e => e.User.Id == userId)
                .AsEnumerable()
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToList();
        }

        public List<VocabularyEntry> GetDueByUserIdAsync(int userId, int count = 5)
        {
            return _context.Entries.Where(e => e.User.Id == userId)
                .Include(e => e.RepetitionState)
                .Where(e => e.RepetitionState.LastRepetitionAt.AddDays(e.RepetitionState.RepetitionInterval) <= DateOnly.FromDateTime(DateTime.UtcNow))
                .Take(count)
                .ToList();
        }
    }
}
