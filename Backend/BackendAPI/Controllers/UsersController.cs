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

        public UsersController(IUsersRepository usersRepo)
        {
            _usersRepo = usersRepo;
        }

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
