namespace BackendAPI.Models;

public static class MultiplayerModes
{
    public const string Clash = "Clash";
    public const string Relay = "Relay";
    public const string Room = "Room";
    public static bool IsSupported(string mode) => mode is Clash or Relay or Room;
}

public enum ResultCardState { Active, Completed, Expired }

public sealed record ResultCardParticipant(string Label, string? AvatarUrl = null, bool IsAnonymous = true);
public sealed record ResultCardBadge(string Name, string Icon);

public sealed record ResultCardPayload(
    int SchemaVersion,
    string Mode,
    ResultCardState State,
    string AggregateResult,
    string? Milestone,
    ResultCardBadge? Badge,
    int ParticipantCount,
    IReadOnlyList<ResultCardParticipant> Participants,
    string AccessibleSummary);

public sealed record ResultCardDto(
    long Id,
    string PublicToken,
    ResultCardPayload Payload,
    string PublicUrl,
    string ImageUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt);

// This is an internal, server-owned completion contract. Controllers never bind it from a request.
public sealed record NormalizedMultiplayerResult(
    long SessionId,
    long OwnerUserId,
    string Mode,
    ResultCardState State,
    string AggregateResult,
    string? Milestone,
    RecordedBadge? EarnedBadge,
    IReadOnlyList<MultiplayerResultParticipant> Participants);

public sealed record RecordedBadge(string Name, string Icon);
public sealed record MultiplayerResultParticipant(long UserId, string DisplayName, string? AvatarUrl, bool PublicCardConsent);

public sealed class ResultCardException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class StoredResultCard
{
    public long Id { get; init; }
    public string PublicToken { get; init; } = string.Empty;
    public long SessionId { get; init; }
    public long OwnerUserId { get; init; }
    public int SchemaVersion { get; init; }
    public string PayloadJson { get; init; } = string.Empty;
    public string PayloadHash { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public sealed record ResultCardPage(IReadOnlyList<ResultCardDto> Items, int Offset, int Limit, bool HasMore);
