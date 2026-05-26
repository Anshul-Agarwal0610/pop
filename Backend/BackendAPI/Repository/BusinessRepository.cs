using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly DapperContext _context;

        public BusinessRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<BusinessAccount> CreateBusinessAsync(
            long ownerUserId,
            CreateBusinessAccountRequest request)
        {
            using var conn = _context.CreateConnection();
            var id = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO BusinessAccounts (OwnerUserId, Name, WebsiteUrl, Status, CreatedAt)
                  VALUES (@OwnerUserId, @Name, @WebsiteUrl, @Status, GETUTCDATE());
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                new
                {
                    OwnerUserId = ownerUserId,
                    Name = request.Name.Trim(),
                    WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim(),
                    Status = BusinessAccountStatus.Active
                });

            return (await conn.QuerySingleAsync<BusinessAccount>(
                "SELECT * FROM BusinessAccounts WHERE Id = @Id",
                new { Id = id }));
        }

        public async Task<IEnumerable<BusinessAccount>> GetBusinessesForUserAsync(long ownerUserId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<BusinessAccount>(
                @"SELECT *
                  FROM BusinessAccounts
                  WHERE OwnerUserId = @OwnerUserId
                  ORDER BY CreatedAt DESC",
                new { OwnerUserId = ownerUserId });
        }

        public async Task<BusinessCampaign?> CreateCampaignAsync(
            long ownerUserId,
            long businessId,
            CreateBusinessCampaignRequest request)
        {
            using var conn = _context.CreateConnection();
            var ownsBusiness = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1)
                  FROM BusinessAccounts
                  WHERE Id = @BusinessId AND OwnerUserId = @OwnerUserId AND Status = @Status",
                new { BusinessId = businessId, OwnerUserId = ownerUserId, Status = BusinessAccountStatus.Active });

            if (ownsBusiness == 0) return null;

            var id = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO BusinessCampaigns
                    (BusinessId, Name, Objective, StartsAt, EndsAt, Status, CreatedAt)
                  VALUES
                    (@BusinessId, @Name, @Objective, @StartsAt, @EndsAt, @Status, GETUTCDATE());
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                new
                {
                    BusinessId = businessId,
                    Name = request.Name.Trim(),
                    Objective = request.Objective.Trim(),
                    request.StartsAt,
                    request.EndsAt,
                    Status = CampaignStatus.Normalize(request.Status)
                });

            return await GetCampaignForUserAsync(ownerUserId, id);
        }

        public async Task<IEnumerable<BusinessCampaign>> GetCampaignsForUserAsync(long ownerUserId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<BusinessCampaign>(
                @"SELECT c.*,
                         b.Name AS BusinessName,
                         COALESCE(SUM(m.Impressions), 0) AS Impressions,
                         COALESCE(SUM(m.Votes), 0) AS Votes,
                         COALESCE(SUM(m.Completions), 0) AS Completions,
                         CAST(CASE
                            WHEN COALESCE(SUM(m.Impressions), 0) = 0 THEN 0
                            ELSE COALESCE(SUM(m.Completions), 0) * 100.0 / COALESCE(SUM(m.Impressions), 0)
                         END AS float) AS CompletionRate
                  FROM BusinessCampaigns c
                  JOIN BusinessAccounts b ON b.Id = c.BusinessId
                  LEFT JOIN SponsoredPollMetrics m ON m.CampaignId = c.Id
                  WHERE b.OwnerUserId = @OwnerUserId
                  GROUP BY c.Id, c.BusinessId, b.Name, c.Name, c.Objective, c.StartsAt, c.EndsAt, c.Status, c.CreatedAt
                  ORDER BY c.CreatedAt DESC",
                new { OwnerUserId = ownerUserId });
        }

        public async Task<long?> CreateSponsoredPollAsync(long ownerUserId, CreateSponsoredPollRequest request)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var campaign = await conn.QuerySingleOrDefaultAsync<CampaignOwnership>(
                    @"SELECT c.Id AS CampaignId, c.BusinessId
                      FROM BusinessCampaigns c
                      JOIN BusinessAccounts b ON b.Id = c.BusinessId
                      WHERE c.Id = @CampaignId
                        AND b.OwnerUserId = @OwnerUserId
                        AND b.Status = @BusinessStatus",
                    new
                    {
                        request.CampaignId,
                        OwnerUserId = ownerUserId,
                        BusinessStatus = BusinessAccountStatus.Active
                    },
                    transaction);

                if (campaign == null)
                {
                    transaction.Rollback();
                    return null;
                }

                var pollId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Polls
                        (Question, Description, Category, ExpiresAt, IsActive, IsTrending,
                         CreatedByUserId, CreatedAt, TotalVotes, SourceType, SourceUrl, ThumbnailUrl,
                         IsAIGenerated, IsSponsored, BusinessId, CampaignId,
                         ModerationStatus, ModerationReason, ModeratedByUserId, ModeratedAt, ReportCount, LastReportedAt)
                      VALUES
                        (@Question, @Description, @Category, @ExpiresAt, 1, 0,
                         @CreatedByUserId, GETUTCDATE(), 0, @SourceType, @SourceUrl, @ThumbnailUrl,
                         0, 1, @BusinessId, @CampaignId,
                         @ModerationStatus, NULL, NULL, NULL, 0, NULL);
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                    new
                    {
                        request.Question,
                        request.Description,
                        Category = CategoryCatalog.NormalizeName(request.Category),
                        request.ExpiresAt,
                        CreatedByUserId = ownerUserId,
                        SourceType = "business",
                        request.SourceUrl,
                        request.ThumbnailUrl,
                        BusinessId = campaign.BusinessId,
                        CampaignId = request.CampaignId,
                        ModerationStatus = PollModerationStatus.PendingReview
                    },
                    transaction);

                foreach (var optionText in request.Options)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO PollOptions (PollId, Text, VoteCount) VALUES (@PollId, @Text, 0)",
                        new { PollId = pollId, Text = optionText },
                        transaction);
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO SponsoredPollMetrics (CampaignId, PollId, Impressions, Votes, Completions, UpdatedAt)
                      VALUES (@CampaignId, @PollId, 0, 0, 0, GETUTCDATE())",
                    new { request.CampaignId, PollId = pollId },
                    transaction);

                transaction.Commit();
                return pollId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> RecordImpressionAsync(long pollId)
        {
            using var conn = _context.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE SponsoredPollMetrics
                  SET Impressions = Impressions + 1, UpdatedAt = GETUTCDATE()
                  WHERE PollId = @PollId",
                new { PollId = pollId });

            return rows > 0;
        }

        public async Task RecordVoteAsync(long pollId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                @"UPDATE SponsoredPollMetrics
                  SET Votes = Votes + 1,
                      Completions = Completions + 1,
                      UpdatedAt = GETUTCDATE()
                  WHERE PollId = @PollId",
                new { PollId = pollId });
        }

        private async Task<BusinessCampaign?> GetCampaignForUserAsync(long ownerUserId, long campaignId)
        {
            return (await GetCampaignsForUserAsync(ownerUserId))
                .FirstOrDefault(campaign => campaign.Id == campaignId);
        }

        private class CampaignOwnership
        {
            public long CampaignId { get; set; }
            public long BusinessId { get; set; }
        }
    }
}
