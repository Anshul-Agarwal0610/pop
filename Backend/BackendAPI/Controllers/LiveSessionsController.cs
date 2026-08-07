using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class LiveSessionsController(ILiveSessionsRepository sessions, IOptions<PollBombOptions> options, ISystemClock clock) : ControllerBase
{
    private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("live-session-modes")]
    public ActionResult<IReadOnlyList<LiveSessionModeDto>> Modes()
    {
        var o=options.Value;
        return Ok(new[] { new LiveSessionModeDto("Bomb",o.AllowedThresholds,o.AllowedDurationsSeconds,[nameof(PollBombExpiryPolicy.ExpireWithoutReveal)],PollBombRules.Capacity(o,o.AllowedThresholds.Max()),true) });
    }

    [HttpPost("live-sessions")]
    public Task<ActionResult<LiveSessionStateDto>> Create(CreateLiveSessionRequest request)=>Run(()=>sessions.CreateAsync(UserId,request,clock.UtcNow),created:true);
    [HttpGet("live-sessions/{publicId}")]
    public async Task<ActionResult<LiveSessionStateDto>> Get(string publicId){var value=await sessions.GetAsync(publicId,UserId,clock.UtcNow);return value is null?NotFound():Ok(value);}
    [HttpPost("live-sessions/{publicId}/join")]
    public Task<ActionResult<LiveSessionStateDto>> Join(string publicId)=>Run(()=>sessions.JoinAsync(publicId,UserId,clock.UtcNow));
    [HttpPost("live-sessions/{publicId}/votes")]
    public Task<ActionResult<LiveSessionStateDto>> Vote(string publicId,LockLiveSessionVoteRequest request)=>Run(()=>sessions.VoteAsync(publicId,UserId,request,clock.UtcNow));
    [HttpDelete("live-sessions/{publicId}/participants/{participantId:long}")]
    public Task<ActionResult<LiveSessionStateDto>> Remove(string publicId,long participantId)=>Run(()=>sessions.RemoveAsync(publicId,UserId,participantId,clock.UtcNow));
    [HttpPut("live-sessions/{publicId}/notifications")]
    public Task<ActionResult<LiveSessionStateDto>> Notifications(string publicId,SetLiveSessionNotificationsRequest request)=>Run(()=>sessions.SetNotificationsAsync(publicId,UserId,request.Enabled,clock.UtcNow));
    [HttpGet("live-sessions/{publicId}/events")]
    public async Task<ActionResult<IReadOnlyList<LiveSessionEventDto>>> Events(string publicId,[FromQuery]long afterSequence=0){try{return Ok(await sessions.EventsAsync(publicId,UserId,afterSequence,clock.UtcNow));}catch(LiveSessionException ex){return Error(ex);}}

    private async Task<ActionResult<LiveSessionStateDto>> Run(Func<Task<LiveSessionStateDto>> action,bool created=false){try{var state=await action();return created?CreatedAtAction(nameof(Get),new{publicId=state.PublicId},state):Ok(state);}catch(LiveSessionException ex){return Error(ex);}}
    private ActionResult Error(LiveSessionException ex)=>ex.Code switch {"not_found"=>NotFound(new{ex.Code,ex.Message}),"forbidden"=>StatusCode(403,new{ex.Code,ex.Message}),"invalid_configuration" or "invalid_option" or "poll_unavailable"=>BadRequest(new{ex.Code,ex.Message}),_=>Conflict(new{ex.Code,ex.Message})};
}
