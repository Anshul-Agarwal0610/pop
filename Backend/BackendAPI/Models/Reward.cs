namespace BackendAPI.Models;

public static class RewardRuleCodes
{
    public const string VoteStandard = "vote.standard";
    public const string VoteTrending = "vote.trending";
    public const string ClashParticipation = "clash.participation";
    public const string ClashPrediction = "clash.prediction";
}

public sealed class RewardRule
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Version { get; set; }
    public int Value { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int PerActionLimit { get; set; }
    public int? PeriodLimit { get; set; }
    public string? PeriodUnit { get; set; }
    public int? PeriodValue { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class RewardLedgerEvent
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? RuleId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public int RuleVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public int Value { get; set; }
    public string EventType { get; set; } = "Grant";
    public long? ReversesEventId { get; set; }
    public long? ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record RewardGrantRequest(long UserId, string RuleCode, string SourceType,
    string SourceReference, DateTime OccurredAtUtc);

public sealed record RewardGrantResult(RewardLedgerEvent Event, int CurrentXp, bool IsDuplicate);

public sealed record ReverseRewardRequest(string Reason, string IdempotencyKey);
public sealed record ManualAdjustmentRequest(long UserId, int Value, string Reason, string IdempotencyKey);

public sealed class RewardConfiguration
{
    public int XpPerLevel { get; init; } = 1000;
    public IEnumerable<RewardRule> Rules { get; init; } = Array.Empty<RewardRule>();
}

public sealed class RewardReconciliation
{
    public long UserId { get; set; }
    public int CachedXp { get; set; }
    public int LedgerXp { get; set; }
    public int Difference => CachedXp - LedgerXp;
}

public sealed class SuspiciousRewardActivity
{
    public long UserId { get; set; }
    public int EventCount { get; set; }
    public int NetXp { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}
