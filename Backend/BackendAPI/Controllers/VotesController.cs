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
        private readonly INotificationsRepository _notificationsRepo;
        private readonly IChallengesRepository _challengesRepo;
        private readonly IBusinessRepository _businessRepo;
        private readonly IAchievementsRepository _achievementsRepo;

        public VotesController(
            IVotesRepository votesRepo,
            IUsersRepository usersRepo,
            IPollsRepository pollsRepo,
            INotificationsRepository notificationsRepo,
            IChallengesRepository challengesRepo,
            IBusinessRepository businessRepo,
            IAchievementsRepository achievementsRepo)
        {
            _votesRepo = votesRepo;
            _usersRepo = usersRepo;
            _pollsRepo = pollsRepo;
            _notificationsRepo = notificationsRepo;
            _challengesRepo = challengesRepo;
            _businessRepo = businessRepo;
            _achievementsRepo = achievementsRepo;
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

            if (!poll.ModerationStatus.Equals(PollModerationStatus.Published, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "This poll is not open for voting." });

            if (poll.IsWellness || poll.IsPrivate || poll.Category.Equals("Health", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Health and wellness check-ins use private wellness responses." });

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

            var userBeforeReward = await _usersRepo.GetByIdAsync(userId);

            // US-50: Award XP and apply daily streak rules after a unique vote.
            var reward = await _usersRepo.ApplyVoteRewardAsync(
                userId,
                GamificationRules.VoteXp(poll),
                DateTime.UtcNow);
            var challenges = await _challengesRepo.AdvanceForVoteAsync(userId, poll, DateTime.UtcNow);
            var awards = await _achievementsRepo.AwardEligibleBadgesAsync(userId, DateTime.UtcNow);
            reward.AwardedBadges = awards.AwardedBadges;
            reward.Xp += awards.BonusXpAwarded;
            reward.XpAwarded += awards.BonusXpAwarded;

            if (userBeforeReward != null && userBeforeReward.Xp / 1000 < reward.Xp / 1000)
            {
                var level = reward.Xp / 1000;
                await _notificationsRepo.CreateAsync(new CreateNotificationRequest
                {
                    UserId = userId,
                    Type = NotificationType.LevelUp,
                    Title = "Level up!",
                    Body = $"You reached level {level} with {reward.Xp:N0} XP.",
                    PollId = null
                });
            }

            // Return updated poll with hasVoted populated for this user
            var updated = await _pollsRepo.GetByIdAsync(request.PollId);
            if (updated != null)
            {
                await CreateVoteMilestoneNotificationAsync(poll, updated.TotalVotes);
                if (updated.IsSponsored)
                {
                    await _businessRepo.RecordVoteAsync(updated.Id);
                }
            }

            if (updated != null)
            {
                updated.HasVoted          = true;
                updated.UserVotedOptionId = request.OptionId;
            }
            return Ok(new CastVoteResponse
            {
                Poll = updated!,
                Reward = reward,
                Challenges = challenges
            });
        }

        private async Task CreateVoteMilestoneNotificationAsync(Poll pollBeforeVote, int updatedTotalVotes)
        {
            if (pollBeforeVote.CreatedByUserId == null) return;

            var milestones = new[] { 10, 50, 100, 500, 1000 };
            var crossed = milestones.FirstOrDefault(
                milestone => pollBeforeVote.TotalVotes < milestone && updatedTotalVotes >= milestone);

            if (crossed == 0) return;

            await _notificationsRepo.CreateAsync(new CreateNotificationRequest
            {
                UserId = pollBeforeVote.CreatedByUserId.Value,
                Type = NotificationType.VoteMilestone,
                Title = "Vote milestone reached",
                Body = $"Your poll reached {crossed:N0} votes: {pollBeforeVote.Question}",
                PollId = pollBeforeVote.Id
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
