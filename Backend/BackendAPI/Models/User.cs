namespace BackendAPI.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Xp { get; set; }
        public int Streak { get; set; }
        public int TotalVotes { get; set; }
        public int PollsCreated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>US-22: One row in a user's voting history.</summary>
    public class VoteHistoryItem
    {
        public long   PollId           { get; set; }
        public string Question         { get; set; } = string.Empty;
        public string Category         { get; set; } = string.Empty;
        public string VotedOptionText  { get; set; } = string.Empty;
        public int    TotalVotes       { get; set; }
        public DateTime VotedAt        { get; set; }
    }
}
