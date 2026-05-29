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
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsRepository _notificationsRepo;

        public NotificationsController(INotificationsRepository notificationsRepo)
        {
            _notificationsRepo = notificationsRepo;
        }

        private long? CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }

        // GET /api/notifications
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var notifications = await _notificationsRepo.GetForUserAsync(userId.Value, 30);
            var unreadCount = await _notificationsRepo.GetUnreadCountAsync(userId.Value);
            Response.Headers["X-Unread-Count"] = unreadCount.ToString();
            return Ok(notifications);
        }

        // POST /api/notifications/read-all
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            await _notificationsRepo.MarkAllReadAsync(userId.Value);
            return Ok(new { success = true });
        }

        // PATCH /api/notifications/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkRead(long id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var updated = await _notificationsRepo.MarkReadAsync(userId.Value, id);
            return updated
                ? Ok(new { success = true })
                : NotFound(new { message = $"Notification {id} not found." });
        }

        // GET /api/notifications/preferences
        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var preferences = await _notificationsRepo.GetPreferencesAsync(userId.Value);
            return Ok(preferences);
        }

        // PUT /api/notifications/preferences
        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences(
            [FromBody] UpdateNotificationPreferencesRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });

            var preferences = await _notificationsRepo.ReplacePreferencesAsync(
                userId.Value,
                request.DisabledTypes);
            return Ok(preferences);
        }

        // POST /api/notifications/device-tokens
        [HttpPost("device-tokens")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterPushTokenRequest request)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { message = "Push token is required." });

            await _notificationsRepo.RegisterDeviceTokenAsync(userId.Value, request);
            return Ok(new { success = true });
        }

        // DELETE /api/notifications/device-tokens?token=ExponentPushToken...
        [HttpDelete("device-tokens")]
        public async Task<IActionResult> DisableDeviceToken([FromQuery] string? token)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token." });
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Push token is required." });

            await _notificationsRepo.DisableDeviceTokenAsync(userId.Value, token);
            return Ok(new { success = true });
        }
    }
}
