using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/live-sessions")]
public sealed class LiveSessionsController(
    ILiveSessionsRepository sessions, ILiveSessionNotifier notifier, ISystemClock clock) : ControllerBase
{
    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> Get(Guid sessionId)
    {
        var state = await sessions.GetAsync(sessionId, UserId(), clock.UtcNow);
        if (state is null) return NotFound(Error("not_found", "Live session not found."));
        if (state.Status == LiveSessionStatuses.Revealed)
            await notifier.PublishAsync(Event("roundRevealed", state));
        return Ok(state);
    }

    [HttpPost("{sessionId:guid}/ready")]
    public Task<IActionResult> Ready(Guid sessionId, LiveReadyRequest request) => Run(async () =>
    {
        var state = await sessions.SetReadyAsync(sessionId, UserId(), request.IsReady, clock.UtcNow);
        await notifier.PublishAsync(Event("participantReadyChanged", state));
        return state;
    });

    [HttpPost("{sessionId:guid}/rounds/{round:int}/votes")]
    public Task<IActionResult> Vote(Guid sessionId, int round, LiveVoteRequest request) => Run(async () =>
    {
        var result = await sessions.VoteAsync(sessionId, round, UserId(), request, clock.UtcNow);
        if (!result.WasDuplicate)
            await notifier.PublishAsync(Event(result.RevealScheduled ? "roundRevealScheduled" : "voteLockChanged", result.State));
        return result;
    });

    [HttpPost("{sessionId:guid}/complete")]
    public Task<IActionResult> Complete(Guid sessionId) => Run(async () =>
    {
        var state = await sessions.CompleteAsync(sessionId, UserId(), clock.UtcNow);
        await notifier.PublishAsync(Event("sessionCompleted", state));
        return state;
    });

    private static LiveSessionEvent Event(string type, LiveSessionStateDto state) =>
        new(type, state.SessionId, state.StateVersion, state.ServerNow, state.RevealAt);

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (LiveSessionException ex)
        {
            var body = Error(ex.Code, ex.Message);
            return ex.Code switch
            {
                "not_found" => NotFound(body),
                "idempotency_conflict" or "stale_round" => Conflict(body),
                "round_not_voting" or "invalid_option" => UnprocessableEntity(body),
                _ => BadRequest(body)
            };
        }
    }

    private long UserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private static object Error(string code, string message) => new { code, message };
}
