using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;

namespace Mnemo.Data.Queries
{
    public class TaskQueries
    {
        private AppDbContext _context;


        public TaskQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<RepetitionTask> GetByUserIdQuery(int userId)
            => _context.RepetitionTasks.Where(s => s.OwnerId == userId).OrderBy(s => s.OrderIndex);


        // Getters
        public async Task<bool> ExistsByUserId(int userId)
            => await GetByUserIdQuery(userId).AnyAsync();


        public async Task<RepetitionTask?> GetTaskByIdAsync(int userId, int taskId)
            => await GetByUserIdQuery(userId).FirstOrDefaultAsync(t => t.Id == taskId);
    }
}
