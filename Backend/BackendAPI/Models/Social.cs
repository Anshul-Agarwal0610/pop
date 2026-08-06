namespace BackendAPI.Models;

public enum RelationshipState { Pending, Accepted, Declined, Removed }
public enum GroupMembershipState { Invited, Active, Declined, Left, Removed }
public enum GroupRole { Owner, Member }

public sealed record SocialUserSummary(long Id, string Username, string DisplayName, string? AvatarUrl);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor);
public sealed record FriendConnection(long Id, SocialUserSummary User, RelationshipState State, bool Incoming, DateTime UpdatedAt);
public sealed record SocialGroup(long Id, string Name, long OwnerUserId, string ModerationStatus, int MemberCount, GroupRole Role, DateTime CreatedAt);
public sealed record GroupMember(SocialUserSummary User, GroupRole Role, DateTime JoinedAt);
public sealed record GroupInviteSummary(string Token, long GroupId, string GroupName, SocialUserSummary Inviter, DateTime ExpiresAt);
public sealed record SocialWeeklyLeaderboardEntry(int Rank, SocialUserSummary User, int Xp, int ActivityCount);
public sealed record WeeklyLeaderboard(DateTime WeekStartUtc, DateTime WeekEndUtc, IReadOnlyList<SocialWeeklyLeaderboardEntry> Items, string? NextCursor);

public sealed class SendFriendRequest { public long TargetUserId { get; set; } }
public sealed class BlockUserRequest { public long TargetUserId { get; set; } }
public sealed class CreateGroupRequest { public string Name { get; set; } = string.Empty; }
public sealed class CreateGroupInviteRequest { public long TargetUserId { get; set; } }

public sealed class SocialConflictException(string message) : Exception(message);
public sealed class SocialForbiddenException(string message) : Exception(message);
public sealed class SocialNotFoundException(string message) : Exception(message);
public sealed class SocialRateLimitException(string message) : Exception(message);

public static class SocialLeagueRules
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;
    public const int MaxGroupMembers = 50;
    public static int ClampLimit(int limit) => Math.Clamp(limit <= 0 ? DefaultPageSize : limit, 1, MaxPageSize);
    public static DateTime WeekStart(DateTime utc) {
        utc = utc.ToUniversalTime();
        var days = ((int)utc.DayOfWeek + 6) % 7;
        return utc.Date.AddDays(-days);
    }
}
