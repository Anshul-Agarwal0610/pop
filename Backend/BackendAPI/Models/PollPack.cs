namespace BackendAPI.Models;
public enum PollPackVisibility { Private, Public }
public enum PollPackModerationStatus { Draft, PendingReview, Published, Rejected }
public sealed record PollPackItemDto(long Id, int Position, string Proposition,
    IReadOnlyList<string> Choices);
public sealed record PollPackDto(long Id, long OwnerId, string Name, string Description,
    PollPackVisibility Visibility, PollPackModerationStatus ModerationStatus, IReadOnlyList<PollPackItemDto> Items);
public sealed record SavePollPackRequest(string Name, string Description, PollPackVisibility Visibility,
    IReadOnlyList<string> Propositions);
public sealed record ModeratePollPackRequest(PollPackModerationStatus Status, string? Reason = null);
public sealed class PollPackException(string code, string message) : Exception(message) { public string Code { get; } = code; }
