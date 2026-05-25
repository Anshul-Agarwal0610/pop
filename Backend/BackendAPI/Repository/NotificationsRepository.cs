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
            return await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO Notifications (UserId, Type, Title, Body, PollId, IsRead, CreatedAt)
                  VALUES (@UserId, @Type, @Title, @Body, @PollId, 0, GETUTCDATE());
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                new
                {
                    request.UserId,
                    Type = request.Type.ToString(),
                    request.Title,
                    request.Body,
                    request.PollId
                }
            );
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
    }
}
