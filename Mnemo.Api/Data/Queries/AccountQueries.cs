using Microsoft.EntityFrameworkCore;
using Mnemo.Data.Entities;

namespace Mnemo.Data.Queries
{
    public class AccountQueries
    {
        private AppDbContext _context;


        public AccountQueries(AppDbContext context)
        {
            _context = context;
        }


        // Queries
        public IQueryable<User> GetByIdQuery(int userId)
            => _context.Users.Where(u => u.Id == userId);

        public IQueryable<User> GetByUsernameQuery(string username)
            => _context.Users.Where(u => u.Username == username);


        // Getters
        public async Task<bool> ExistsByIdAsync(int userId)
            => await GetByIdQuery(userId).AnyAsync();

        public async Task<User?> GetByIdAsync(int userId)
            => await GetByIdQuery(userId).FirstOrDefaultAsync();


        public async Task<bool> ExistsByUsernameAsync(string username)
            => await GetByUsernameQuery(username).AnyAsync();

        public async Task<User?> GetByUsernameAsync(string username)
            => await GetByUsernameQuery(username).FirstOrDefaultAsync();
    }
}
