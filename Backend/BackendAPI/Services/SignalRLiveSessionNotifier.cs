using BackendAPI.Hubs;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.SignalR;

namespace BackendAPI.Services;

public sealed class SignalRLiveSessionNotifier(IHubContext<LiveSessionHub> hub) : ILiveSessionNotifier
{
    public Task StateChangedAsync(string publicId, LiveSessionEventDto @event, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(LiveSessionHub.Group(publicId)).SendAsync("liveSessionEvent", @event, cancellationToken);
}
