namespace BackendAPI.Models
{
    public class BusinessAccount
    {
        public long Id { get; set; }
        public long OwnerUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string Status { get; set; } = BusinessAccountStatus.Active;
        public DateTime CreatedAt { get; set; }
    }

    public static class BusinessAccountStatus
    {
        public const string Active = "Active";
        public const string Disabled = "Disabled";
    }

    public class BusinessCampaign
    {
        public long Id { get; set; }
        public long BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string Status { get; set; } = CampaignStatus.Draft;
        public DateTime CreatedAt { get; set; }
        public int Impressions { get; set; }
        public int Votes { get; set; }
        public int Completions { get; set; }
        public double CompletionRate { get; set; }
    }

    public static class CampaignStatus
    {
        public const string Draft = "Draft";
        public const string Active = "Active";
        public const string Paused = "Paused";
        public const string Completed = "Completed";

        public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
        {
            Draft,
            Active,
            Paused,
            Completed
        };

        public static string Normalize(string? status, string fallback = Draft)
        {
            if (string.IsNullOrWhiteSpace(status)) return fallback;
            return All.TryGetValue(status.Trim(), out var normalized) ? normalized : fallback;
        }
    }

    public class CampaignPollMetric
    {
        public long CampaignId { get; set; }
        public long PollId { get; set; }
        public int Impressions { get; set; }
        public int Votes { get; set; }
        public int Completions { get; set; }
        public double CompletionRate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateBusinessAccountRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
    }

    public class CreateBusinessCampaignRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string Status { get; set; } = CampaignStatus.Draft;
    }

    public class CreateSponsoredPollRequest : CreatePollRequest
    {
        public long CampaignId { get; set; }
    }
}
