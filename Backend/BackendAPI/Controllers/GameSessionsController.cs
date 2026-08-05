using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Authorize]
public sealed class GameSessionsController(IGameSessionsRepository sessions, ISystemClock clock) : ControllerBase
{
    [HttpGet("api/game-modes")]
    public IActionResult Modes() => Ok(new[] { new GameModeDto() });

    [HttpPost("api/game-sessions")]
    public async Task<IActionResult> Start(StartGameSessionRequest request) =>
        await Run(() => sessions.StartOrResumeAsync(UserId(), request, clock.UtcNow));

    [HttpGet("api/game-sessions/active")]
    public async Task<IActionResult> Active()
    {
        var session = await sessions.GetActiveAsync(UserId(), clock.UtcNow);
        return session is null ? NoContent() : Ok(session);
    }

    [HttpGet("api/game-sessions/{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var session = await sessions.GetAsync(id, UserId(), clock.UtcNow);
        return session is null ? NotFound(new { code = "not_found", message = "Round not found." }) : Ok(session);
    }

    [HttpPost("api/game-sessions/{id:long}/votes")]
    public async Task<IActionResult> Vote(long id, GameVoteRequest request) =>
        await Run(() => sessions.VoteAsync(id, UserId(), request, clock.UtcNow));

    [HttpPost("api/game-sessions/{id:long}/complete")]
    public async Task<IActionResult> Complete(long id) =>
        await Run(() => sessions.CompleteAsync(id, UserId(), clock.UtcNow));

    private long UserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (claim is null || !long.TryParse(claim.Value, out var id)) throw new UnauthorizedAccessException();
        return id;
    }

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (GameSessionException ex)
        {
            var body = new { ex.Code, message = ex.Message };
            return ex.Code switch
            {
                "not_found" => NotFound(body),
                "already_voted" => Conflict(body),
                "expired" or "poll_unavailable" or "unavailable" or "insufficient_content" => UnprocessableEntity(body),
                _ => BadRequest(body)
            };
        }
    }
}
