using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository;

public sealed class PollTossInvitationRepository(DapperContext context) : IPollTossInvitationRepository
{
    public async Task CreateAsync(PollTossInvitation value)
    {
        using var db = context.CreateConnection();
        await db.ExecuteAsync("""INSERT INTO PollTossInvitations (Id,TokenHash,PollId,CreatorUserId,CreatedAt,ExpiresAt) VALUES (@Id,@TokenHash,@PollId,@CreatorUserId,@CreatedAt,@ExpiresAt)""", value);
    }

    public async Task<PollTossInvitation?> ConsumeAsync(byte[] tokenHash, DateTime now)
    {
        using var db = context.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<PollTossInvitation>("""
            UPDATE PollTossInvitations WITH (UPDLOCK, ROWLOCK)
            SET ConsumedAt=@Now OUTPUT inserted.*
            WHERE TokenHash=@TokenHash AND ExpiresAt>@Now AND ConsumedAt IS NULL AND RevokedAt IS NULL
            """, new { TokenHash = tokenHash, Now = now });
    }

    public async Task<bool> RevokeAsync(Guid id, long creatorUserId, DateTime now)
    {
        using var db = context.CreateConnection();
        return await db.ExecuteAsync("UPDATE PollTossInvitations SET RevokedAt=@Now WHERE Id=@Id AND CreatorUserId=@CreatorUserId AND ConsumedAt IS NULL AND RevokedAt IS NULL", new { Id=id, CreatorUserId=creatorUserId, Now=now }) > 0;
    }

    public async Task<int> PurgeExpiredAsync(DateTime cutoff)
    {
        using var db = context.CreateConnection();
        return await db.ExecuteAsync("DELETE FROM PollTossInvitations WHERE ExpiresAt < @Cutoff", new { Cutoff=cutoff });
    }
}
