using BackendAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace BackendAPI.Hubs;
[Authorize]
public sealed class PollClashHub(IPollClashRepository clashes) : Hub
{
 public async Task Watch(long clashId){var value=Context.UserIdentifier??Context.User?.FindFirst("sub")?.Value;if(!long.TryParse(value,out var userId)||!await clashes.IsParticipantAsync(clashId,userId))throw new HubException("Not authorized for this Clash.");await Groups.AddToGroupAsync(Context.ConnectionId,$"clash:{clashId}");}
}
