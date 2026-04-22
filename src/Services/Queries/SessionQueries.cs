using Microsoft.EntityFrameworkCore;
using Mnemo.Data;
using Mnemo.Data.Entities;

namespace Mnemo.Services.Queries
{
    public class SessionQueries
    {
        private AppDbContext _context;


        public SessionQueries(AppDbContext context)
        {
            _context = context;
        }



        private IQueryable<RepetitionSession> GetByUserIdQuery(int userId)
            => _context.RepetitionSessions.Where(s => s.UserId == userId);


        public async Task<RepetitionSession?> GetByUserIdAsync(int userId)
        {
            return await _context.RepetitionSessions
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.User.Id == userId);
        }

        public async Task<List<RepetitionTask>> GetAllTasksByUserIdAsync(int userId)
        {
            return await _context.RepetitionTasks
                .Where(t => t.RepetitionSession.UserId == userId)
                .ToListAsync();
        }

        public async Task<RepetitionTask?> GetRepetitionTaskByIdAsync(int userId, int taskId)
        {
            return await _context.RepetitionTasks
                .Include(t => t.RepetitionSession)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.RepetitionSession.UserId == userId);
        }
    }
}
