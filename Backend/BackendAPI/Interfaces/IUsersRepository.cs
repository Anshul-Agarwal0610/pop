using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User>> GetLeaderboardAsync(int count = 20);
        Task<LeaderboardResponse> GetRankingsAsync(LeaderboardPeriod period, int limit, int offset, long? currentUserId, DateTime utcNow);
        Task<User?> GetByIdAsync(long id);
        Task<User?> GetByUsernameAsync(string username);
        Task<long> CreateAsync(CreateUserRequest request);
        Task IncrementPollsCreatedAsync(long userId);
        Task<VoteRewardResult> ApplyVoteRewardAsync(long userId, long pollId, int xpToAdd, DateTime utcNow, bool leaderboardEligible = true);
        /// <summary>US-22: Returns the user's vote history with poll details.</summary>
        Task<IEnumerable<VoteHistoryItem>> GetVoteHistoryAsync(long userId, int count = 20);
        Task<IEnumerable<UserCategoryPreference>> GetCategoryPreferencesAsync(long userId);
        Task<IEnumerable<UserCategoryPreference>> ReplaceCategoryPreferencesAsync(long userId, IEnumerable<string> categories);
        Task ResetCategoryPreferencesAsync(long userId);
    }
}
