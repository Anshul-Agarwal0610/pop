namespace BackendAPI.Models
{
    public class Poll
    {
        public long Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsTrending { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalVotes { get; set; }
        public List<PollOption> Options { get; set; } = new();
    }

    public class PollOption
    {
        public long Id { get; set; }
        public long PollId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public double VotePercentage { get; set; }
    }

    public class CreatePollRequest
    {
        public string Question { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public List<string> Options { get; set; } = new();
    }
}
