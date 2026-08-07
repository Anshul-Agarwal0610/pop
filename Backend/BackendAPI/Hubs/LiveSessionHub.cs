using BackendAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackendAPI.Hubs;

[Authorize]
public sealed class LiveSessionHub(ILiveSessionsRepository sessions) : Hub
{
    public async Task Subscribe(string publicId)
    {
        if (!long.TryParse(Context.UserIdentifier, out var userId) || await sessions.GetAsync(publicId, userId, DateTime.UtcNow) is null)
            throw new HubException("Session not found.");
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(publicId));
    }

    internal static string Group(string publicId) => $"live:{publicId}";
}
