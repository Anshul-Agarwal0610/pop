namespace BackendAPI.Analytics;
public static class AnalyticsEventNames
{
    public const string ChallengeStarted = "challenge_started";
    public const string ChallengeProgressed = "challenge_progressed";
    public const string ChallengeCompleted = "challenge_completed";
    public const string StreakChanged = "streak_changed";
    public const string LevelUp = "level_up";
    public const string AchievementUnlocked = "achievement_unlocked";
    public const string GameRoundCompleted = "game_round_completed";
    public const string PollTossAccepted = "poll_toss_server_accepted";
    public const string PollTossExpired = "poll_toss_server_expired";
}
