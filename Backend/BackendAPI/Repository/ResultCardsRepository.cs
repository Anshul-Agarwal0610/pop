using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using System.Security.Cryptography;

namespace BackendAPI.Repository;

public sealed class ResultCardsRepository(DapperContext context, ResultCardFactory factory, ISystemClock clock, IConfiguration configuration) : IResultCardsRepository
{
    private string PublicBaseUrl => (configuration["PublicAppUrl"] ?? "http://localhost:3000").TrimEnd('/');
    private string PublicApiUrl => (configuration["PublicApiUrl"] ?? "http://localhost:5177").TrimEnd('/');

    public async Task<ResultCardDto> IssueAsync(NormalizedMultiplayerResult result)
    {
        var payload = factory.Create(result);
        var now = clock.UtcNow;
        var stored = new StoredResultCard { SessionId = result.SessionId, OwnerUserId = result.OwnerUserId,
            SchemaVersion = ResultCardFactory.SchemaVersion, PublicToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            PayloadJson = factory.Serialize(payload), PayloadHash = factory.Hash(payload), CreatedAt = now, ExpiresAt = now.AddDays(365) };
        using var conn = context.CreateConnection();
        var row = await conn.QuerySingleAsync<StoredResultCard>(@"
MERGE MultiplayerResultCards WITH (HOLDLOCK) AS target
USING (SELECT @SessionId SessionId,@OwnerUserId OwnerUserId,@SchemaVersion SchemaVersion) source
ON target.SessionId=source.SessionId AND target.OwnerUserId=source.OwnerUserId AND target.SchemaVersion=source.SchemaVersion
WHEN MATCHED THEN UPDATE SET PayloadHash=target.PayloadHash
WHEN NOT MATCHED THEN INSERT(PublicToken,SessionId,OwnerUserId,SchemaVersion,PayloadJson,PayloadHash,CreatedAt,ExpiresAt)
VALUES(@PublicToken,@SessionId,@OwnerUserId,@SchemaVersion,@PayloadJson,@PayloadHash,@CreatedAt,@ExpiresAt)
OUTPUT inserted.*;", stored);
        return ToDto(row);
    }

    public async Task<ResultCardDto?> GetForParticipantAsync(long sessionId, long userId)
    {
        using var conn = context.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<StoredResultCard>("SELECT TOP 1 * FROM MultiplayerResultCards WHERE SessionId=@sessionId AND OwnerUserId=@userId AND RevokedAt IS NULL ORDER BY SchemaVersion DESC", new { sessionId, userId });
        return row is null ? null : ToDto(row);
    }

    public async Task<ResultCardPage> GetMineAsync(long userId, int offset, int limit)
    {
        using var conn = context.CreateConnection();
        var rows = (await conn.QueryAsync<StoredResultCard>("SELECT * FROM MultiplayerResultCards WHERE OwnerUserId=@userId ORDER BY CreatedAt DESC,Id DESC OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY", new { userId, offset, take = limit + 1 })).ToList();
        return new ResultCardPage(rows.Take(limit).Select(ToDto).ToList(), offset, limit, rows.Count > limit);
    }

    public async Task<StoredResultCard?> GetPublicAsync(string token)
    {
        using var conn = context.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<StoredResultCard>("SELECT * FROM MultiplayerResultCards WHERE PublicToken=@token", new { token });
    }

    private ResultCardDto ToDto(StoredResultCard row) => new(row.Id, row.PublicToken, factory.Deserialize(row.PayloadJson),
        $"{PublicBaseUrl}/live/cards/{row.PublicToken}", $"{PublicApiUrl}/api/result-cards/public/{row.PublicToken}/image", row.CreatedAt, row.ExpiresAt);
}
