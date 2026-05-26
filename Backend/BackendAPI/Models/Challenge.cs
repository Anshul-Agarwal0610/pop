namespace BackendAPI.Models
{
    public class Challenge
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int RequiredVotes { get; set; }
        public int RewardXp { get; set; }
        public string? RewardBadge { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserChallenge
    {
        public long ChallengeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int RequiredVotes { get; set; }
        public int RewardXp { get; set; }
        public string? RewardBadge { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int CurrentVotes { get; set; }
        public bool IsCompleted { get; set; }
        public bool RewardGranted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
