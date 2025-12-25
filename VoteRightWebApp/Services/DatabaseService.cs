using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VoteRightWebApp.Database;
using VoteRightWebApp.Models;

namespace VoteRightWebApp.Services
{
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;

        public DatabaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> FindUserAsync(string phoneNumber)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public List<User> GetUsers(string district, string assembly)
        {
            var usersQuery = _context.Users.AsNoTracking().Where(u => u.District == district);

            if (!string.IsNullOrEmpty(assembly))
            {
                usersQuery = usersQuery
                    .Join(
                        _context.Downloads.AsNoTracking().Where(d => d.Assembly == assembly),
                        user => user.Id,
                        download => download.UserId,
                        (user, download) => user
                    );
            }

            var users = usersQuery.Distinct().ToList();
            return users;
        }

        public async Task AddDownloadAsync(Download download)
        {
            _context.Downloads.Add(download);
            await _context.SaveChangesAsync();
        }
    }
}