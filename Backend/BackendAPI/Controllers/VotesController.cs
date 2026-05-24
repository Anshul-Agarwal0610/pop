using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VotesController : ControllerBase
    {
        private readonly IVotesRepository _votesRepo;
        private readonly IUsersRepository _usersRepo;
        private readonly IPollsRepository _pollsRepo;

        public VotesController(
            IVotesRepository votesRepo,
            IUsersRepository usersRepo,
            IPollsRepository pollsRepo)
        {
            _votesRepo = votesRepo;
            _usersRepo = usersRepo;
            _pollsRepo = pollsRepo;
        }

        // POST /api/votes  (US-15: requires authentication)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CastVote([FromBody] CastVoteRequest request)
        {
            // Extract userId from JWT claims (set by JWT middleware)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "Invalid token." });

            var poll = await _pollsRepo.GetByIdAsync(request.PollId);
            if (poll == null)
                return NotFound(new { message = $"Poll {request.PollId} not found." });

            if (!poll.IsActive || poll.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "This poll has expired or is no longer active." });

            var validOption = poll.Options.Any(o => o.Id == request.OptionId);
            if (!validOption)
                return BadRequest(new { message = "Invalid option for this poll." });

            try
            {
                await _votesRepo.CastVoteAsync(request, userId);
            }
            catch (Exception ex) when (ex.Message.Contains("UQ_Votes_PollUser") ||
                                        ex.Message.Contains("duplicate key") ||
                                        ex.Message.Contains("UNIQUE"))
            {
                return Conflict(new { message = "You have already voted on this poll." });
            }

            // US-50: Award XP and apply daily streak rules after a unique vote.
            var reward = await _usersRepo.ApplyVoteRewardAsync(
                userId,
                GamificationRules.VoteXp(poll),
                DateTime.UtcNow);

            // Return updated poll with hasVoted populated for this user
            var updated = await _pollsRepo.GetByIdAsync(request.PollId);
            if (updated != null)
            {
                updated.HasVoted          = true;
                updated.UserVotedOptionId = request.OptionId;
            }
            return Ok(new CastVoteResponse
            {
                Poll = updated!,
                Reward = reward
            });
        }

        // GET /api/votes/{pollId}
        [HttpGet("{pollId}")]
        public async Task<IActionResult> GetVotesByPoll(long pollId)
        {
            var votes = await _votesRepo.GetVotesByPollAsync(pollId);
            return Ok(votes);
        }
    }
}
