namespace BackendAPI.Models;

public sealed class PollTossInvitation
{
    public Guid Id { get; init; }
    public byte[] TokenHash { get; init; } = [];
    public long PollId { get; init; }
    public long CreatorUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public sealed record CreatePollTossRequest(long PollId);
public sealed record RedeemPollTossRequest(string InvitationToken);
public sealed record PollTossInvitationResponse(Guid Id, string InvitationToken, DateTime ExpiresAt, string ShareUrl);

public sealed class NearbyPollTossOptions
{
    public const string Section = "NearbyPollToss";
    public bool Enabled { get; set; }
    public int RolloutPercent { get; set; }
    public int InvitationTtlSeconds { get; set; } = 120;
    public int DiscoveryTimeoutSeconds { get; set; } = 60;
    public string ShareBaseUrl { get; set; } = "https://pollify.app/toss";
}

public static class PollTossRules
{
    public static bool IsEligible(Poll? poll, DateTime now) => poll is
    {
        IsActive: true, IsPrivate: false, IsWellness: false,
        PollMode: PollModes.Public, ModerationStatus: PollModerationStatus.Published
    } && poll.ExpiresAt > now && !poll.Category.Equals("Health", StringComparison.OrdinalIgnoreCase);
}
