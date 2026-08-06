using Microsoft.AspNetCore.SignalR;
namespace BackendAPI.Hubs;
public sealed class LiveRoomHub : Hub
{
    public Task Watch(string roomId,string audience) {
        if(!Guid.TryParse(roomId,out _)||audience is not("host" or "participants" or "display"))throw new HubException("Invalid room audience.");
        return Groups.AddToGroupAsync(Context.ConnectionId,$"room:{roomId}:{audience}");
    }
}
