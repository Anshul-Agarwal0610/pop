using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollsController : ControllerBase
    {
        private readonly IPollsRepository _pollsRepo;

        public PollsController(IPollsRepository pollsRepo)
        {
            _pollsRepo = pollsRepo;
        }

        // ── Helper: extract userId from JWT if present (US-16) ────────────────
        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        // GET /api/polls
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var polls = await _pollsRepo.GetAllAsync(CurrentUserId());
            return Ok(polls);
        }

        // GET /api/polls/trending
        [HttpGet("trending")]
        public async Task<IActionResult> GetTrending([FromQuery] int count = 10)
        {
            var polls = await _pollsRepo.GetTrendingAsync(count, CurrentUserId());
            return Ok(polls);
        }

        // GET /api/polls/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var poll = await _pollsRepo.GetByIdAsync(id);
            if (poll == null) return NotFound(new { message = $"Poll {id} not found." });
            return Ok(poll);
        }

        // POST /api/polls
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePollRequest request)
        {
            if (request.Options.Count < 2)
                return BadRequest(new { message = "A poll must have at least 2 options." });

            if (request.ExpiresAt <= DateTime.UtcNow)
                return BadRequest(new { message = "ExpiresAt must be in the future." });

            var id = await _pollsRepo.CreateAsync(request);
            var poll = await _pollsRepo.GetByIdAsync(id);
            return CreatedAtAction(nameof(GetById), new { id }, poll);
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
