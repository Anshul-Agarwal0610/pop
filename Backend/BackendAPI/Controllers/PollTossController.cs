using System.Security.Claims; using BackendAPI.Interfaces; using BackendAPI.Models; using BackendAPI.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace BackendAPI.Controllers;
[ApiController,Route("api/poll-tosses"),Authorize]
public sealed class PollTossController(IPollTossRepository repo,ISystemClock clock):ControllerBase {
 long? UserId(){var v=User.FindFirst(ClaimTypes.NameIdentifier)?.Value??User.FindFirst("sub")?.Value;return long.TryParse(v,out var id)?id:null;}
 [HttpPost] public async Task<IActionResult> Create(CreatePollTossRequest request){var uid=UserId();if(uid is null)return Unauthorized();try{var (i,t)=await repo.CreateAsync(request.PollId,uid.Value,clock.UtcNow);return CreatedAtAction(nameof(Get),new{id=i.Id},new CreatedPollTossResponse(i.Id,i.PollId,i.Status,i.StateVersion,i.ExpiresAt,t,i.RoomCode,i.Poll));}catch(PollTossException e){return BadRequest(new{code=e.Code,message=e.Message});}}
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id){var i=await repo.GetForSenderAsync(id,UserId()??0,clock.UtcNow);return i is null?NotFound():Ok(i);}
 [HttpGet("invite/{token}")] public async Task<IActionResult> Preview(string token){var i=await repo.PreviewByTokenAsync(token,clock.UtcNow);return i is null?NotFound():Ok(i);}
 [HttpGet("room/{code}")] public async Task<IActionResult> Room(string code){var i=await repo.PreviewByRoomCodeAsync(code,clock.UtcNow);return i is null?NotFound():Ok(i);}
 [HttpPost("invite/{token}/accept")] public async Task<IActionResult> Accept(string token)=>await Transition(()=>repo.AcceptAsync(token,UserId()??0,clock.UtcNow));
 [HttpPost("{id:guid}/cancel")] public async Task<IActionResult> Cancel(Guid id)=>await Transition(()=>repo.CancelAsync(id,UserId()??0,clock.UtcNow));
 async Task<IActionResult> Transition(Func<Task<PollTossInvitation>> action){try{return Ok(await action());}catch(PollTossException e){return Conflict(new{code=e.Code,message=e.Message});}}
}
