using BackendAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChallengesController : ControllerBase
    {
        private readonly IChallengesRepository _challengesRepo;

        public ChallengesController(IChallengesRepository challengesRepo)
        {
            _challengesRepo = challengesRepo;
        }

        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var challenges = await _challengesRepo.GetActiveForUserAsync(userId.Value, DateTime.UtcNow);
            return Ok(challenges);
        }
    }
}
