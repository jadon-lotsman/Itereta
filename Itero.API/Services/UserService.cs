using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Itero.API.Data;
using Itero.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itero.API.Services
{
    public class UserService
    {
        private AppDbContext _context;


        public UserService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async void CreateAsync(string username)
        {
            await _context.Users.AddAsync(new User(username));

            await _context.SaveChangesAsync();
        }
    }
}
