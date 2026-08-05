using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class WellnessController : ControllerBase
    {
        private readonly IWellnessRepository _wellnessRepo;

        public WellnessController(IWellnessRepository wellnessRepo)
        {
            _wellnessRepo = wellnessRepo;
        }

        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            return Ok(await _wellnessRepo.GetOverviewAsync(userId.Value));
        }

        [HttpPost("responses")]
        public async Task<IActionResult> CreateResponse([FromBody] CreateWellnessResponseRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var response = await _wellnessRepo.CreateResponseAsync(userId.Value, request);
            return response == null
                ? NotFound(new { message = "Wellness poll or option not found." })
                : Ok(response);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int count = 30)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var history = await _wellnessRepo.GetHistoryAsync(userId.Value, Math.Clamp(count, 1, 100));
            return Ok(history);
        }

        [HttpGet("export.csv")]
        public async Task<IActionResult> Export()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var history = await _wellnessRepo.GetHistoryAsync(userId.Value, 500);
            var csv = new StringBuilder();
            csv.AppendLine("Question,Response,Note,CreatedAt");

            foreach (var item in history)
            {
                csv.AppendLine(string.Join(",",
                    Csv(item.Question),
                    Csv(item.OptionText),
                    Csv(item.Note),
                    item.CreatedAt.ToString("O")));
            }

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                "wellness-responses.csv");
        }

        [HttpDelete("responses")]
        public async Task<IActionResult> DeleteResponses()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            await _wellnessRepo.DeleteResponsesAsync(userId.Value);
            return NoContent();
        }

        private static string Csv(string? value)
        {
            var text = value ?? string.Empty;
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }
}
