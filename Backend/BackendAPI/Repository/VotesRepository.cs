using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using BackendAPI.Analytics;

namespace BackendAPI.Repository
{
    public class VotesRepository : IVotesRepository
    {
        private readonly DapperContext _context;
        private readonly IAnalyticsOutbox _analytics;

        public VotesRepository(DapperContext context, IAnalyticsOutbox analytics)
        {
            _context = context;
            _analytics = analytics;
        }

        public async Task<bool> CastVoteAsync(CastVoteRequest request, long? userId)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Record the vote — include UserId when authenticated (US-15)
                await conn.ExecuteAsync(
                    "INSERT INTO Votes (PollId, OptionId, UserId, CreatedAt) VALUES (@PollId, @OptionId, @UserId, GETUTCDATE())",
                    new { request.PollId, request.OptionId, UserId = userId },
                    transaction
                );

                // Increment option vote count
                await conn.ExecuteAsync(
                    "UPDATE PollOptions SET VoteCount = VoteCount + 1 WHERE Id = @OptionId AND PollId = @PollId",
                    new { request.OptionId, request.PollId },
                    transaction
                );

                // Update total votes on poll
                await conn.ExecuteAsync(
                    "UPDATE Polls SET TotalVotes = TotalVotes + 1 WHERE Id = @PollId",
                    new { request.PollId },
                    transaction
                );

                // Recalculate percentages
                await conn.ExecuteAsync(@"
                    UPDATE o SET
                        VotePercentage = CASE
                            WHEN p.TotalVotes = 0 THEN 0
                            ELSE CAST(o.VoteCount AS FLOAT) / p.TotalVotes * 100
                        END
                    FROM PollOptions o
                    JOIN Polls p ON p.Id = o.PollId
                    WHERE o.PollId = @PollId",
                    new { request.PollId },
                    transaction
                );

                if (userId.HasValue && await conn.ExecuteScalarAsync<string>("SELECT AnalyticsConsent FROM Users WHERE Id=@UserId", new { UserId = userId.Value }, transaction) == "granted")
                    await _analytics.EnqueueAsync(conn, transaction, new AnalyticsEvent(Guid.NewGuid(), AnalyticsEventNames.GameRoundCompleted, $"usr_{userId.Value}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["round_id"] = $"server-{userId.Value}-{request.PollId}", ["surface"] = "api", ["outcome"] = "voted", ["xp_awarded"] = 0 }, "round_id", "surface", "outcome", "xp_awarded"), DateTime.UtcNow, $"vote:{userId.Value}:{request.PollId}:round-completed"));

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<Vote>> GetVotesByPollAsync(long pollId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<Vote>(
                "SELECT * FROM Votes WHERE PollId = @PollId ORDER BY CreatedAt DESC",
                new { PollId = pollId }
            );
        }
    }
}
