using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IAchievementsRepository
    {
        Task<IEnumerable<UserBadge>> GetUserBadgesAsync(long userId);
        Task<Dictionary<long, List<UserBadge>>> GetBadgesForUsersAsync(IEnumerable<long> userIds);
        Task<AchievementAwardResult> AwardEligibleBadgesAsync(long userId, DateTime utcNow);
        Task<AchievementOverview> GetOverviewAsync(long userId);
    }
}
