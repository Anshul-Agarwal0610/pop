using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackendAPI.Controllers;

[ApiController, Route("api/multiplayer")]
public sealed class MultiplayerTrustController(IMultiplayerTrustRepository repository) : ControllerBase
{
    private long? UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
    private Guid? ParticipantId => Guid.TryParse(User.FindFirstValue("participant_id"), out var id) ? id : null;

    [HttpPost("reports"), EnableRateLimiting("multiplayer-join")]
    public async Task<IActionResult> Report(CreateSafetyReportRequest request) => Ok(await repository.CreateReportAsync(UserId, ParticipantId, request));

    [Authorize, HttpGet("privacy")]
    public Task<MultiplayerPrivacySettings> Privacy() => repository.GetPrivacyAsync(UserId!.Value);
    [Authorize, HttpPut("privacy")]
    public async Task<IActionResult> Privacy(MultiplayerPrivacySettings settings) { await repository.SavePrivacyAsync(UserId!.Value, settings); return NoContent(); }
    [Authorize, HttpGet("notifications")]
    public Task<MultiplayerNotificationSettings> Notifications() => repository.GetNotificationsAsync(UserId!.Value);
    [Authorize, HttpPut("notifications")]
    public async Task<IActionResult> Notifications(MultiplayerNotificationSettings settings) { await repository.SaveNotificationsAsync(UserId!.Value, settings); return NoContent(); }

    [HttpDelete("live-sessions/{sessionId:guid}/participants/me")]
    public async Task<IActionResult> Leave(Guid sessionId, [FromHeader(Name="X-Reconnect-Capability")] string? capability)
    { await repository.LeaveAsync(sessionId, UserId, capability); return NoContent(); }
}
