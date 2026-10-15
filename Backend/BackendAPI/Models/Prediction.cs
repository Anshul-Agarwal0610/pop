namespace BackendAPI.Models;

public static class PredictionStatuses
{
    public const string Pending = "Pending";
    public const string Correct = "Correct";
    public const string Incorrect = "Incorrect";
    public const string NoContest = "NoContest";
}

public static class PredictionTypes
{
    public const string Winner = "Winner";
    public const string Split = "Split";
}

public sealed class CreatePredictionRequest
{
    public long PollId { get; set; }
    public long PredictedOptionId { get; set; }
    public string PredictionType { get; set; } = PredictionTypes.Winner;
    public decimal? PredictedPercentage { get; set; }
}

public sealed class PredictionOpportunity
{
    public Poll Poll { get; set; } = new();
    public DateTime LocksAt => Poll.ExpiresAt;
}

public sealed class PredictionEntry
{
    public long Id { get; set; }
    public long PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long PredictedOptionId { get; set; }
    public string PredictedOptionText { get; set; } = string.Empty;
    public string PredictionType { get; set; } = PredictionTypes.Winner;
    public decimal? PredictedPercentage { get; set; }
    public decimal? ActualPercentage { get; set; }
    public decimal? AccuracyDifference { get; set; }
    public long? WinningOptionId { get; set; }
    public string? WinningOptionText { get; set; }
    public string Status { get; set; } = PredictionStatuses.Pending;
    public int XpAwarded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LocksAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public int TotalVotes { get; set; }
}

public sealed record PredictionRewardCandidate(long Id, string RuleCode);
