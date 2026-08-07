namespace BackendAPI.Models
{
    public enum NotificationType
    {
        VoteMilestone,
        LevelUp,
        PollTrending,
        DailyReminder,
        ChallengeAvailable,
        StreakReminder,
        StreakMilestone,
        PollExpiring,
        PollBombReminder
    }

    public class Notification
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long? PollId { get; set; }
        public string? DedupKey { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationRequest
    {
        public long UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long? PollId { get; set; }
        public string? DedupKey { get; set; }
    }

    public class NotificationPreference
    {
        public NotificationType Type { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateNotificationPreferencesRequest
    {
        public List<NotificationType> DisabledTypes { get; set; } = new();
    }

    public class RegisterPushTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Platform { get; set; } = "android";
        public string? DeviceId { get; set; }
    }

    public class PushNotificationCandidate
    {
        public long NotificationId { get; set; }
        public long DeviceTokenId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long? PollId { get; set; }
        public NotificationType Type { get; set; }
    }
}
