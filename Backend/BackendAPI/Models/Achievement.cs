namespace BackendAPI.Models;

public class AchievementBadge
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public int Threshold { get; set; }
    public int RewardXp { get; set; }
    public string Category { get; set; } = AchievementCategory.Voting;
    public string RequirementText { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public bool IsPublic { get; set; }
    public bool ProgressVisible { get; set; }
    public string? RewardTitle { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class UserBadge
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long BadgeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int RewardXp { get; set; }
    public string? RewardTitle { get; set; }
    public DateTime AwardedAt { get; set; }
}

public record AchievementMetrics(int TotalVotes, int Streak, int PollsCreated, int CompletedChallenges, int DistinctCategoriesVoted)
{
    public int ForRule(string rule) => rule switch
    {
        AchievementRuleType.VoteCount => TotalVotes,
        AchievementRuleType.Streak => Streak,
        AchievementRuleType.PollCreation => PollsCreated,
        AchievementRuleType.ChallengeCompletion => CompletedChallenges,
        AchievementRuleType.DistinctCategoriesVoted => DistinctCategoriesVoted,
        _ => 0
    };
}

public class AchievementCollectionItem
{
    public long BadgeId { get; set; }
    public long? UserBadgeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = AchievementStatus.Locked;
    public string? Requirement { get; set; }
    public int RewardXp { get; set; }
    public string? RewardTitle { get; set; }
    public DateTime? AwardedAt { get; set; }
    public int? CurrentProgress { get; set; }
    public int? TargetProgress { get; set; }
    public int? ProgressPercent { get; set; }
    public bool IsSecret { get; set; }
}

public class AchievementCollectionResponse
{
    public IEnumerable<AchievementCollectionItem> Achievements { get; set; } = [];
    public string? SelectedTitle { get; set; }
    public long? SelectedTitleBadgeId { get; set; }
    public int EarnedCount { get; set; }
    public int TotalCount { get; set; }
}

public class PublicAchievement
{
    public long BadgeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime AwardedAt { get; set; }
    public string? RewardTitle { get; set; }
}

public class PublicAchievementsResponse
{
    public IEnumerable<PublicAchievement> Achievements { get; set; } = [];
    public string? SelectedTitle { get; set; }
}

public class AchievementCelebration : UserBadge { }
public class SelectProfileTitleRequest { public long BadgeId { get; set; } }

public class AchievementAwardResult
{
    public IEnumerable<UserBadge> AwardedBadges { get; set; } = [];
    public int BonusXpAwarded { get; set; }
}

public class AchievementProgress
{
    public long BadgeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int Threshold { get; set; }
    public double ProgressPercent { get; set; }
    public int RewardXp { get; set; }
}

public class AchievementOverview
{
    public List<UserBadge> RecentlyEarned { get; set; } = [];
    public List<AchievementProgress> NextAchievable { get; set; } = [];
    public bool AllEarned { get; set; }
}

public static class AchievementRuleType
{
    public const string VoteCount = "VoteCount";
    public const string Streak = "Streak";
    public const string PollCreation = "PollCreation";
    public const string ChallengeCompletion = "ChallengeCompletion";
    public const string DistinctCategoriesVoted = "DistinctCategoriesVoted";
}
public static class AchievementCategory
{
    public const string Voting = "Voting";
    public const string Streak = "Streak";
    public const string Challenge = "Challenge";
    public const string Exploration = "Exploration";
}
public static class AchievementStatus
{
    public const string Earned = "earned";
    public const string InProgress = "in-progress";
    public const string Locked = "locked";
}
