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

        public VotesController(
            IVotesRepository votesRepo,
            IUsersRepository usersRepo,
            IPollsRepository pollsRepo,
            INotificationsRepository notificationsRepo,
            IChallengesRepository challengesRepo,
            IBusinessRepository businessRepo)
        {
            _votesRepo = votesRepo;
            _usersRepo = usersRepo;
            _pollsRepo = pollsRepo;
            _notificationsRepo = notificationsRepo;
            _challengesRepo = challengesRepo;
            _businessRepo = businessRepo;
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
                var currentUser = await _usersRepo.GetByIdAsync(userId);
                var progression = GamificationRules.FromTotalXp(currentUser?.Xp ?? 0);
                return Conflict(new
                {
                    message = "You have already voted on this poll.",
                    reward = new ProgressionReward
                    {
                        AwardedXp = 0,
                        Progression = progression,
                        PreviousLevel = progression.Level,
                        Streak = currentUser?.Streak ?? 0,
                        TotalVotes = currentUser?.TotalVotes ?? 0,
                        LastVoteDate = currentUser?.LastVoteDate
                    }
                });
            }

            var userBeforeReward = await _usersRepo.GetByIdAsync(userId);

            // US-50: Award XP and apply daily streak rules after a unique vote.
            var voteReward = await _usersRepo.ApplyVoteRewardAsync(
                userId,
                GamificationRules.VoteXp(poll),
                DateTime.UtcNow);
            var challenges = (await _challengesRepo.AdvanceForVoteAsync(userId, poll, DateTime.UtcNow)).ToList();
            var finalUser = await _usersRepo.GetByIdAsync(userId);
            var previousProgression = GamificationRules.FromTotalXp(userBeforeReward?.Xp ?? 0);
            var finalProgression = GamificationRules.FromTotalXp(finalUser?.Xp ?? voteReward.Xp);
            var events = new List<RewardEvent>
            {
                new(RewardEventType.Vote, request.PollId.ToString(), GamificationRules.VoteXp(poll), "Vote")
            };
            events.AddRange(challenges
                .Where(challenge => challenge.AwardedXp > 0)
                .Select(challenge => new RewardEvent(
                    RewardEventType.Challenge,
                    challenge.ChallengeId.ToString(),
                    challenge.AwardedXp,
                    challenge.Title)));
            var achievementXp = Math.Max(0, voteReward.XpAwarded - GamificationRules.VoteXp(poll));
            if (achievementXp > 0)
            {
                events.Add(new RewardEvent(
                    RewardEventType.Achievement,
                    string.Join(",", voteReward.AwardedBadges.Select(badge => badge.BadgeId)),
                    achievementXp,
                    string.Join(", ", voteReward.AwardedBadges.Select(badge => badge.Name))));
            }

            var reward = new ProgressionReward
            {
                AwardedXp = Math.Max(0, finalProgression.TotalXp - previousProgression.TotalXp),
                Progression = finalProgression,
                PreviousLevel = previousProgression.Level,
                Events = events,
                Streak = voteReward.Streak,
                TotalVotes = voteReward.TotalVotes,
                StreakAdvanced = voteReward.StreakAdvanced,
                LastVoteDate = voteReward.LastVoteDate,
                AwardedBadges = voteReward.AwardedBadges
            };

            if (reward.LeveledUp)
            {
                await _notificationsRepo.CreateAsync(new CreateNotificationRequest
                {
                    UserId = userId,
                    Type = NotificationType.LevelUp,
                    Title = "Level up!",
                    Body = $"You reached level {reward.Level} with {reward.Xp:N0} XP.",
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
