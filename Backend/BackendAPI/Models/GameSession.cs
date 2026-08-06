namespace BackendAPI.Models;

public static class GameModes
{
    public const string OpinionSprint = "OpinionSprint";
}

public static class GameSessionStatuses
{
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Expired = "Expired";
}

public sealed class GameModeDto
{
    public string Mode { get; init; } = GameModes.OpinionSprint;
    public string Name { get; init; } = "Opinion Sprint";
    public string Category { get; init; } = "General";
    public int PollCount { get; init; } = 5;
    public int? TimeLimitSeconds { get; init; } = 120;
    public int CompletionXp { get; init; } = 100;
    public string Rules { get; init; } = "Choose the option that best matches your opinion. There are no right or wrong answers.";
    public bool Available { get; init; } = true;
}

public sealed class StartGameSessionRequest
{
    public string Mode { get; init; } = GameModes.OpinionSprint;
    public string Category { get; init; } = "General";
    public bool Timed { get; init; } = true;
}

public sealed class GameSessionDto
{
    public long Id { get; init; }
    public string Mode { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PollCount { get; init; }
    public int CurrentPosition { get; init; }
    public int VotesCast { get; init; }
    public int RemainingPolls => Math.Max(0, PollCount - CurrentPosition);
    public int? TimeLimitSeconds { get; init; }
    public int CompletionXp { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime ServerNow { get; set; }
    public Poll? CurrentPoll { get; set; }
    public CompletionSummaryDto? Summary { get; set; }
}

public sealed class GameVoteRequest
{
    public int Position { get; init; }
    public long PollId { get; init; }
    public long OptionId { get; init; }
}

public sealed class GameVoteResult
{
    public GameSessionDto Session { get; init; } = new();
    public int XpAwarded { get; init; }
    public IEnumerable<UserChallenge> Challenges { get; set; } = [];
    public IEnumerable<UserBadge> AchievementsUnlocked { get; set; } = [];
}

public sealed class CompletionSummaryDto
{
    public int Votes { get; init; }
    public int VoteXpEarned { get; init; }
    public int CompletionXpEarned { get; init; }
    public int TotalXpEarned => VoteXpEarned + CompletionXpEarned;
    public IEnumerable<UserChallenge> ChallengeProgress { get; init; } = [];
    public IEnumerable<UserBadge> AchievementsUnlocked { get; init; } = [];
}

public sealed class GameSessionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
