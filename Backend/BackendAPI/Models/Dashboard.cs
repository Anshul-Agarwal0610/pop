namespace BackendAPI.Models
{
    public class DashboardData
    {
        public List<Poll> TrendingPolls { get; set; } = new();
        public List<Poll> RecentPolls { get; set; } = new();
        public DashboardStats Stats { get; set; } = new();
    }

    public class DashboardStats
    {
        public int TotalPolls { get; set; }
        public int TotalVotes { get; set; }
        public int ActivePolls { get; set; }
        public int TotalUsers { get; set; }
        public int BusinessAccounts { get; set; }
        public int SponsoredPolls { get; set; }
        public int PendingReview { get; set; }
        public int FlaggedPolls { get; set; }
    }
}
