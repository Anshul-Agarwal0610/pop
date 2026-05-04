using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User>> GetLeaderboardAsync(int count = 20);
        Task<User?> GetByIdAsync(long id);
        Task<User?> GetByUsernameAsync(string username);
        Task<long> CreateAsync(CreateUserRequest request);
        Task UpdateXpAsync(long userId, int xpToAdd);
        Task UpdateStreakAsync(long userId);
        /// <summary>US-22: Returns the user's vote history with poll details.</summary>
        Task<IEnumerable<VoteHistoryItem>> GetVoteHistoryAsync(long userId, int count = 20);
    }
}
