using System.Security.Claims;
using BackendAPI.Hubs;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
namespace BackendAPI.Controllers;
[ApiController,Authorize,Route("api/poll-clashes")]
public sealed class PollClashesController(IPollClashRepository clashes,ISystemClock clock,IHubContext<PollClashHub> hub):ControllerBase
{
 [HttpPost] public Task<IActionResult> Create(CreatePollClashRequest request)=>Run(()=>clashes.CreateAsync(UserId(),request,clock.UtcNow));
 [HttpGet("{id:long}")] public async Task<IActionResult> Get(long id){try{var value=await clashes.GetAsync(id,UserId(),clock.UtcNow);return value is null?NotFound():Ok(value);}catch(UnauthorizedAccessException){return Forbid();}}
 [HttpGet("invite/{code}")] public async Task<IActionResult> Invite(string code){var value=await clashes.GetInviteAsync(code,UserId(),clock.UtcNow);return value is null?NotFound():Ok(value);}
 [HttpPost("{id:long}/join")] public Task<IActionResult> Join(long id)=>Run(async()=>{var x=await clashes.JoinAsync(id,UserId(),clock.UtcNow);await Notify(id,"playerJoined");return x;});
 [HttpPost("{id:long}/responses")] public Task<IActionResult> Respond(long id,PollClashResponseRequest request)=>Run(async()=>{var x=await clashes.RespondAsync(id,UserId(),request,clock.UtcNow);await Notify(id,x.Status==PollClashStatuses.Completed?"clashCompleted":x.Rounds.Any(r=>r.Id==request.RoundId&&r.Status==PollClashRoundStatuses.Revealed)?"roundRevealed":"opponentSubmitted");return x;});
 [HttpPost("{id:long}/rematch-requests")] public Task<IActionResult> RequestRematch(long id)=>Run(async()=>{var x=await clashes.RequestRematchAsync(id,UserId(),clock.UtcNow);await Notify(id,"rematchRequested");return x;});
 [HttpPost("{id:long}/rematch-requests/{requestId:long}/accept")] public Task<IActionResult> Accept(long id,long requestId)=>Run(async()=>{var x=await clashes.AcceptRematchAsync(id,requestId,UserId(),clock.UtcNow);await Notify(id,"rematchAccepted");return x;});
 [HttpPost("{id:long}/rematch-requests/{requestId:long}/decline")] public Task<IActionResult> Decline(long id,long requestId)=>Run(()=>clashes.DeclineRematchAsync(id,requestId,UserId(),clock.UtcNow));
 private Task Notify(long id,string name)=>hub.Clients.Group($"clash:{id}").SendAsync("stateChanged",new{name});
 private long UserId(){var claim=User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub");return claim is not null&&long.TryParse(claim.Value,out var id)?id:throw new UnauthorizedAccessException();}
 private async Task<IActionResult> Run<T>(Func<Task<T>> action){try{return Ok(await action());}catch(UnauthorizedAccessException){return Forbid();}catch(PollClashException ex){var body=new{ex.Code,message=ex.Message};return ex.Code switch{"not_found" or "round_not_found"=>NotFound(body),"already_submitted" or "clash_full" or "rematch_pending" or "rematch_resolved"=>Conflict(body),"poll_ineligible" or "insufficient_content" or "invite_unavailable"=>UnprocessableEntity(body),_=>BadRequest(body)};}}
}
