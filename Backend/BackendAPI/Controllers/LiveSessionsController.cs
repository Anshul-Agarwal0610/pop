using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/live-sessions")]
public sealed class LiveSessionsController(ILiveSessionsRepository sessions, ISystemClock clock) : ControllerBase
{
    [HttpPost] public Task<IActionResult> Create(CreateLiveSessionRequest request) => Run(()=>sessions.CreateAsync(UserId(),request,clock.UtcNow),created:true);
    [HttpGet("{id:long}")] public async Task<IActionResult> Get(long id) => (await sessions.GetAsync(id,UserId())) is { } session ? Ok(session) : NotFound(Error("not_found","Session not found."));
    [HttpGet("{id:long}/events")] public Task<IActionResult> Events(long id,[FromQuery]long afterSequence=0)=>Run(()=>sessions.GetEventsAsync(id,UserId(),afterSequence));
    [HttpPost("{id:long}/participants")] public Task<IActionResult> Join(long id,LiveVersionRequest request)=>Run(()=>sessions.JoinAsync(id,UserId(),request.Version,clock.UtcNow));
    [HttpDelete("{id:long}/participants/me")] public Task<IActionResult> Leave(long id,[FromBody]LiveVersionRequest request)=>Run(()=>sessions.LeaveAsync(id,UserId(),request.Version,clock.UtcNow));
    [HttpPost("{id:long}/start")] public Task<IActionResult> Start(long id,LiveVersionRequest request)=>Run(()=>sessions.StartAsync(id,UserId(),request.Version,clock.UtcNow));
    [HttpPost("{id:long}/rounds/{roundId:long}/responses")] public Task<IActionResult> Respond(long id,long roundId,SubmitLiveResponseRequest request)=>Run(()=>sessions.SubmitResponseAsync(id,roundId,UserId(),request,clock.UtcNow));
    [HttpPost("{id:long}/rounds/{roundId:long}/complete")] public Task<IActionResult> CompleteRound(long id,long roundId,LiveVersionRequest request)=>Run(()=>sessions.CompleteRoundAsync(id,roundId,UserId(),request.Version,clock.UtcNow));
    [HttpPost("{id:long}/complete")] public Task<IActionResult> Complete(long id,LiveVersionRequest request)=>Run(()=>sessions.CompleteAsync(id,UserId(),request.Version,clock.UtcNow));
    [HttpPost("{id:long}/abandon")] public Task<IActionResult> Abandon(long id,LiveVersionRequest request)=>Run(()=>sessions.AbandonAsync(id,UserId(),request.Version,clock.UtcNow));

    private long UserId(){var claim=User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub");return claim is not null&&long.TryParse(claim.Value,out var id)?id:throw new UnauthorizedAccessException();}
    private async Task<IActionResult> Run<T>(Func<Task<T>> command,bool created=false){try{var value=await command();return created?StatusCode(StatusCodes.Status201Created,value):Ok(value);}catch(UnauthorizedAccessException){return Unauthorized();}catch(LiveSessionException ex){var body=Error(ex.Code,ex.Message);return ex.Code switch{"not_found"=>NotFound(body),"forbidden"=>StatusCode(403,body),"stale_version" or "response_conflict"=>Conflict(body),"content_ineligible" or "session_expired"=>UnprocessableEntity(body),_=>BadRequest(body)};}}
    private static object Error(string code,string message)=>new{code,message};
}
