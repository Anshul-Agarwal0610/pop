using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BusinessController : ControllerBase
    {
        private readonly IBusinessRepository _businessRepo;
        private readonly IPollsRepository _pollsRepo;

        public BusinessController(IBusinessRepository businessRepo, IPollsRepository pollsRepo)
        {
            _businessRepo = businessRepo;
            _pollsRepo = pollsRepo;
        }

        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var accounts = await _businessRepo.GetBusinessesForUserAsync(userId.Value);
            return Ok(accounts);
        }

        [HttpPost("accounts")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateBusinessAccountRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Business name is required." });

            var account = await _businessRepo.CreateBusinessAsync(userId.Value, request);
            return CreatedAtAction(nameof(GetAccounts), account);
        }

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var campaigns = await _businessRepo.GetCampaignsForUserAsync(userId.Value);
            return Ok(campaigns);
        }

        [HttpPost("accounts/{businessId}/campaigns")]
        public async Task<IActionResult> CreateCampaign(
            long businessId,
            [FromBody] CreateBusinessCampaignRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Campaign name is required." });

            var campaign = await _businessRepo.CreateCampaignAsync(userId.Value, businessId, request);
            return campaign == null
                ? NotFound(new { message = $"Business account {businessId} not found." })
                : CreatedAtAction(nameof(GetCampaigns), campaign);
        }

        [HttpPost("campaigns/{campaignId}/polls")]
        public async Task<IActionResult> CreateSponsoredPoll(
            long campaignId,
            [FromBody] CreateSponsoredPollRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            request.CampaignId = campaignId;
            if (request.Options.Count < 2)
                return BadRequest(new { message = "A poll must have at least 2 options." });
            if (request.ExpiresAt <= DateTime.UtcNow)
                return BadRequest(new { message = "ExpiresAt must be in the future." });

            var pollId = await _businessRepo.CreateSponsoredPollAsync(userId.Value, request);
            if (pollId == null) return NotFound(new { message = $"Campaign {campaignId} not found." });

            var poll = await _pollsRepo.GetByIdAsync(pollId.Value, userId.Value);
            return CreatedAtAction("GetById", "Polls", new { id = pollId.Value }, poll);
        }
    }
}
