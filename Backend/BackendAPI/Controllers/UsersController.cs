using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersRepository _usersRepo;
        private readonly IAchievementsRepository _achievementsRepo;

        public UsersController(IUsersRepository usersRepo, IAchievementsRepository achievementsRepo)
        {
            _usersRepo = usersRepo;
            _achievementsRepo = achievementsRepo;
        }

        [HttpGet("{id}/achievements")]
        public async Task<IActionResult> GetAchievements(long id) => Ok(await _achievementsRepo.GetPublicAchievementsAsync(id));

        // ── Helper ────────────────────────────────────────────────────────────
        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        // GET /api/users/leaderboard  (US-23)
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard([FromQuery] int count = 20)
        {
            var users = await _usersRepo.GetLeaderboardAsync(count);
            return Ok(users);
        }

        // Competition ranking: ties share a rank (1, 2, 2, 4), then username and id
        // provide stable display order. Weekly boundaries are supplied by the server in UTC.
        [HttpGet("leaderboard/rankings")]
        public async Task<IActionResult> GetRankings(
            [FromQuery] string period = "weekly",
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0)
        {
            if (!TryParsePeriod(period, out var parsed))
                return BadRequest(new { message = "period must be 'weekly' or 'allTime'." });

            limit = Math.Clamp(limit, 1, 100);
            offset = Math.Max(0, offset);
            return Ok(await _usersRepo.GetRankingsAsync(parsed, limit, offset, CurrentUserId(), DateTime.UtcNow));
        }

        internal static bool TryParsePeriod(string value, out LeaderboardPeriod period)
        {
            if (value.Equals("weekly", StringComparison.OrdinalIgnoreCase))
            { period = LeaderboardPeriod.Weekly; return true; }
            if (value.Equals("allTime", StringComparison.OrdinalIgnoreCase) || value.Equals("all-time", StringComparison.OrdinalIgnoreCase))
            { period = LeaderboardPeriod.AllTime; return true; }
            period = default;
            return false;
        }

        // GET /api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var user = await _usersRepo.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = $"User {id} not found." });
            return Ok(user);
        }

        // GET /api/users/me/votes  (US-22)
        [HttpGet("me/votes")]
        [Authorize]
        public async Task<IActionResult> GetMyVotes([FromQuery] int count = 10)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var history = await _usersRepo.GetVoteHistoryAsync(userId.Value, count);
            return Ok(history);
        }

        [HttpGet("me/streak")]
        [Authorize]
        public async Task<IActionResult> GetMyStreak()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();
            var status = await _usersRepo.GetStreakStatusAsync(userId.Value, DateTime.UtcNow);
            return status == null ? NotFound() : Ok(status);
        }

        [HttpGet("me/progression")]
        [Authorize]
        public async Task<IActionResult> GetMyProgression()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();
            var progression = await _usersRepo.GetProgressionAsync(userId.Value, DateTime.UtcNow);
            return progression == null ? NotFound() : Ok(progression);
        }

        [HttpGet("leaderboard/weekly")]
        [Authorize]
        public async Task<IActionResult> GetWeeklyLeaderboard([FromQuery] int count = 5)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();
            return Ok(await _usersRepo.GetWeeklyLeaderboardAsync(userId.Value, count, DateTime.UtcNow));
        }

        // GET /api/users/me/preferences/categories
        [HttpGet("me/preferences/categories")]
        [Authorize]
        public async Task<IActionResult> GetMyCategoryPreferences()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var preferences = await _usersRepo.GetCategoryPreferencesAsync(userId.Value);
            return Ok(preferences);
        }

        // PUT /api/users/me/preferences/categories
        [HttpPut("me/preferences/categories")]
        [Authorize]
        public async Task<IActionResult> UpdateMyCategoryPreferences(
            [FromBody] UpdateCategoryPreferencesRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var preferences = await _usersRepo.ReplaceCategoryPreferencesAsync(
                userId.Value,
                request.Categories);
            return Ok(preferences);
        }

        // DELETE /api/users/me/preferences/categories
        [HttpDelete("me/preferences/categories")]
        [Authorize]
        public async Task<IActionResult> ResetMyCategoryPreferences()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            await _usersRepo.ResetCategoryPreferencesAsync(userId.Value);
            return NoContent();
        }

        // POST /api/users
        [HttpGet("me/privacy")]
        [Authorize]
        public async Task<IActionResult> GetMyPrivacy()
        {
            var userId = CurrentUserId(); if (userId == null) return Unauthorized();
            return Ok(await _usersRepo.GetAnalyticsPrivacyAsync(userId.Value));
        }

        [HttpPut("me/privacy")]
        [Authorize]
        public async Task<IActionResult> UpdateMyPrivacy([FromBody] UpdateAnalyticsPrivacyRequest request)
        {
            var userId = CurrentUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _usersRepo.UpdateAnalyticsPrivacyAsync(userId.Value, request.Consent)); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // POST /api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var existing = await _usersRepo.GetByUsernameAsync(request.Username);
            if (existing != null)
                return Conflict(new { message = "Username already taken." });

            var id = await _usersRepo.CreateAsync(request);
            var user = await _usersRepo.GetByIdAsync(id);
            return CreatedAtAction(nameof(GetById), new { id }, user);
        }
    }
}
