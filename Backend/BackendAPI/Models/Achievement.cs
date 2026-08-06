namespace BackendAPI.Models
{
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
        public DateTime AwardedAt { get; set; }
    }

    public class AchievementAwardResult
    {
        public IEnumerable<UserBadge> AwardedBadges { get; set; } = Enumerable.Empty<UserBadge>();
        public int BonusXpAwarded { get; set; }
    }

    public class AchievementProgress
    {
        public long BadgeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public int CurrentValue { get; set; }
        public int Threshold { get; set; }
        public double ProgressPercent { get; set; }
        public int RewardXp { get; set; }
    }

    public class AchievementOverview
    {
        public List<UserBadge> RecentlyEarned { get; set; } = new();
        public List<AchievementProgress> NextAchievable { get; set; } = new();
        public bool AllEarned { get; set; }
    }

    public static class AchievementRuleType
    {
        public const string VoteCount = "VoteCount";
        public const string Streak = "Streak";
        public const string PollCreation = "PollCreation";
        public const string ChallengeCompletion = "ChallengeCompletion";
    }
}
