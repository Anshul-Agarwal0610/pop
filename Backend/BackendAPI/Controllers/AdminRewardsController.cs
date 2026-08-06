using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/admin/rewards")]
[Authorize(Policy = "Admin")]
public sealed class AdminRewardsController : ControllerBase
{
    private readonly IRewardRepository _repository;
    private readonly IRewardService _service;
    public AdminRewardsController(IRewardRepository repository, IRewardService service) { _repository=repository; _service=service; }

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] long? userId,[FromQuery] int count=100,CancellationToken token=default)
        => Ok(await _repository.GetEventsAsync(userId,count,token));

    [HttpGet("suspicious")]
    public async Task<IActionResult> Suspicious([FromQuery] int hours=24,[FromQuery] int minimumEvents=50,CancellationToken token=default)
        => Ok(await _repository.GetSuspiciousAsync(DateTime.UtcNow.AddHours(-Math.Clamp(hours,1,24*30)),Math.Clamp(minimumEvents,2,10000),token));

    [HttpGet("reconciliation")]
    public async Task<IActionResult> Reconciliation(CancellationToken token) => Ok(await _repository.GetReconciliationAsync(token));

    [HttpPost("{eventId:long}/reverse")]
    public async Task<IActionResult> Reverse(long eventId,[FromBody] ReverseRewardRequest request,CancellationToken token)
        => Ok(await _service.ReverseAsync(eventId,ActorId(),request.Reason,request.IdempotencyKey,token));

    [HttpPost("adjustments")]
    public async Task<IActionResult> Adjust([FromBody] ManualAdjustmentRequest request,CancellationToken token)
        => Ok(await _service.AdjustAsync(request.UserId,request.Value,ActorId(),request.Reason,request.IdempotencyKey,token));

    private long ActorId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
