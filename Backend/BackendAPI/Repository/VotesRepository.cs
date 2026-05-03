using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class VotesRepository : IVotesRepository
    {
        private readonly DapperContext _context;

        public VotesRepository(DapperContext context)
        {
            _context = context;
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
