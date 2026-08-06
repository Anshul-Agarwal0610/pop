using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/social")]
public sealed class SocialController(ISocialRepository repository) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw new SocialForbiddenException("Invalid authenticated user.");

    [HttpGet("users")]
    public Task<PagedResult<SocialUserSummary>> Search([FromQuery] string query = "", [FromQuery] string? cursor = null, [FromQuery] int limit = 20) => repository.SearchUsersAsync(UserId, query, cursor, limit);
    [HttpPost("friends/requests")]
    public async Task<IActionResult> Send(SendFriendRequest request) => Created("", new { id = await repository.SendFriendRequestAsync(UserId, request.TargetUserId) });
    [HttpGet("friends")]
    public Task<PagedResult<FriendConnection>> Friends([FromQuery] RelationshipState? state = null, [FromQuery] string? cursor = null, [FromQuery] int limit = 20) => repository.GetFriendsAsync(UserId, state, cursor, limit);
    [HttpPost("friends/requests/{id:long}/accept")]
    public async Task<IActionResult> Accept(long id) { await repository.ChangeFriendRequestAsync(UserId, id, true); return NoContent(); }
    [HttpPost("friends/requests/{id:long}/decline")]
    public async Task<IActionResult> Decline(long id) { await repository.ChangeFriendRequestAsync(UserId, id, false); return NoContent(); }
    [HttpDelete("friends/{userId:long}")]
    public async Task<IActionResult> Remove(long userId) { await repository.RemoveFriendAsync(UserId, userId); return NoContent(); }
    [HttpPost("blocks")]
    public async Task<IActionResult> Block(BlockUserRequest request) { await repository.BlockAsync(UserId, request.TargetUserId); return NoContent(); }
    [HttpDelete("blocks/{userId:long}")]
    public async Task<IActionResult> Unblock(long userId) { await repository.UnblockAsync(UserId, userId); return NoContent(); }
    [HttpGet("leaderboards/friends")]
    public Task<WeeklyLeaderboard> FriendBoard([FromQuery] DateTime? week = null, [FromQuery] string? cursor = null, [FromQuery] int limit = 20) => repository.GetFriendsLeaderboardAsync(UserId, week ?? DateTime.UtcNow, cursor, limit);
    [HttpPost("groups")]
    public async Task<IActionResult> Create(CreateGroupRequest request) => Created("", await repository.CreateGroupAsync(UserId, request.Name));
    [HttpGet("groups")]
    public Task<PagedResult<SocialGroup>> Groups([FromQuery] string? cursor = null, [FromQuery] int limit = 20) => repository.GetGroupsAsync(UserId, cursor, limit);
    [HttpGet("groups/{groupId:long}")]
    public async Task<IActionResult> Group(long groupId) => (await repository.GetGroupAsync(UserId, groupId)) is { } group ? Ok(group) : NotFound();
    [HttpPost("groups/{groupId:long}/invites")]
    public async Task<IActionResult> Invite(long groupId, CreateGroupInviteRequest request) => Created("", new { token = await repository.InviteToGroupAsync(UserId, groupId, request.TargetUserId) });
    [HttpGet("group-invites/{token}")]
    public async Task<IActionResult> Invite(string token) => (await repository.GetInviteAsync(UserId, token)) is { } invite ? Ok(invite) : NotFound();
    [HttpPost("group-invites/{token}/accept")]
    public async Task<IActionResult> AcceptInvite(string token) { await repository.RespondToInviteAsync(UserId, token, true); return NoContent(); }
    [HttpPost("group-invites/{token}/decline")]
    public async Task<IActionResult> DeclineInvite(string token) { await repository.RespondToInviteAsync(UserId, token, false); return NoContent(); }
    [HttpDelete("groups/{groupId:long}/membership")]
    public async Task<IActionResult> Leave(long groupId) { await repository.LeaveGroupAsync(UserId, groupId); return NoContent(); }
    [HttpGet("groups/{groupId:long}/leaderboard")]
    public Task<WeeklyLeaderboard> GroupBoard(long groupId, [FromQuery] DateTime? week = null, [FromQuery] string? cursor = null, [FromQuery] int limit = 20) => repository.GetGroupLeaderboardAsync(UserId, groupId, week ?? DateTime.UtcNow, cursor, limit);
}
