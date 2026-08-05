using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class WellnessRepository : IWellnessRepository
    {
        private readonly DapperContext _context;

        public WellnessRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Poll>> GetActivePollsAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            var pollDict = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT p.*, o.*
                  FROM Polls p
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                    AND p.IsWellness = 1
                    AND p.IsPrivate = 1
                    AND (p.CreatedByUserId IS NULL OR p.CreatedByUserId = @UserId)
                    AND p.ExpiresAt > GETUTCDATE()
                  ORDER BY p.CreatedAt DESC",
                (poll, option) =>
                {
                    if (!pollDict.TryGetValue(poll.Id, out var existing))
                    {
                        existing = poll;
                        existing.Options = new List<PollOption>();
                        pollDict[poll.Id] = existing;
                    }
                    if (option != null) existing.Options.Add(option);
                    return existing;
                },
                new { UserId = userId },
                splitOn: "Id");

            return pollDict.Values;
        }

        public async Task<WellnessOverview> GetOverviewAsync(long userId)
        {
            return new WellnessOverview
            {
                Polls = await GetActivePollsAsync(userId),
                History = await GetHistoryAsync(userId, 10),
                Insight = await GetInsightAsync(userId)
            };
        }

        public async Task<WellnessResponse?> CreateResponseAsync(
            long userId,
            CreateWellnessResponseRequest request)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var pollOption = await conn.QuerySingleOrDefaultAsync<WellnessPollOption>(
                    @"SELECT
                          p.Id AS PollId,
                          p.Question,
                          o.Id AS OptionId,
                          o.Text AS OptionText
                      FROM Polls p
                      JOIN PollOptions o ON o.PollId = p.Id
                      WHERE p.Id = @PollId
                        AND o.Id = @OptionId
                        AND p.IsActive = 1
                        AND p.IsWellness = 1
                        AND p.IsPrivate = 1
                        AND p.ExpiresAt > GETUTCDATE()
                        AND (p.CreatedByUserId IS NULL OR p.CreatedByUserId = @UserId)",
                    new { request.PollId, request.OptionId, UserId = userId },
                    transaction);

                if (pollOption == null)
                {
                    transaction.Rollback();
                    return null;
                }

                var id = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO WellnessResponses
                        (UserId, PollId, OptionId, Note, CreatedAt)
                      VALUES
                        (@UserId, @PollId, @OptionId, @Note, GETUTCDATE());
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                    new
                    {
                        UserId = userId,
                        request.PollId,
                        request.OptionId,
                        Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
                    },
                    transaction);

                transaction.Commit();

                return new WellnessResponse
                {
                    Id = id,
                    UserId = userId,
                    PollId = pollOption.PollId,
                    OptionId = pollOption.OptionId,
                    Question = pollOption.Question,
                    OptionText = pollOption.OptionText,
                    Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<WellnessResponse>> GetHistoryAsync(long userId, int count = 30)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<WellnessResponse>(
                @"SELECT TOP (@Count)
                      wr.Id,
                      wr.UserId,
                      wr.PollId,
                      wr.OptionId,
                      p.Question,
                      o.Text AS OptionText,
                      wr.Note,
                      wr.CreatedAt
                  FROM WellnessResponses wr
                  JOIN Polls p ON p.Id = wr.PollId
                  JOIN PollOptions o ON o.Id = wr.OptionId
                  WHERE wr.UserId = @UserId
                  ORDER BY wr.CreatedAt DESC",
                new { UserId = userId, Count = count });
        }

        public async Task<WellnessInsight> GetInsightAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            var summary = await conn.QuerySingleAsync<WellnessInsight>(
                @"SELECT
                      COUNT(1) AS TotalCheckIns,
                      MAX(CreatedAt) AS LastCheckInAt
                  FROM WellnessResponses
                  WHERE UserId = @UserId",
                new { UserId = userId });

            summary.MostCommonResponse = await conn.ExecuteScalarAsync<string?>(
                @"SELECT TOP (1) o.Text
                  FROM WellnessResponses wr
                  JOIN PollOptions o ON o.Id = wr.OptionId
                  WHERE wr.UserId = @UserId
                  GROUP BY o.Text
                  ORDER BY COUNT(1) DESC, o.Text ASC",
                new { UserId = userId });

            return summary;
        }

        public async Task DeleteResponsesAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM WellnessResponses WHERE UserId = @UserId",
                new { UserId = userId });
        }

        private class WellnessPollOption
        {
            public long PollId { get; set; }
            public string Question { get; set; } = string.Empty;
            public long OptionId { get; set; }
            public string OptionText { get; set; } = string.Empty;
        }
    }
}
