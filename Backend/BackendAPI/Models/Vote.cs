namespace BackendAPI.Models
{
    public class Vote
    {
        public long Id { get; set; }
        public long PollId { get; set; }
        public long OptionId { get; set; }
        public long? UserId { get; set; }          // null for anonymous votes
        public DateTime CreatedAt { get; set; }
    }

    public class CastVoteRequest
    {
        public long PollId { get; set; }
        public long OptionId { get; set; }
        // UserId is NOT accepted from the client — set server-side from JWT (US-15)
    }

    public class CastVoteResponse
    {
        public Poll Poll { get; set; } = new();
        public VoteRewardResult Reward { get; set; } = new();
    }
}
