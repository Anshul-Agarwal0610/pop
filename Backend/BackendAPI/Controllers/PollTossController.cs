using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BackendAPI.Analytics;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/poll-toss/invitations")]
public sealed class PollTossController(PollTossService service, IPollTossInvitationRepository invitations, IOptionsSnapshot<NearbyPollTossOptions> options, IFeatureFlagService flags) : ControllerBase
{
    private long? UserId() => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;
    private bool Enabled(long userId) => options.Value.Enabled && flags.IsEnabled("nearby_poll_toss_v1", userId.ToString(), options.Value.RolloutPercent);

    [HttpPost]
    public async Task<IActionResult> Create(CreatePollTossRequest request)
    {
        var userId=UserId(); if (userId is null) return Unauthorized();
        if (!Enabled(userId.Value)) return NotFound();
        var result=await service.CreateAsync(request.PollId,userId.Value);
        if (result is null) return BadRequest(new { message="Poll is not eligible for nearby toss." });
        var shareUrl=$"{options.Value.ShareBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(result.Value.Token)}";
        return Ok(new PollTossInvitationResponse(result.Value.Invitation.Id,result.Value.Token,result.Value.Invitation.ExpiresAt,shareUrl));
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(RedeemPollTossRequest request)
    {
        var userId=UserId(); if (userId is null) return Unauthorized();
        if (!Enabled(userId.Value)) return NotFound();
        var poll=await service.RedeemAsync(request.InvitationToken,userId.Value);
        return poll is null ? BadRequest(new { message="Invitation is invalid or expired." }) : Ok(poll);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId=UserId(); if (userId is null) return Unauthorized();
        return await invitations.RevokeAsync(id,userId.Value,DateTime.UtcNow) ? NoContent() : NotFound();
    }
}
