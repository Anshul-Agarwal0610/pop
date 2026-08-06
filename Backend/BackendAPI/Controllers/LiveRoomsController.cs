using System.Security.Claims; using BackendAPI.Hubs; using BackendAPI.Interfaces; using BackendAPI.Models; using BackendAPI.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.SignalR;
namespace BackendAPI.Controllers;
[ApiController] public sealed class LiveRoomsController(ILiveRoomsRepository rooms,ISystemClock clock,IHubContext<LiveRoomHub> hub):ControllerBase
{
 [Authorize,HttpPost("api/live-rooms")] public Task<IActionResult> Create(CreateLiveRoomRequest q)=>Run(async()=>await rooms.CreateAsync(UserId(),q,clock.UtcNow));
 [Authorize,HttpGet("api/live-rooms/{id:guid}/host")] public Task<IActionResult> Host(Guid id)=>Run(async()=>await rooms.HostAsync(id,UserId(),clock.UtcNow));
 [AllowAnonymous,HttpPost("api/live-rooms/join")] public Task<IActionResult> Join(JoinLiveRoomRequest q)=>Run(async()=>{var x=await rooms.JoinAsync(q,clock.UtcNow);await Changed(x.RoomId,x.Snapshot.Version);return x;});
 [AllowAnonymous,HttpGet("api/live-rooms/{id:guid}/participant")] public Task<IActionResult> Participant(Guid id)=>Run(async()=>await rooms.ParticipantAsync(id,RoomToken(),clock.UtcNow));
 [AllowAnonymous,HttpPost("api/live-rooms/{id:guid}/votes")] public Task<IActionResult> Vote(Guid id,LiveVoteRequest q)=>Run(async()=>{var x=await rooms.VoteAsync(id,RoomToken(),q,clock.UtcNow);await Changed(id,x.Version);return x;});
 [AllowAnonymous,HttpGet("api/live-rooms/{id:guid}/display")] public Task<IActionResult> Display(Guid id,[FromQuery]string capability)=>Run(async()=>await rooms.DisplayAsync(id,capability,clock.UtcNow));
 [Authorize,HttpPost("api/live-rooms/{id:guid}/{command:start|pause|resume|close|advance|end}")] public Task<IActionResult> Command(Guid id,string command)=>Run(async()=>{var x=await rooms.CommandAsync(id,UserId(),command,null,clock.UtcNow);await Changed(id,x.Version);return x;});
 [Authorize,HttpDelete("api/live-rooms/{id:guid}/participants/{participantId:guid}")] public Task<IActionResult> Remove(Guid id,Guid participantId)=>Run(async()=>{var x=await rooms.CommandAsync(id,UserId(),"remove",participantId,clock.UtcNow);await Changed(id,x.Version);return x;});
 async Task Changed(Guid id,long version){foreach(var a in new[]{"host","participants","display"})await hub.Clients.Group($"room:{id}:{a}").SendAsync("roomChanged",version);}
 string RoomToken()=>Request.Headers["X-Room-Token"].FirstOrDefault()??throw new UnauthorizedAccessException();
 long UserId(){var c=User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub");return c is not null&&long.TryParse(c.Value,out var id)?id:throw new UnauthorizedAccessException();}
 async Task<IActionResult> Run<T>(Func<Task<T>> f){try{return Ok(await f());}catch(UnauthorizedAccessException){return Unauthorized();}catch(Exception e)when(e is LiveRoomException or PollPackException){var code=e is LiveRoomException l?l.Code:((PollPackException)e).Code;var b=new{code,message=e.Message};return code switch{"not_found"=>NotFound(b),"capacity" or "already_voted"=>Conflict(b),"expired" or "ended" or "late_join"=>UnprocessableEntity(b),_=>BadRequest(b)};}}
}
