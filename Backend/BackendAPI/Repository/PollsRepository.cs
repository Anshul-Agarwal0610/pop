using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class PollsRepository : IPollsRepository
    {
        private readonly DapperContext _context;

        public PollsRepository(DapperContext context)
        {
            _context = context;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Fetches the set of (PollId → OptionId) the user has voted on.
        /// Returns an empty dictionary when userId is null.
        /// </summary>
        private async Task<Dictionary<long, long>> GetUserVotesAsync(long? userId)
        {
            if (userId == null) return new Dictionary<long, long>();

            using var conn = _context.CreateConnection();
            var rows = await conn.QueryAsync<(long PollId, long OptionId)>(
                "SELECT PollId, OptionId FROM Votes WHERE UserId = @UserId",
                new { UserId = userId }
            );
            return rows.ToDictionary(r => r.PollId, r => r.OptionId);
        }

        /// <summary>Applies HasVoted / UserVotedOptionId to a list of polls in-memory.</summary>
        private static IEnumerable<Poll> ApplyVoteState(
            IEnumerable<Poll> polls,
            Dictionary<long, long> userVotes)
        {
            foreach (var poll in polls)
            {
                if (userVotes.TryGetValue(poll.Id, out var optionId))
                {
                    poll.HasVoted          = true;
                    poll.UserVotedOptionId = optionId;
                }
            }
            return polls;
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetAllAsync(long? userId = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                "SELECT p.*, o.* FROM Polls p LEFT JOIN PollOptions o ON o.PollId = p.Id WHERE p.IsActive = 1 ORDER BY p.CreatedAt DESC",
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
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        // ── GetById ───────────────────────────────────────────────────────────

        public async Task<Poll?> GetByIdAsync(long id)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                "SELECT p.*, o.* FROM Polls p LEFT JOIN PollOptions o ON o.PollId = p.Id WHERE p.Id = @Id",
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
                new { Id = id },
                splitOn: "Id"
            );

            return pollDict.Values.FirstOrDefault();
        }

        // ── GetTrending ───────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            // US-09: order by TotalVotes DESC directly
            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT TOP (@Count) p.*, o.*
                  FROM Polls p
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                  ORDER BY p.TotalVotes DESC, p.CreatedAt DESC",
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
                new { Count = count },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        // ── GetRecent ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetRecentAsync(int count = 10)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT TOP (@Count) p.*, o.*
                  FROM Polls p
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
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
                new { Count = count },
                splitOn: "Id"
            );

            return pollDict.Values;
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<long> CreateAsync(CreatePollRequest request)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var pollId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Polls
                        (Question, Description, Category, ExpiresAt, IsActive, IsTrending,
                         CreatedAt, TotalVotes, SourceType, SourceUrl, ThumbnailUrl, IsAIGenerated)
                      VALUES
                        (@Question, @Description, @Category, @ExpiresAt, 1, 0,
                         GETUTCDATE(), 0, @SourceType, @SourceUrl, @ThumbnailUrl, @IsAIGenerated);
                      SELECT SCOPE_IDENTITY();",
                    new
                    {
                        request.Question,
                        request.Description,
                        request.Category,
                        request.ExpiresAt,
                        request.SourceType,
                        request.SourceUrl,
                        request.ThumbnailUrl,
                        request.IsAIGenerated
                    },
                    transaction
                );

                foreach (var optionText in request.Options)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO PollOptions (PollId, Text, VoteCount) VALUES (@PollId, @Text, 0)",
                        new { PollId = pollId, Text = optionText },
                        transaction
                    );
                }

                transaction.Commit();
                return pollId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> DeleteAsync(long id)
        {
            using var conn = _context.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "UPDATE Polls SET IsActive = 0 WHERE Id = @Id",
                new { Id = id }
            );
            return rows > 0;
        }

        // ── UpdateTrending ────────────────────────────────────────────────────

        public async Task UpdateTrendingAsync()
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(@"
                UPDATE Polls SET IsTrending = 0 WHERE IsActive = 1;
                UPDATE TOP (10) Polls SET IsTrending = 1
                FROM Polls
                WHERE IsActive = 1
                ORDER BY TotalVotes DESC;
            ");
        }
    }
}
