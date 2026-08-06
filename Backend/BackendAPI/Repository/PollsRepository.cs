using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class PollsRepository : IPollsRepository
    {
        private readonly DapperContext _context;
        private readonly IUsersRepository _usersRepo;

        public PollsRepository(DapperContext context, IUsersRepository usersRepo)
        {
            _context = context;
            _usersRepo = usersRepo;
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

        private static string? NormalizeFilterCategory(string? category)
        {
            return string.IsNullOrWhiteSpace(category)
                ? null
                : CategoryCatalog.NormalizeName(category);
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

        public async Task<IEnumerable<Poll>> GetAllAsync(long? userId = null, string? category = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();
            var normalizedCategory = NormalizeFilterCategory(category);

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT p.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM Polls p
                  LEFT JOIN Users u ON u.Id = p.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = p.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = p.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                    AND p.ModerationStatus = 'Published'
                    AND COALESCE(p.IsPrivate, 0) = 0
                    AND COALESCE(p.IsWellness, 0) = 0
                    AND (@Category IS NULL OR LOWER(p.Category) = LOWER(@Category))
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
                new { Category = normalizedCategory },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        // ── GetById ───────────────────────────────────────────────────────────

        public async Task<Poll?> GetByIdAsync(long id, long? userId = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT p.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM Polls p
                  LEFT JOIN Users u ON u.Id = p.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = p.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = p.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.Id = @Id
                    AND (COALESCE(p.IsPrivate, 0) = 0 OR p.CreatedByUserId = @UserId)",
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
                new { Id = id, UserId = userId },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes).FirstOrDefault();
        }

        // ── GetTrending ───────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null, string? category = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();
            var normalizedCategory = NormalizeFilterCategory(category);

            // US-09: order by TotalVotes DESC directly
            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT TOP (@Count) p.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM Polls p
                  LEFT JOIN Users u ON u.Id = p.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = p.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = p.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                    AND p.ModerationStatus = 'Published'
                    AND COALESCE(p.IsPrivate, 0) = 0
                    AND COALESCE(p.IsWellness, 0) = 0
                    AND (@Category IS NULL OR LOWER(p.Category) = LOWER(@Category))
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
                new { Count = count, Category = normalizedCategory },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        // ── GetRecent ─────────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetPersonalizedAsync(
            long? userId = null,
            int count = 20,
            string? category = null)
        {
            if (userId == null)
            {
                return await GetTrendingAsync(count, null, category);
            }

            using var conn = _context.CreateConnection();
            var pollDict = new Dictionary<long, Poll>();
            var normalizedCategory = NormalizeFilterCategory(category);

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"WITH Activity AS (
                    SELECT p.Category, COUNT_BIG(*) AS VoteCount
                    FROM Votes v
                    JOIN Polls p ON p.Id = v.PollId
                    WHERE v.UserId = @UserId
                      AND p.Category <> 'Health'
                    GROUP BY p.Category
                  ),
                  RankedPolls AS (
                    SELECT TOP (@Count)
                      p.*,
                      CAST(
                        CASE WHEN pref.Category IS NOT NULL THEN 100 ELSE 0 END
                        + CASE WHEN activity.Category IS NOT NULL THEN
                            CASE WHEN activity.VoteCount > 10 THEN 50 ELSE activity.VoteCount * 5 END
                          ELSE 0 END
                        + CASE WHEN p.IsTrending = 1 THEN 20 ELSE 0 END
                        + CASE WHEN p.CreatedAt >= DATEADD(day, -2, GETUTCDATE()) THEN 10 ELSE 0 END
                        AS int
                      ) AS PersonalizationScore
                    FROM Polls p
                    LEFT JOIN UserCategoryPreferences pref
                      ON pref.UserId = @UserId AND LOWER(pref.Category) = LOWER(p.Category)
                    LEFT JOIN Activity activity
                      ON LOWER(activity.Category) = LOWER(p.Category)
                    WHERE p.IsActive = 1
                      AND p.ModerationStatus = 'Published'
                      AND COALESCE(p.IsPrivate, 0) = 0
                      AND COALESCE(p.IsWellness, 0) = 0
                      AND (@Category IS NULL OR LOWER(p.Category) = LOWER(@Category))
                    ORDER BY PersonalizationScore DESC, p.TotalVotes DESC, p.CreatedAt DESC
                  )
                  SELECT rp.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM RankedPolls rp
                  LEFT JOIN Users u ON u.Id = rp.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = rp.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = rp.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = rp.Id
                  ORDER BY rp.PersonalizationScore DESC, rp.TotalVotes DESC, rp.CreatedAt DESC",
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
                new { UserId = userId.Value, Count = count, Category = normalizedCategory },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        public async Task<IEnumerable<Poll>> SearchAsync(string query, string? category = null, long? userId = null)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();
            var normalizedCategory = NormalizeFilterCategory(category);

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"WITH SearchPolls AS (
                    SELECT TOP (50) p.*
                    FROM Polls p
                    WHERE p.IsActive = 1
                      AND p.ModerationStatus = 'Published'
                      AND COALESCE(p.IsPrivate, 0) = 0
                      AND COALESCE(p.IsWellness, 0) = 0
                      AND p.Question LIKE @Search
                      AND (@Category IS NULL OR LOWER(p.Category) = LOWER(@Category))
                    ORDER BY p.TotalVotes DESC, p.CreatedAt DESC
                  )
                  SELECT sp.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM SearchPolls sp
                  LEFT JOIN Users u ON u.Id = sp.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = sp.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = sp.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = sp.Id
                  ORDER BY sp.TotalVotes DESC, sp.CreatedAt DESC",
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
                new { Search = $"%{query.Trim()}%", Category = normalizedCategory },
                splitOn: "Id"
            );

            var userVotes = await GetUserVotesAsync(userId);
            return ApplyVoteState(pollDict.Values, userVotes);
        }

        public async Task<IEnumerable<Poll>> GetRecentAsync(int count = 10)
        {
            using var conn = _context.CreateConnection();
            var pollDict   = new Dictionary<long, Poll>();

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT TOP (@Count) p.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM Polls p
                  LEFT JOIN Users u ON u.Id = p.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = p.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = p.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                    AND p.ModerationStatus = 'Published'
                    AND COALESCE(p.IsPrivate, 0) = 0
                    AND COALESCE(p.IsWellness, 0) = 0
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

        public async Task<IEnumerable<Poll>> GetRecentGeneratedAsync(int count = 100)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<Poll>(
                @"SELECT TOP (@Count) Id, Question, Category, CreatedAt, SourceUrl, IsAIGenerated, GenerationProvider, GenerationModel
                  FROM Polls
                  WHERE IsAIGenerated = 1
                  ORDER BY CreatedAt DESC",
                new { Count = count });
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Poll>> GetModerationQueueAsync(string? status = null, int count = 50)
        {
            using var conn = _context.CreateConnection();
            var pollDict = new Dictionary<long, Poll>();
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? null
                : PollModerationStatus.Normalize(status, PollModerationStatus.PendingReview);

            await conn.QueryAsync<Poll, PollOption, Poll>(
                @"SELECT TOP (@Count) p.*, u.Username AS CreatedByUsername, u.DisplayName AS CreatedByDisplayName,
                         b.Name AS SponsorName, c.Name AS CampaignName, o.*
                  FROM Polls p
                  LEFT JOIN Users u ON u.Id = p.CreatedByUserId
                  LEFT JOIN BusinessAccounts b ON b.Id = p.BusinessId
                  LEFT JOIN BusinessCampaigns c ON c.Id = p.CampaignId
                  LEFT JOIN PollOptions o ON o.PollId = p.Id
                  WHERE p.IsActive = 1
                    AND (@Status IS NULL OR p.ModerationStatus = @Status)
                    AND (@Status IS NOT NULL OR p.ModerationStatus IN ('PendingReview', 'Flagged'))
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
                new { Count = count, Status = normalizedStatus },
                splitOn: "Id"
            );

            return pollDict.Values;
        }

        public async Task<long> CreateAsync(CreatePollRequest request, long? createdByUserId = null)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var normalizedCategory = CategoryCatalog.NormalizeName(request.Category);
            var isWellness = request.IsWellness || normalizedCategory.Equals("Health", StringComparison.OrdinalIgnoreCase);
            var isPrivate = request.IsPrivate || isWellness;
            var pollMode = isWellness ? PollModes.Wellness : PollModes.Public;
            var moderationStatus = isWellness
                ? PollModerationStatus.Published
                : PollModerationStatus.PendingReview;

            try
            {
                var pollId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Polls
                        (Question, Description, Category, ExpiresAt, IsActive, IsTrending,
                         CreatedByUserId,
                         CreatedAt, TotalVotes, SourceType, SourceUrl, ThumbnailUrl, IsAIGenerated,
                         IsPrivate, IsWellness, PollMode,
                         ModerationStatus, ModerationReason, ModeratedByUserId, ModeratedAt, ReportCount, LastReportedAt,
                         GenerationProvider, GenerationModel)
                      VALUES
                        (@Question, @Description, @Category, @ExpiresAt, 1, 0,
                         @CreatedByUserId,
                         GETUTCDATE(), 0, @SourceType, @SourceUrl, @ThumbnailUrl, @IsAIGenerated,
                         @IsPrivate, @IsWellness, @PollMode,
                         @ModerationStatus, @ModerationReason, NULL, NULL, 0, NULL,
                         @GenerationProvider, @GenerationModel);
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                    new
                    {
                        request.Question,
                        request.Description,
                        Category = normalizedCategory,
                        request.ExpiresAt,
                        request.SourceType,
                        request.SourceUrl,
                        request.ThumbnailUrl,
                        request.IsAIGenerated,
                        GenerationProvider = request.IsAIGenerated ? request.GenerationProvider : null,
                        GenerationModel = request.IsAIGenerated ? request.GenerationModel : null,
                        IsPrivate = isPrivate,
                        IsWellness = isWellness,
                        PollMode = pollMode,
                        ModerationStatus = moderationStatus,
                        ModerationReason = string.IsNullOrWhiteSpace(request.ModerationReason)
                            ? null
                            : request.ModerationReason.Trim(),
                        CreatedByUserId = createdByUserId
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
                if (createdByUserId != null)
                {
                    await _usersRepo.IncrementPollsCreatedAsync(createdByUserId.Value);
                }
                return pollId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task<bool> ReportAsync(long pollId, long reportedByUserId, string reason)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var rows = await conn.ExecuteAsync(
                    @"UPDATE Polls
                      SET ModerationStatus = 'Flagged',
                          ModerationReason = @Reason,
                          ReportCount = ReportCount + 1,
                          LastReportedAt = GETUTCDATE()
                      WHERE Id = @PollId AND IsActive = 1",
                    new
                    {
                        PollId = pollId,
                        Reason = string.IsNullOrWhiteSpace(reason) ? "Reported by user" : reason.Trim()
                    },
                    transaction);

                if (rows == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO PollReports (PollId, ReportedByUserId, Reason, CreatedAt)
                      VALUES (@PollId, @ReportedByUserId, @Reason, GETUTCDATE())",
                    new
                    {
                        PollId = pollId,
                        ReportedByUserId = reportedByUserId,
                        Reason = string.IsNullOrWhiteSpace(reason) ? "Reported by user" : reason.Trim()
                    },
                    transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> ModerateAsync(long pollId, string status, string? reason, long moderatedByUserId)
        {
            using var conn = _context.CreateConnection();
            var normalizedStatus = PollModerationStatus.Normalize(status);

            var rows = await conn.ExecuteAsync(
                @"UPDATE Polls
                  SET ModerationStatus = @Status,
                      ModerationReason = @Reason,
                      ModeratedByUserId = @ModeratedByUserId,
                      ModeratedAt = GETUTCDATE(),
                      IsActive = CASE WHEN @Status = 'Rejected' THEN 0 ELSE IsActive END
                  WHERE Id = @PollId",
                new
                {
                    PollId = pollId,
                    Status = normalizedStatus,
                    Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                    ModeratedByUserId = moderatedByUserId
                });

            return rows > 0;
        }

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
                WHERE IsActive = 1 AND ModerationStatus = 'Published'
                ORDER BY TotalVotes DESC;
            ");
        }
    }
}
