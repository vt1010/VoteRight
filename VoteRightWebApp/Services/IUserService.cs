using VoteRightWebApp.Models;

namespace VoteRightWebApp.Services
{
    public interface IUserService
    {
        Task<User?> FindUserAsync(int phoneNumber);
        Task AddUserAsync(User user);
        List<User> GetUsers(string district, string assembly);
    }
}
