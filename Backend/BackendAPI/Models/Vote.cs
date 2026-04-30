namespace BackendAPI.Models
{
    public class Vote
    {
        public long Id { get; set; }
        public long PollId { get; set; }
        public long OptionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CastVoteRequest
    {
        public long PollId { get; set; }
        public long OptionId { get; set; }
    }
}
