using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollsController : ControllerBase
    {
        private readonly IPollsRepository _pollsRepo;
        private readonly IConfiguration _config;

        public PollsController(IPollsRepository pollsRepo, IConfiguration config)
        {
            _pollsRepo = pollsRepo;
            _config = config;
        }

        // ── Helper: extract userId from JWT if present (US-16) ────────────────
        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        private bool CurrentUserCanModerate(long userId)
        {
            if (_config.GetValue<bool>("Moderation:AllowAuthenticatedReviewers"))
                return true;

            var reviewerIds = _config["Moderation:ReviewerUserIds"];
            if (string.IsNullOrWhiteSpace(reviewerIds)) return false;

            return reviewerIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(value => long.TryParse(value, out var reviewerId) && reviewerId == userId);
        }

        // GET /api/polls
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? category = null)
        {
            var polls = await _pollsRepo.GetAllAsync(CurrentUserId(), category);
            return Ok(polls);
        }

        // GET /api/polls/trending
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending(
            [FromQuery] int count = 10,
            [FromQuery] string? category = null)
        {
            var polls = await _pollsRepo.GetTrendingAsync(count, CurrentUserId(), category);
            return Ok(polls);
        }

        // GET /api/polls/personalized
        [HttpGet("personalized")]
        public async Task<IActionResult> GetPersonalized(
            [FromQuery] int count = 20,
            [FromQuery] string? category = null)
        {
            var polls = await _pollsRepo.GetPersonalizedAsync(
                CurrentUserId(),
                Math.Clamp(count, 1, 50),
                category);
            return Ok(polls);
        }

        // GET /api/polls/search?q=keyword
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? q,
            [FromQuery] string? category = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Search query is required." });

            var polls = await _pollsRepo.SearchAsync(q, category, CurrentUserId());
            return Ok(polls);
        }

        // GET /api/polls/moderation
        [HttpGet("moderation")]
        [Authorize]
        public async Task<IActionResult> GetModerationQueue(
            [FromQuery] string? status = null,
            [FromQuery] int count = 50)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (!CurrentUserCanModerate(userId.Value)) return Forbid();

            var polls = await _pollsRepo.GetModerationQueueAsync(status, Math.Clamp(count, 1, 100));
            return Ok(polls);
        }

        // GET /api/polls/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var poll = await _pollsRepo.GetByIdAsync(id, CurrentUserId());
            if (poll == null) return NotFound(new { message = $"Poll {id} not found." });
            return Ok(poll);
        }

        // POST /api/polls
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreatePollRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            if (request.Options.Count < 2)
                return BadRequest(new { message = "A poll must have at least 2 options." });

            if (request.ExpiresAt <= DateTime.UtcNow)
                return BadRequest(new { message = "ExpiresAt must be in the future." });

            var id = await _pollsRepo.CreateAsync(request, userId);
            var poll = await _pollsRepo.GetByIdAsync(id, userId);
            return CreatedAtAction(nameof(GetById), new { id }, poll);
        }

        // POST /api/polls/{id}/report
        [HttpPost("{id}/report")]
        [Authorize]
        public async Task<IActionResult> Report(long id, [FromBody] ReportPollRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var success = await _pollsRepo.ReportAsync(id, userId.Value, request.Reason);
            if (!success) return NotFound(new { message = $"Poll {id} not found." });

            return Ok(new { message = "Poll reported for review." });
        }

        // PATCH /api/polls/{id}/moderation
        [HttpPatch("{id}/moderation")]
        [Authorize]
        public async Task<IActionResult> Moderate(long id, [FromBody] ModeratePollRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (!CurrentUserCanModerate(userId.Value)) return Forbid();

            var status = PollModerationStatus.Normalize(request.Status);
            if (status == PollModerationStatus.Rejected && string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "A rejection reason is required." });

            var success = await _pollsRepo.ModerateAsync(id, status, request.Reason, userId.Value);
            if (!success) return NotFound(new { message = $"Poll {id} not found." });

            var poll = await _pollsRepo.GetByIdAsync(id, userId.Value);
            return Ok(poll);
        }

        // DELETE /api/polls/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _pollsRepo.DeleteAsync(id);
            if (!success) return NotFound(new { message = $"Poll {id} not found." });
            return NoContent();
        }
    }
}
