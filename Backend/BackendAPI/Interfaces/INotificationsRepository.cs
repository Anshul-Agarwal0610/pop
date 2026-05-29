using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface INotificationsRepository
    {
        Task<long> CreateAsync(CreateNotificationRequest request);
        Task<IEnumerable<Notification>> GetForUserAsync(long userId, int count = 30);
        Task<int> GetUnreadCountAsync(long userId);
        Task MarkAllReadAsync(long userId);
        Task<bool> MarkReadAsync(long userId, long notificationId);
        Task<IEnumerable<NotificationPreference>> GetPreferencesAsync(long userId);
        Task<IEnumerable<NotificationPreference>> ReplacePreferencesAsync(
            long userId,
            IEnumerable<NotificationType> disabledTypes);
        Task RegisterDeviceTokenAsync(long userId, RegisterPushTokenRequest request);
        Task DisableDeviceTokenAsync(long userId, string token);
        Task<IEnumerable<PushNotificationCandidate>> GetPendingPushNotificationsAsync(int count = 100);
        Task MarkPushAttemptAsync(
            long notificationId,
            long deviceTokenId,
            bool success,
            string? providerMessageId,
            string? errorMessage);
        Task<int> CreateDailyChallengeNotificationsAsync(DateTime utcNow);
        Task<int> CreateStreakReminderNotificationsAsync(DateTime utcNow);
        Task<int> CreateTrendingPollNotificationsAsync(DateTime utcNow);
        Task<int> CreateExpiringPollNotificationsAsync(DateTime utcNow);
    }
}
