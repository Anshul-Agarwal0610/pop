namespace BackendAPI.Models;

public static class PollTossStatuses { public const string Pending="Pending", Accepted="Accepted", Cancelled="Cancelled", Expired="Expired"; }
public sealed class PollTossInvitation {
    public Guid Id { get; set; } public long PollId { get; set; } public long SenderUserId { get; set; }
    public long? RecipientUserId { get; set; } public string Status { get; set; } = PollTossStatuses.Pending;
    public string RoomCode { get; set; } = ""; public DateTime CreatedAt { get; set; } public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; } public DateTime? CancelledAt { get; set; } public long StateVersion { get; set; }
    public PollTossPollPreview? Poll { get; set; }
}
public sealed class PollTossPollPreview { public long Id { get; set; } public string Question { get; set; }=""; public string Category { get; set; }=""; public string? ThumbnailUrl { get; set; } }
public sealed record CreatePollTossRequest(long PollId);
public sealed record CreatedPollTossResponse(Guid Id,long PollId,string Status,long StateVersion,DateTime ExpiresAt,string Token,string RoomCode,PollTossPollPreview? Poll);
public sealed class PollTossException(string code,string message) : Exception(message) { public string Code { get; }=code; }
