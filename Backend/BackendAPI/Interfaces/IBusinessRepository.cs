using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IBusinessRepository
    {
        Task<BusinessAccount> CreateBusinessAsync(long ownerUserId, CreateBusinessAccountRequest request);
        Task<IEnumerable<BusinessAccount>> GetBusinessesForUserAsync(long ownerUserId);
        Task<BusinessCampaign?> CreateCampaignAsync(long ownerUserId, long businessId, CreateBusinessCampaignRequest request);
        Task<IEnumerable<BusinessCampaign>> GetCampaignsForUserAsync(long ownerUserId);
        Task<CampaignAnalytics?> GetCampaignAnalyticsAsync(long ownerUserId, long campaignId);
        Task<long?> CreateSponsoredPollAsync(long ownerUserId, CreateSponsoredPollRequest request);
        Task<bool> RecordImpressionAsync(long pollId);
        Task RecordVoteAsync(long pollId);
    }
}
