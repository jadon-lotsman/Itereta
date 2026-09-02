using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared;
using Mnemo.Shared.Enums;

namespace Mnemo.Services.AccountService
{
    public class AccountManagementService
    {
        private AppDbContext _context;

        private AccountQueries _accountQueries;


        public AccountManagementService(AppDbContext context, AccountQueries accountQueries)
        {
            _context = context;
            _accountQueries = accountQueries;
        }



        public async Task<RequestResult<bool>> CreateAsync(string username)
        {
            if (await _accountQueries.ExistsByUsernameAsync(username))
                return RequestResult<bool>.Failure(ErrorCode.UsernameTaken, $"Username '{username}' Is Taken");


            var user = new User();
            user.Username = username;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return RequestResult<bool>.Success(true);
        }
    }
}
