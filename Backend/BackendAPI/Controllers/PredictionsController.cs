using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController, Authorize, Route("api/predictions")]
public sealed class PredictionsController(IPredictionsRepository repository, IRewardService rewards, ISystemClock clock) : ControllerBase
{
    [HttpGet("available")]
    public Task<IReadOnlyList<PredictionOpportunity>> Available([FromQuery] string type=PredictionTypes.Winner, [FromQuery] int count=10) => repository.GetAvailableAsync(UserId, type, count, clock.UtcNow);

    [HttpGet]
    public async Task<IReadOnlyList<PredictionEntry>> Mine([FromQuery] int count=50)
    {
        var correct = await repository.SettleMineAsync(UserId, clock.UtcNow);
        foreach (var candidate in correct)
        {
            var grant = await rewards.GrantAsync(new RewardGrantRequest(UserId, candidate.RuleCode, "Prediction", candidate.Id.ToString(), clock.UtcNow));
            await repository.MarkRewardedAsync(candidate.Id, grant.Event.Value);
        }
        return await repository.GetMineAsync(UserId, count, clock.UtcNow);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePredictionRequest request)
    {
        try { return Created("", await repository.CreateAsync(UserId, request, clock.UtcNow)); }
        catch (PredictionException ex)
        {
            var body=new { ex.Code, message=ex.Message };
            return ex.Code=="not_found" ? NotFound(body) : ex.Code=="already_predicted" ? Conflict(body) : UnprocessableEntity(body);
        }
    }

    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
}
