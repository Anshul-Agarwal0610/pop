using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Text;
using System.Text.Json;

namespace BackendAPI.Services
{
    public class ExpoPushNotificationService : IPushNotificationService
    {
        private const string ExpoPushEndpoint = "https://exp.host/--/api/v2/push/send";

        private readonly HttpClient _httpClient;
        private readonly INotificationsRepository _notificationsRepo;
        private readonly ILogger<ExpoPushNotificationService> _logger;

        public ExpoPushNotificationService(
            HttpClient httpClient,
            INotificationsRepository notificationsRepo,
            ILogger<ExpoPushNotificationService> logger)
        {
            _httpClient = httpClient;
            _notificationsRepo = notificationsRepo;
            _logger = logger;
        }

        public async Task<int> SendPendingAsync(int count = 100)
        {
            var candidates = (await _notificationsRepo.GetPendingPushNotificationsAsync(count)).ToList();
            var sent = 0;

            foreach (var candidate in candidates)
            {
                var payload = new
                {
                    to = candidate.Token,
                    title = candidate.Title,
                    body = candidate.Body,
                    sound = "default",
                    data = new
                    {
                        notificationId = candidate.NotificationId,
                        type = candidate.Type.ToString(),
                        pollId = candidate.PollId,
                        path = candidate.PollId.HasValue ? $"/polls/{candidate.PollId.Value}" : "/notifications"
                    }
                };

                try
                {
                    using var content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json");
                    using var response = await _httpClient.PostAsync(ExpoPushEndpoint, content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    var success = response.IsSuccessStatusCode
                        && !responseText.Contains("\"status\":\"error\"", StringComparison.OrdinalIgnoreCase);

                    await _notificationsRepo.MarkPushAttemptAsync(
                        candidate.NotificationId,
                        candidate.DeviceTokenId,
                        success,
                        TryReadExpoId(responseText),
                        success ? null : responseText[..Math.Min(1000, responseText.Length)]);

                    if (success)
                    {
                        sent++;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[ExpoPush] Failed to send notification {NotificationId} to device token {DeviceTokenId}. Response={Response}",
                            candidate.NotificationId,
                            candidate.DeviceTokenId,
                            responseText[..Math.Min(400, responseText.Length)]);
                    }
                }
                catch (Exception ex)
                {
                    await _notificationsRepo.MarkPushAttemptAsync(
                        candidate.NotificationId,
                        candidate.DeviceTokenId,
                        false,
                        null,
                        ex.Message);

                    _logger.LogError(
                        ex,
                        "[ExpoPush] Exception sending notification {NotificationId} to device token {DeviceTokenId}",
                        candidate.NotificationId,
                        candidate.DeviceTokenId);
                }
            }

            if (candidates.Count > 0)
            {
                _logger.LogInformation(
                    "[ExpoPush] Sent {SentCount}/{CandidateCount} pending push notifications",
                    sent,
                    candidates.Count);
            }

            return sent;
        }

        private static string? TryReadExpoId(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.ValueKind == JsonValueKind.Object
                        && data.TryGetProperty("id", out var id))
                    {
                        return id.GetString();
                    }

                    if (data.ValueKind == JsonValueKind.Array
                        && data.GetArrayLength() > 0
                        && data[0].TryGetProperty("id", out var firstId))
                    {
                        return firstId.GetString();
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
