using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IAchievementsRepository
    {
        Task<IEnumerable<UserBadge>> GetUserBadgesAsync(long userId);
        Task<Dictionary<long, List<UserBadge>>> GetBadgesForUsersAsync(IEnumerable<long> userIds);
        Task<AchievementAwardResult> AwardEligibleBadgesAsync(long userId, DateTime utcNow);
        Task<AchievementCollectionResponse> GetCollectionAsync(long userId);
        Task<PublicAchievementsResponse> GetPublicAchievementsAsync(long userId);
        Task<IEnumerable<AchievementCelebration>> ClaimPendingCelebrationsAsync(long userId, DateTime utcNow);
        Task<bool> SelectTitleAsync(long userId, long badgeId);
        Task ClearTitleAsync(long userId);
    }
}
