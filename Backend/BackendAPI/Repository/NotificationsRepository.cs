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

        public async Task RegisterDeviceTokenAsync(long userId, RegisterPushTokenRequest request)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                @"IF EXISTS (SELECT 1 FROM MobileDeviceTokens WHERE Token = @Token)
                  BEGIN
                      UPDATE MobileDeviceTokens
                         SET UserId = @UserId,
                             Platform = @Platform,
                             DeviceId = @DeviceId,
                             IsActive = 1,
                             UpdatedAt = GETUTCDATE(),
                             LastSeenAt = GETUTCDATE()
                       WHERE Token = @Token;
                  END
                  ELSE
                  BEGIN
                      INSERT INTO MobileDeviceTokens (UserId, Token, Platform, DeviceId, IsActive, CreatedAt, UpdatedAt, LastSeenAt)
                      VALUES (@UserId, @Token, @Platform, @DeviceId, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
                  END",
                new
                {
                    UserId = userId,
                    Token = request.Token.Trim(),
                    Platform = string.IsNullOrWhiteSpace(request.Platform) ? "android" : request.Platform.Trim().ToLowerInvariant(),
                    DeviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? null : request.DeviceId.Trim()
                });
        }

        public async Task DisableDeviceTokenAsync(long userId, string token)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                @"UPDATE MobileDeviceTokens
                     SET IsActive = 0,
                         UpdatedAt = GETUTCDATE()
                   WHERE UserId = @UserId AND Token = @Token",
                new { UserId = userId, Token = token.Trim() });
        }

        public async Task<IEnumerable<PushNotificationCandidate>> GetPendingPushNotificationsAsync(int count = 100)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<PushNotificationCandidate>(
                @"SELECT TOP (@Count)
                         n.Id AS NotificationId,
                         token.Id AS DeviceTokenId,
                         token.Token,
                         n.Title,
                         n.Body,
                         n.PollId,
                         n.Type
                    FROM Notifications n
                    JOIN MobileDeviceTokens token
                      ON token.UserId = n.UserId
                     AND token.IsActive = 1
                    WHERE n.CreatedAt >= DATEADD(day, -2, GETUTCDATE())
                      AND NOT EXISTS (
                          SELECT 1
                          FROM NotificationPushDeliveries delivery
                          WHERE delivery.NotificationId = n.Id
                            AND delivery.DeviceTokenId = token.Id
                      )
                    ORDER BY n.CreatedAt ASC",
                new { Count = Math.Clamp(count, 1, 500) });
        }

        public async Task MarkPushAttemptAsync(
            long notificationId,
            long deviceTokenId,
            bool success,
            string? providerMessageId,
            string? errorMessage)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                @"IF NOT EXISTS (
                      SELECT 1
                      FROM NotificationPushDeliveries
                      WHERE NotificationId = @NotificationId AND DeviceTokenId = @DeviceTokenId
                  )
                  BEGIN
                      INSERT INTO NotificationPushDeliveries (
                          NotificationId, DeviceTokenId, Status, ProviderMessageId, ErrorMessage, AttemptedAt)
                      VALUES (
                          @NotificationId, @DeviceTokenId, @Status, @ProviderMessageId, @ErrorMessage, GETUTCDATE());
                  END",
                new
                {
                    NotificationId = notificationId,
                    DeviceTokenId = deviceTokenId,
                    Status = success ? "Sent" : "Failed",
                    ProviderMessageId = providerMessageId,
                    ErrorMessage = errorMessage
                });
        }

        public async Task<int> CreateDailyChallengeNotificationsAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(
                RetentionInsertSql(
                    NotificationType.ChallengeAvailable,
                    @"SELECT u.Id AS UserId,
                             eligible.Id AS PollId,
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
                             'A poll for today' AS Title,
                             'You haven''t voted today. Here''s a poll if you''d like to continue your streak.' AS Body
                      FROM Users u
                      CROSS APPLY (
                          SELECT TOP (1) p.Id
                          FROM Polls p
                          WHERE p.IsActive = 1 AND p.ModerationStatus = 'Published'
                            AND p.ExpiresAt > @UtcNow AND COALESCE(p.IsPrivate, 0) = 0
                            AND COALESCE(p.IsWellness, 0) = 0 AND p.Category <> 'Health'
                            AND NOT EXISTS (SELECT 1 FROM Votes v WHERE v.UserId = u.Id AND v.PollId = p.Id)
                          ORDER BY CASE WHEN EXISTS (SELECT 1 FROM UserCategoryPreferences pref
                              WHERE pref.UserId = u.Id AND pref.Category = p.Category) THEN 0 ELSE 1 END,
                              p.IsTrending DESC, p.TotalVotes DESC, p.CreatedAt DESC
                      ) eligible
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
