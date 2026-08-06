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
    public const string PopLiveTossShown = "pop_live_toss_shown";
    public const string PopLiveInvitationCreated = "pop_live_invitation_created";
    public const string PopLiveInvitationOpened = "pop_live_invitation_opened";
    public const string PopLiveSessionJoined = "pop_live_session_joined";
    public const string PopLiveFirstResponseLocked = "pop_live_first_response_locked";
    public const string PopLiveSessionCompleted = "pop_live_session_completed";
    public const string PopLiveResultShared = "pop_live_result_shared";
    public const string PopLiveRematchRequested = "pop_live_rematch_requested";
    public const string PopLiveRematchStarted = "pop_live_rematch_started";
    public const string PopLiveRelayHandoff = "pop_live_relay_handoff";
}
