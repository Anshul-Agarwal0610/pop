using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class NotificationsRepository : INotificationsRepository
    {
        private readonly DapperContext _context;

        public NotificationsRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<long> CreateAsync(CreateNotificationRequest request)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteScalarAsync<long?>(
                @"IF NOT EXISTS (
                      SELECT 1 FROM NotificationPreferences
                      WHERE UserId = @UserId AND Type = @Type AND IsEnabled = 0
                  )
                  AND (
                      @DedupKey IS NULL OR NOT EXISTS (
                          SELECT 1 FROM Notifications
                          WHERE UserId = @UserId AND DedupKey = @DedupKey
                      )
                  )
                  BEGIN
                      INSERT INTO Notifications (UserId, Type, Title, Body, PollId, DedupKey, IsRead, CreatedAt)
                      VALUES (@UserId, @Type, @Title, @Body, @PollId, @DedupKey, 0, GETUTCDATE());
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                  END
                  ELSE
                  BEGIN
                      SELECT CAST(0 AS BIGINT);
                  END",
                new
                {
                    request.UserId,
                    Type = request.Type.ToString(),
                    request.Title,
                    request.Body,
                    request.PollId,
                    request.DedupKey
                }
            ) ?? 0;
        }

        public async Task<IEnumerable<Notification>> GetForUserAsync(long userId, int count = 30)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<Notification>(
                @"SELECT TOP (@Count) *
                  FROM Notifications
                  WHERE UserId = @UserId
                  ORDER BY CreatedAt DESC",
                new { UserId = userId, Count = count }
            );
        }

        public async Task<int> GetUnreadCountAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0",
                new { UserId = userId }
            );
        }

        public async Task MarkAllReadAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0",
                new { UserId = userId }
            );
        }

        public async Task<bool> MarkReadAsync(long userId, long notificationId)
        {
            using var conn = _context.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Notifications
                  SET IsRead = 1
                  WHERE Id = @NotificationId AND UserId = @UserId",
                new { NotificationId = notificationId, UserId = userId }
            );
            return rows > 0;
        }

        public async Task<IEnumerable<NotificationPreference>> GetPreferencesAsync(long userId)
        {
            return await QueryPreferencesAsync(userId);
        }

        public async Task<IEnumerable<NotificationPreference>> ReplacePreferencesAsync(
            long userId,
            IEnumerable<NotificationType> disabledTypes)
        {
            var disabled = disabledTypes.Distinct().Select(type => type.ToString()).ToList();

            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                await conn.ExecuteAsync(
                    "DELETE FROM NotificationPreferences WHERE UserId = @UserId",
                    new { UserId = userId },
                    transaction);

                foreach (var type in Enum.GetValues<NotificationType>())
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO NotificationPreferences (UserId, Type, IsEnabled, UpdatedAt)
                          VALUES (@UserId, @Type, @IsEnabled, GETUTCDATE())",
                        new
                        {
                            UserId = userId,
                            Type = type.ToString(),
                            IsEnabled = !disabled.Contains(type.ToString())
                        },
                        transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return await QueryPreferencesAsync(userId);
        }

        public async Task<int> CreateDailyChallengeNotificationsAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(
                RetentionInsertSql(
                    NotificationType.ChallengeAvailable,
                    @"SELECT u.Id AS UserId,
                             NULL AS PollId,
                             CONCAT('challenge:', c.Id, ':available:', CONVERT(char(8), @UtcNow, 112)) AS DedupKey,
                             'Daily challenge is live' AS Title,
                             CONCAT(c.Title, ' is available now. Vote ', c.RequiredVotes, ' times before it ends for +', c.RewardXp, ' XP.') AS Body
                      FROM Users u
                      JOIN Challenges c
                        ON c.IsActive = 1
                       AND c.StartAt <= @UtcNow
                       AND c.EndAt > @UtcNow
                      LEFT JOIN UserChallengeProgress progress
                        ON progress.UserId = u.Id AND progress.ChallengeId = c.Id
                      WHERE COALESCE(progress.IsCompleted, 0) = 0"),
                new { UtcNow = utcNow, Type = NotificationType.ChallengeAvailable.ToString() });
        }

        public async Task<int> CreateStreakReminderNotificationsAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(
                RetentionInsertSql(
                    NotificationType.StreakReminder,
                    @"SELECT u.Id AS UserId,
                             NULL AS PollId,
                             CONCAT('streak:', u.Id, ':', CONVERT(char(8), @UtcNow, 112)) AS DedupKey,
                             'Keep your streak alive' AS Title,
                             CONCAT('Your ', u.Streak, '-day streak is waiting. Vote once today to keep it going.') AS Body
                      FROM Users u
                      WHERE u.Streak > 0
                        AND CONVERT(date, u.LastVoteDate) = DATEADD(day, -1, CONVERT(date, @UtcNow))"),
                new { UtcNow = utcNow, Type = NotificationType.StreakReminder.ToString() });
        }

        public async Task<int> CreateTrendingPollNotificationsAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(
                RetentionInsertSql(
                    NotificationType.PollTrending,
                    @"SELECT u.Id AS UserId,
                             p.Id AS PollId,
                             CONCAT('trending:', p.Id, ':', CONVERT(char(8), @UtcNow, 112)) AS DedupKey,
                             'A poll is trending' AS Title,
                             CONCAT('People are voting on: ', p.Question) AS Body
                      FROM Users u
                      CROSS JOIN (
                          SELECT TOP (3) Id, Question
                          FROM Polls
                          WHERE IsActive = 1
                            AND ModerationStatus = 'Published'
                            AND Category <> 'Health'
                            AND ExpiresAt > @UtcNow
                            AND (IsTrending = 1 OR TotalVotes >= 10)
                          ORDER BY IsTrending DESC, TotalVotes DESC, CreatedAt DESC
                      ) p
                      WHERE NOT EXISTS (
                          SELECT 1 FROM Votes v WHERE v.UserId = u.Id AND v.PollId = p.Id
                      )"),
                new { UtcNow = utcNow, Type = NotificationType.PollTrending.ToString() });
        }

        public async Task<int> CreateExpiringPollNotificationsAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(
                RetentionInsertSql(
                    NotificationType.PollExpiring,
                    @"SELECT u.Id AS UserId,
                             p.Id AS PollId,
                             CONCAT('expiring:', p.Id, ':', CONVERT(char(8), @UtcNow, 112)) AS DedupKey,
                             'Poll closing soon' AS Title,
                             CONCAT('Last chance to vote: ', p.Question) AS Body
                      FROM Users u
                      CROSS JOIN (
                          SELECT TOP (5) Id, Question
                          FROM Polls
                          WHERE IsActive = 1
                            AND ModerationStatus = 'Published'
                            AND Category <> 'Health'
                            AND ExpiresAt > @UtcNow
                            AND ExpiresAt <= DATEADD(hour, 6, @UtcNow)
                          ORDER BY ExpiresAt ASC, TotalVotes DESC
                      ) p
                      WHERE NOT EXISTS (
                          SELECT 1 FROM Votes v WHERE v.UserId = u.Id AND v.PollId = p.Id
                      )"),
                new { UtcNow = utcNow, Type = NotificationType.PollExpiring.ToString() });
        }

        private async Task<IEnumerable<NotificationPreference>> QueryPreferencesAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            var rows = (await conn.QueryAsync<NotificationPreference>(
                @"SELECT Type, IsEnabled
                  FROM NotificationPreferences
                  WHERE UserId = @UserId",
                new { UserId = userId })).ToDictionary(row => row.Type);

            return Enum.GetValues<NotificationType>()
                .Select(type => rows.TryGetValue(type, out var existing)
                    ? existing
                    : new NotificationPreference { Type = type, IsEnabled = true });
        }

        private static string RetentionInsertSql(NotificationType type, string sourceSql)
        {
            return $@"WITH Candidates AS (
                        {sourceSql}
                      )
                      INSERT INTO Notifications (UserId, Type, Title, Body, PollId, DedupKey, IsRead, CreatedAt)
                      SELECT c.UserId, @Type, c.Title, c.Body, c.PollId, c.DedupKey, 0, GETUTCDATE()
                      FROM Candidates c
                      WHERE NOT EXISTS (
                          SELECT 1 FROM NotificationPreferences pref
                          WHERE pref.UserId = c.UserId AND pref.Type = @Type AND pref.IsEnabled = 0
                      )
                        AND NOT EXISTS (
                          SELECT 1 FROM Notifications existing
                          WHERE existing.UserId = c.UserId AND existing.DedupKey = c.DedupKey
                      );";
        }
    }
}
