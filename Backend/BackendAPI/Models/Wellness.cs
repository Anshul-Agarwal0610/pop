namespace BackendAPI.Models
{
    public class WellnessResponse
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long PollId { get; set; }
        public long OptionId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string OptionText { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WellnessInsight
    {
        public int TotalCheckIns { get; set; }
        public DateTime? LastCheckInAt { get; set; }
        public string? MostCommonResponse { get; set; }
    }

    public class WellnessOverview
    {
        public IEnumerable<Poll> Polls { get; set; } = Enumerable.Empty<Poll>();
        public IEnumerable<WellnessResponse> History { get; set; } = Enumerable.Empty<WellnessResponse>();
        public WellnessInsight Insight { get; set; } = new();
    }

    public class CreateWellnessResponseRequest
    {
        public long PollId { get; set; }
        public long OptionId { get; set; }
        public string? Note { get; set; }
    }
}
