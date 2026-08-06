using BackendAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/achievements")]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly IAchievementsRepository _repository;
        public AchievementsController(IAchievementsRepository repository) => _repository = repository;

        [HttpGet("me/overview")]
        public async Task<IActionResult> GetMyOverview()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null || !long.TryParse(claim.Value, out var userId)) return Unauthorized();
            return Ok(await _repository.GetOverviewAsync(userId));
        }
    }
}
