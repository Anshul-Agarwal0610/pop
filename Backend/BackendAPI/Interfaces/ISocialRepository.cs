using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface ISocialRepository
{
    Task<PagedResult<SocialUserSummary>> SearchUsersAsync(long actorId, string query, string? cursor, int limit);
    Task<long> SendFriendRequestAsync(long actorId, long targetId);
    Task<PagedResult<FriendConnection>> GetFriendsAsync(long actorId, RelationshipState? state, string? cursor, int limit);
    Task ChangeFriendRequestAsync(long actorId, long relationshipId, bool accept);
    Task RemoveFriendAsync(long actorId, long otherUserId);
    Task BlockAsync(long actorId, long targetId);
    Task UnblockAsync(long actorId, long targetId);
    Task<WeeklyLeaderboard> GetFriendsLeaderboardAsync(long actorId, DateTime week, string? cursor, int limit);
    Task<SocialGroup> CreateGroupAsync(long actorId, string name);
    Task<PagedResult<SocialGroup>> GetGroupsAsync(long actorId, string? cursor, int limit);
    Task<SocialGroup?> GetGroupAsync(long actorId, long groupId);
    Task<string> InviteToGroupAsync(long actorId, long groupId, long targetId);
    Task<GroupInviteSummary?> GetInviteAsync(long actorId, string token);
    Task RespondToInviteAsync(long actorId, string token, bool accept);
    Task LeaveGroupAsync(long actorId, long groupId);
    Task<WeeklyLeaderboard> GetGroupLeaderboardAsync(long actorId, long groupId, DateTime week, string? cursor, int limit);
}
