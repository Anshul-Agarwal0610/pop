namespace BackendAPI.Models
{
    public enum NotificationType
    {
        VoteMilestone,
        LevelUp,
        PollTrending,
        DailyReminder
    }

    public class Notification
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public long? PollId { get; set; }
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
    }
}
