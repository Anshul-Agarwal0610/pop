using BackendAPI.Models;

namespace BackendAPI.Services;

public static class AchievementPresenter
{
    public static AchievementCollectionItem Present(AchievementBadge badge, UserBadge? earned, AchievementMetrics metrics)
    {
        var current = Math.Max(0, metrics.ForRule(badge.RuleType));
        if (earned != null)
            return new AchievementCollectionItem { BadgeId = badge.Id, UserBadgeId = earned.Id, Code = badge.Code, Name = badge.Name,
                Description = badge.Description, Icon = badge.Icon, Category = badge.Category, Status = AchievementStatus.Earned,
                RewardXp = badge.RewardXp, RewardTitle = badge.RewardTitle, AwardedAt = earned.AwardedAt, IsSecret = badge.IsSecret };

        if (badge.IsSecret)
            return new AchievementCollectionItem { BadgeId = badge.Id, Code = badge.Code, Name = "Secret achievement",
                Description = "Keep exploring to discover this achievement.", Icon = "LockKeyhole", Category = badge.Category,
                Status = AchievementStatus.Locked, RewardXp = badge.RewardXp, IsSecret = true };

        var showProgress = badge.ProgressVisible && badge.Threshold > 0;
        return new AchievementCollectionItem { BadgeId = badge.Id, Code = badge.Code, Name = badge.Name,
            Description = badge.Description, Requirement = badge.RequirementText, Icon = badge.Icon, Category = badge.Category,
            Status = showProgress && current > 0 ? AchievementStatus.InProgress : AchievementStatus.Locked,
            RewardXp = badge.RewardXp, RewardTitle = badge.RewardTitle,
            CurrentProgress = showProgress ? Math.Min(current, badge.Threshold) : null,
            TargetProgress = showProgress ? badge.Threshold : null,
            ProgressPercent = showProgress ? Math.Min(100, current * 100 / badge.Threshold) : null };
    }
}
