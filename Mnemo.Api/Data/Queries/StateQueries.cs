using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;

namespace Mnemo.Data.Queries
{
    public class StateQueries
    {
        private AppDbContext _context;


        public StateQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<RepetitionState> GetByUserIdQuery(int userId)
            => _context.RepetitionStates.Where(rs => rs.VocabularyEntry.Vocabulary.OwnerId == userId);


        // Getters
        public async Task<RepetitionState?> GetByEntryIdAsync(int userId, int entryId)
            => await GetByUserIdQuery(userId).FirstOrDefaultAsync(s => s.VocabularyEntryId == entryId);

        public async Task<Dictionary<int, RepetitionState>> GetDictByEntryIdsAsync(int userId, IEnumerable<int> ids)
        {
            var list = await GetByUserIdQuery(userId)
                .Where(s => ids.Contains(s.VocabularyEntryId))
                .ToListAsync();

            return list.ToDictionary(s => s.Id);
        }
    }
}
