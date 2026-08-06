using System.Security.Claims;
using BackendAPI.Analytics;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/mobile/experiments")]
public sealed class MobileExperimentsController(IOptionsSnapshot<NearbyPollTossOptions> options, IFeatureFlagService flags) : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true)]
    public IActionResult Get()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
        var value = options.Value;
        var enabled = value.Enabled && subject.Length > 0 && flags.IsEnabled("nearby_poll_toss_v1", subject, value.RolloutPercent);
        return Ok(new { nearbyPollToss = new { enabled, discoveryTimeoutSeconds=Math.Clamp(value.DiscoveryTimeoutSeconds,15,120), invitationTtlSeconds=Math.Clamp(value.InvitationTtlSeconds,30,300) } });
    }
}
