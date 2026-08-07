using System.Collections.Concurrent;
using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackendAPI.Hubs;

[Authorize]
public sealed class LiveSessionHub(ILiveSessionsRepository sessions, ISystemClock clock) : Hub
{
    private static readonly ConcurrentDictionary<string, (Guid SessionId, long UserId)> Connections = new();

    public async Task JoinSession(Guid sessionId)
    {
        var userId = UserId();
        if (!await sessions.IsMemberAsync(sessionId, userId))
            throw new HubException("You are not a member of this private session.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
        Connections[Context.ConnectionId] = (sessionId, userId);
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("liveSessionEvent",
            new LiveSessionEvent("participantJoined", sessionId, 0, clock.UtcNow));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var connection))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(connection.SessionId));
            if (!Connections.Values.Any(x => x == connection))
                await Clients.Group(GroupName(connection.SessionId)).SendAsync("liveSessionEvent",
                    new LiveSessionEvent("participantLeft", connection.SessionId, 0, clock.UtcNow));
        }
        await base.OnDisconnectedAsync(exception);
    }

    internal static string GroupName(Guid sessionId) => $"live:{sessionId:N}";

    private long UserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return long.TryParse(value, out var id) ? id : throw new HubException("Authentication required.");
    }
}

public sealed class SignalRLiveSessionNotifier(IHubContext<LiveSessionHub> hub) : ILiveSessionNotifier
{
    public Task PublishAsync(LiveSessionEvent message, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(LiveSessionHub.GroupName(message.SessionId))
            .SendAsync("liveSessionEvent", message, cancellationToken);
}
