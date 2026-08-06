using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/rewards")]
public sealed class RewardsController : ControllerBase
{
    private readonly IRewardRepository _rewards;
    public RewardsController(IRewardRepository rewards) => _rewards = rewards;

    [HttpGet("configuration")]
    public async Task<ActionResult<RewardConfiguration>> GetConfiguration(CancellationToken cancellationToken)
        => Ok(new RewardConfiguration { Rules = await _rewards.GetActiveRulesAsync(DateTime.UtcNow, cancellationToken) });
}
