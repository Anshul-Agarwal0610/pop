using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers;

[ApiController, Route("api/achievements"), Authorize]
public class AchievementsController(IAchievementsRepository repository) : ControllerBase
{
    private long? UserId => long.TryParse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))?.Value, out var id) ? id : null;
    [HttpGet("me")] public async Task<IActionResult> GetMine() => UserId is long id ? Ok(await repository.GetCollectionAsync(id)) : Unauthorized();
    [HttpGet("me/overview")] public async Task<IActionResult> GetOverview() => UserId is long id ? Ok(await repository.GetOverviewAsync(id)) : Unauthorized();
    [HttpPost("me/celebrations/claim")] public async Task<IActionResult> Claim() => UserId is long id ? Ok(await repository.ClaimPendingCelebrationsAsync(id, DateTime.UtcNow)) : Unauthorized();
    [HttpPut("me/title")] public async Task<IActionResult> Select([FromBody] SelectProfileTitleRequest request)
    { if (UserId is not long id) return Unauthorized(); return await repository.SelectTitleAsync(id, request.BadgeId) ? NoContent() : BadRequest(new { message="Only an earned achievement title can be selected." }); }
    [HttpDelete("me/title")] public async Task<IActionResult> Clear()
    { if (UserId is not long id) return Unauthorized(); await repository.ClearTitleAsync(id); return NoContent(); }
}
