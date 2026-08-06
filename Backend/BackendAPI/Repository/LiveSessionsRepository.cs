using System.Data;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class LiveSessionsRepository(DapperContext context) : ILiveSessionsRepository
{
    private static readonly TimeSpan RevealDelay = TimeSpan.FromSeconds(2);

    public async Task<bool> IsMemberAsync(Guid sessionId, long userId)
    {
        using var db = context.CreateConnection();
        return await db.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM dbo.LiveSessions s
            JOIN dbo.LiveSessionMembers m ON m.SessionId=s.Id
            WHERE s.PublicId=@sessionId AND m.UserId=@userId AND m.Status='Active'
            """, new { sessionId, userId }) > 0;
    }

    public async Task<LiveSessionStateDto?> GetAsync(Guid sessionId, long userId, DateTime utcNow)
    {
        using var db = context.CreateConnection();
        await PromoteRevealAsync(db, null, sessionId, utcNow);
        return await ReadStateAsync(db, null, sessionId, userId, utcNow);
    }

    public async Task<LiveSessionStateDto> SetReadyAsync(Guid sessionId, long userId, bool ready, DateTime utcNow)
    {
        using var db = context.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        var changed = await db.ExecuteAsync("""
            UPDATE m WITH (UPDLOCK) SET IsReady=@ready, UpdatedAt=@utcNow
            FROM dbo.LiveSessionMembers m JOIN dbo.LiveSessions s ON s.Id=m.SessionId
            WHERE s.PublicId=@sessionId AND m.UserId=@userId AND m.Status='Active' AND s.Status='Lobby'
              AND m.IsReady<>@ready;
            """, new { sessionId, userId, ready, utcNow }, tx);
        if (!await IsMemberAsync(db, tx, sessionId, userId)) throw new LiveSessionException("not_found", "Live session not found.");
        if (changed > 0)
            await db.ExecuteAsync("UPDATE dbo.LiveSessions SET StateVersion=StateVersion+1, UpdatedAt=@utcNow WHERE PublicId=@sessionId", new { sessionId, utcNow }, tx);
        tx.Commit();
        return (await GetAsync(sessionId, userId, utcNow))!;
    }

    public async Task<LiveVoteResult> VoteAsync(Guid sessionId, int round, long userId, LiveVoteRequest request, DateTime utcNow)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new LiveSessionException("invalid_idempotency_key", "An idempotency key is required.");
        using var db = context.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);

        var row = await db.QuerySingleOrDefaultAsync<VoteContext>("""
            SELECT s.Id SessionKey, s.Status, s.CurrentRound, r.Id RoundId, r.RevealAt,
                   m.Id MemberId, m.EligibleFromRound
            FROM dbo.LiveSessions s WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.LiveSessionMembers m ON m.SessionId=s.Id AND m.UserId=@userId AND m.Status='Active'
            JOIN dbo.LiveSessionRounds r WITH (UPDLOCK, HOLDLOCK) ON r.SessionId=s.Id AND r.RoundNumber=s.CurrentRound
            WHERE s.PublicId=@sessionId
            """, new { sessionId, userId }, tx);
        if (row is null) throw new LiveSessionException("not_found", "Live session not found.");
        if (row.CurrentRound != round) throw new LiveSessionException("stale_round", "The round has changed; refresh session state.");

        var previous = await db.QuerySingleOrDefaultAsync<ExistingVote>("""
            SELECT OptionId, IdempotencyKey FROM dbo.LiveSessionVotes
            WHERE RoundId=@RoundId AND MemberId=@MemberId
            """, row, tx);
        if (previous is not null)
        {
            if (previous.IdempotencyKey != request.IdempotencyKey || previous.OptionId != request.OptionId)
                throw new LiveSessionException("idempotency_conflict", "This participant already locked a different vote.");
            tx.Commit();
            return new LiveVoteResult((await GetAsync(sessionId, userId, utcNow))!, true, false);
        }
        if (row.Status != LiveSessionStatuses.Voting || row.RevealAt.HasValue)
            throw new LiveSessionException("round_not_voting", "Voting is closed for this round.");

        var validOption = await db.ExecuteScalarAsync<int>("""
            SELECT COUNT(1) FROM dbo.LiveSessionRounds r JOIN dbo.PollOptions o ON o.PollId=r.PollId
            WHERE r.Id=@RoundId AND o.Id=@OptionId
            """, new { row.RoundId, request.OptionId }, tx) == 1;
        if (!validOption) throw new LiveSessionException("invalid_option", "The option is not part of this round.");

        await db.ExecuteAsync("""
            INSERT dbo.LiveSessionVotes(RoundId,MemberId,OptionId,IdempotencyKey,LockedAt)
            VALUES(@RoundId,@MemberId,@OptionId,@IdempotencyKey,@utcNow)
            """, new { row.RoundId, row.MemberId, request.OptionId, request.IdempotencyKey, utcNow }, tx);

        var counts = await db.QuerySingleAsync<LockCounts>("""
            SELECT COUNT(*) EligibleCount,
                   SUM(CASE WHEN v.Id IS NULL THEN 0 ELSE 1 END) LockedCount
            FROM dbo.LiveSessionMembers m
            LEFT JOIN dbo.LiveSessionVotes v ON v.MemberId=m.Id AND v.RoundId=@RoundId
            WHERE m.SessionId=@SessionKey AND m.Status='Active' AND m.EligibleFromRound<=@CurrentRound
            """, row, tx);
        var scheduled = LiveSessionRules.ShouldScheduleReveal(row.Status, counts.EligibleCount, counts.LockedCount);
        var revealAt = scheduled ? LiveSessionRules.RevealDeadline(utcNow, RevealDelay) : (DateTime?)null;
        await db.ExecuteAsync("""
            UPDATE dbo.LiveSessions SET StateVersion=StateVersion+1, UpdatedAt=@utcNow WHERE Id=@SessionKey;
            UPDATE dbo.LiveSessionRounds SET RevealAt=COALESCE(RevealAt,@revealAt), UpdatedAt=@utcNow WHERE Id=@RoundId;
            """, new { row.SessionKey, row.RoundId, utcNow, revealAt }, tx);
        tx.Commit();
        return new LiveVoteResult((await GetAsync(sessionId, userId, utcNow))!, false, scheduled);
    }

    public async Task<LiveSessionStateDto> CompleteAsync(Guid sessionId, long userId, DateTime utcNow)
    {
        using var db = context.CreateConnection();
        var changed = await db.ExecuteAsync("""
            UPDATE s SET Status='Completed',CompletedAt=@utcNow,UpdatedAt=@utcNow,StateVersion=StateVersion+1
            FROM dbo.LiveSessions s JOIN dbo.LiveSessionMembers m ON m.SessionId=s.Id
            WHERE s.PublicId=@sessionId AND s.HostUserId=@userId AND m.UserId=@userId AND m.Status='Active'
              AND s.Status='Revealed'
            """, new { sessionId, userId, utcNow });
        if (changed == 0)
        {
            if (!await IsMemberAsync(sessionId, userId)) throw new LiveSessionException("not_found", "Live session not found.");
            throw new LiveSessionException("invalid_transition", "Only the host can complete a revealed session.");
        }
        return (await GetAsync(sessionId, userId, utcNow))!;
    }

    private static async Task PromoteRevealAsync(IDbConnection db, IDbTransaction? tx, Guid sessionId, DateTime utcNow) =>
        await db.ExecuteAsync("""
            UPDATE s SET Status='Revealed', StateVersion=StateVersion+1, UpdatedAt=@utcNow
            FROM dbo.LiveSessions s JOIN dbo.LiveSessionRounds r ON r.SessionId=s.Id AND r.RoundNumber=s.CurrentRound
            WHERE s.PublicId=@sessionId AND s.Status='Voting' AND r.RevealAt IS NOT NULL AND r.RevealAt<=@utcNow;
            UPDATE r SET Status='Revealed', RevealedAt=COALESCE(RevealedAt,@utcNow), UpdatedAt=@utcNow
            FROM dbo.LiveSessionRounds r JOIN dbo.LiveSessions s ON s.Id=r.SessionId
            WHERE s.PublicId=@sessionId AND s.Status='Revealed' AND r.RoundNumber=s.CurrentRound AND r.Status='Voting';
            """, new { sessionId, utcNow }, tx);

    private static async Task<LiveSessionStateDto?> ReadStateAsync(IDbConnection db, IDbTransaction? tx, Guid sessionId, long userId, DateTime utcNow)
    {
        using var multi = await db.QueryMultipleAsync("""
            SELECT s.Id SessionKey,s.PublicId SessionId,s.Status,s.StateVersion,s.CurrentRound,
                   r.Id RoundId,r.PollId,p.Question,r.RevealAt,r.RevealedAt,
                   (SELECT COUNT(*) FROM dbo.LiveSessionMembers em WHERE em.SessionId=s.Id AND em.Status='Active' AND em.EligibleFromRound<=s.CurrentRound) EligibleCount,
                   (SELECT COUNT(*) FROM dbo.LiveSessionVotes ev JOIN dbo.LiveSessionMembers em ON em.Id=ev.MemberId WHERE ev.RoundId=r.Id AND em.Status='Active' AND em.EligibleFromRound<=s.CurrentRound) LockedCount,
                   mine.OptionId MyOptionId
            FROM dbo.LiveSessions s
            JOIN dbo.LiveSessionMembers me ON me.SessionId=s.Id AND me.UserId=@userId AND me.Status='Active'
            LEFT JOIN dbo.LiveSessionRounds r ON r.SessionId=s.Id AND r.RoundNumber=s.CurrentRound
            LEFT JOIN dbo.Polls p ON p.Id=r.PollId
            LEFT JOIN dbo.LiveSessionVotes mine ON mine.RoundId=r.Id AND mine.MemberId=me.Id
            WHERE s.PublicId=@sessionId;
            SELECT m.UserId,u.DisplayName,m.IsReady,CAST(CASE WHEN v.Id IS NULL THEN 0 ELSE 1 END AS bit) IsLocked
            FROM dbo.LiveSessions s JOIN dbo.LiveSessionMembers m ON m.SessionId=s.Id AND m.Status='Active'
            JOIN dbo.Users u ON u.Id=m.UserId LEFT JOIN dbo.LiveSessionRounds r ON r.SessionId=s.Id AND r.RoundNumber=s.CurrentRound
            LEFT JOIN dbo.LiveSessionVotes v ON v.RoundId=r.Id AND v.MemberId=m.Id WHERE s.PublicId=@sessionId;
            SELECT o.Id,o.Text,COUNT(v.Id) VoteCount FROM dbo.LiveSessions s
            JOIN dbo.LiveSessionRounds r ON r.SessionId=s.Id AND r.RoundNumber=s.CurrentRound
            JOIN dbo.PollOptions o ON o.PollId=r.PollId LEFT JOIN dbo.LiveSessionVotes v ON v.OptionId=o.Id AND v.RoundId=r.Id
            WHERE s.PublicId=@sessionId GROUP BY o.Id,o.Text ORDER BY o.Id;
            """, new { sessionId, userId }, tx);
        var header = await multi.ReadSingleOrDefaultAsync<StateRow>();
        if (header is null) return null;
        var members = (await multi.ReadAsync<LiveParticipantDto>()).AsList();
        var rawOptions = (await multi.ReadAsync<OptionRow>()).AsList();
        var exposed = LiveSessionRules.CanExposeResults(header.Status, header.RevealedAt);
        var options = rawOptions.Select(x => new LiveOptionDto(x.Id, x.Text, exposed ? x.VoteCount : null)).ToList();
        var winner = exposed && rawOptions.Count > 0 ? rawOptions.OrderByDescending(x => x.VoteCount).ThenBy(x => x.Id).First().Id : (long?)null;
        return new LiveSessionStateDto { SessionId=header.SessionId, Status=header.Status, StateVersion=header.StateVersion,
            ServerNow=utcNow, CurrentRound=header.CurrentRound, PollId=header.PollId, Question=header.Question,
            RevealAt=header.RevealAt, EligibleCount=header.EligibleCount, LockedCount=header.LockedCount,
            MyOptionId=header.MyOptionId, Participants=members, Options=options,
            Reveal=exposed ? new LiveRevealDto(winner, options) : null };
    }

    private static async Task<bool> IsMemberAsync(IDbConnection db, IDbTransaction tx, Guid sessionId, long userId) =>
        await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.LiveSessions s JOIN dbo.LiveSessionMembers m ON m.SessionId=s.Id WHERE s.PublicId=@sessionId AND m.UserId=@userId AND m.Status='Active'", new { sessionId, userId }, tx) > 0;

    private sealed class VoteContext { public long SessionKey { get; init; } public string Status { get; init; }=""; public int CurrentRound { get; init; } public long RoundId { get; init; } public long MemberId { get; init; } public int EligibleFromRound { get; init; } public DateTime? RevealAt { get; init; } }
    private sealed class ExistingVote { public long OptionId { get; init; } public Guid IdempotencyKey { get; init; } }
    private sealed class LockCounts { public int EligibleCount { get; init; } public int LockedCount { get; init; } }
    private sealed class StateRow { public Guid SessionId { get; init; } public string Status { get; init; }=""; public long StateVersion { get; init; } public int CurrentRound { get; init; } public long? PollId { get; init; } public string? Question { get; init; } public DateTime? RevealAt { get; init; } public DateTime? RevealedAt { get; init; } public int EligibleCount { get; init; } public int LockedCount { get; init; } public long? MyOptionId { get; init; } }
    private sealed class OptionRow { public long Id { get; init; } public string Text { get; init; }=""; public int VoteCount { get; init; } }
}
