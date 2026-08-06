namespace BackendAPI.Models
{
    public sealed record ProgressionSnapshot(
        int TotalXp,
        int Level,
        int CurrentLevelXp,
        int NextLevelXp,
        int XpIntoLevel,
        int XpRequiredForNextLevel,
        int ProgressPercent);

    public enum RewardEventType
    {
        Vote,
        Challenge,
        Achievement
    }

    public sealed record RewardEvent(
        RewardEventType Type,
        string SourceId,
        int AwardedXp,
        string? Label = null);

    public class ProgressionReward
    {
        public int AwardedXp { get; set; }
        public ProgressionSnapshot Progression { get; set; } = Services.GamificationRules.FromTotalXp(0);
        public int PreviousLevel { get; set; } = 1;
        public bool LeveledUp => Progression.Level > PreviousLevel;
        public int LevelsGained => Math.Max(0, Progression.Level - PreviousLevel);
        public IReadOnlyList<RewardEvent> Events { get; set; } = Array.Empty<RewardEvent>();
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalVotes { get; set; }
        public bool StreakAdvanced { get; set; }
        public bool TodayComplete { get; set; }
        public bool RecoveryEligible { get; set; }
        public bool RecoveryUsed { get; set; }
        public DateTime? NextRecoveryAt { get; set; }
        public int? MilestoneReached { get; set; }
        public DateTime? LastVoteDate { get; set; }
        public IEnumerable<UserBadge> AwardedBadges { get; set; } = Enumerable.Empty<UserBadge>();

        // Transitional aliases retained for existing clients.
        public int Xp => Progression.TotalXp;
        public int Level => Progression.Level;
        public int XpAwarded => AwardedXp;
    }
}
