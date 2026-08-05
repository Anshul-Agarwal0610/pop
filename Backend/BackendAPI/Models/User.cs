namespace BackendAPI.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Xp { get; set; }
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalVotes { get; set; }
        public int PollsCreated { get; set; }
        public DateTime? LastVoteDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Level => Xp / 1000 + 1;
        public List<UserBadge> Badges { get; set; } = new();
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>US-22: One row in a user's voting history.</summary>
    public class VoteHistoryItem
    {
        public long   PollId           { get; set; }
        public string Question         { get; set; } = string.Empty;
        public string Category         { get; set; } = string.Empty;
        public string VotedOptionText  { get; set; } = string.Empty;
        public int    TotalVotes       { get; set; }
        public DateTime VotedAt        { get; set; }
    }

    public class VoteRewardResult
    {
        public int Xp { get; set; }
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalVotes { get; set; }
        public int XpAwarded { get; set; }
        public bool StreakAdvanced { get; set; }
        public bool TodayComplete { get; set; }
        public bool RecoveryEligible { get; set; }
        public bool RecoveryUsed { get; set; }
        public DateTime? NextRecoveryAt { get; set; }
        public int? MilestoneReached { get; set; }
        public DateTime? LastVoteDate { get; set; }
        public int Level => Xp / 1000 + 1;
        public IEnumerable<UserBadge> AwardedBadges { get; set; } = Enumerable.Empty<UserBadge>();
    }

    public class StreakStatus
    {
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public bool TodayComplete { get; set; }
        public DateTime? LastVoteDate { get; set; }
        public bool RecoveryEligible { get; set; }
        public DateTime? NextRecoveryAt { get; set; }
        public string TimeZone { get; set; } = "UTC";
        public string DayBoundary { get; set; } = "00:00 UTC";
        public int[] Milestones { get; set; } = Services.GamificationRules.StreakMilestones;
    }

    public class UserCategoryPreference
    {
        public string Category { get; set; } = string.Empty;
        public bool IsExplicit { get; set; }
        public int VoteCount { get; set; }
    }

    public class UpdateCategoryPreferencesRequest
    {
        public List<string> Categories { get; set; } = new();
    }
}
