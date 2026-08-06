using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using System.Security.Cryptography;
using System.Text;

namespace BackendAPI.Repository;

public sealed class SocialRepository(DapperContext context) : ISocialRepository
{
    private static long Cursor(string? value) => long.TryParse(value, out var id) ? id : 0;
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<PagedResult<SocialUserSummary>> SearchUsersAsync(long actorId, string query, string? cursor, int limit)
    {
        limit = SocialLeagueRules.ClampLimit(limit); query = query.Trim();
        if (query.Length < 2) return new([], null);
        using var db = context.CreateConnection();
        var rows = (await db.QueryAsync<SocialUserSummary>(@"SELECT TOP (@Take) u.Id,u.Username,u.DisplayName,u.AvatarUrl FROM Users u
          WHERE u.Id>@After AND u.Id<>@Actor AND (u.Username LIKE @Query OR u.DisplayName LIKE @Query)
          AND NOT EXISTS(SELECT 1 FROM UserBlocks b WHERE (b.BlockerUserId=@Actor AND b.BlockedUserId=u.Id) OR (b.BlockerUserId=u.Id AND b.BlockedUserId=@Actor)) ORDER BY u.Id",
          new { Actor=actorId, Query=$"%{query}%", After=Cursor(cursor), Take=limit+1 })).ToList();
        return Page(rows, limit, x => x.Id.ToString());
    }

    public async Task<long> SendFriendRequestAsync(long actorId, long targetId)
    {
        if (actorId == targetId) throw new SocialConflictException("You cannot friend yourself.");
        using var db = context.CreateConnection(); db.Open(); using var tx=db.BeginTransaction(System.Data.IsolationLevel.Serializable);
        if (!await ExistsUser(db, tx, targetId)) throw new SocialNotFoundException("User not found.");
        await EnsureNotBlocked(db, tx, actorId, targetId);
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserRelationships WITH(UPDLOCK,HOLDLOCK) WHERE UserLowId=@Low AND UserHighId=@High AND State IN ('Pending','Accepted')", Pair(actorId,targetId),tx)>0)
            throw new SocialConflictException("A friendship or request already exists.");
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserRelationships WHERE RequesterUserId=@Actor AND CreatedAt>DATEADD(hour,-1,SYSUTCDATETIME())",new{Actor=actorId},tx)>=10)
            throw new SocialRateLimitException("Friend request limit reached. Try again later.");
        var existingId=await db.ExecuteScalarAsync<long?>("SELECT Id FROM UserRelationships WITH(UPDLOCK,HOLDLOCK) WHERE UserLowId=@Low AND UserHighId=@High",Pair(actorId,targetId),tx);
        var args=new{Actor=actorId,Target=targetId,Low=Math.Min(actorId,targetId),High=Math.Max(actorId,targetId),Id=existingId};
        var id=existingId.HasValue
            ? await db.ExecuteScalarAsync<long>("UPDATE UserRelationships SET RequesterUserId=@Actor,AddresseeUserId=@Target,State='Pending',LastActorUserId=@Actor,CreatedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME() OUTPUT inserted.Id WHERE Id=@Id",args,tx)
            : await db.ExecuteScalarAsync<long>(@"INSERT UserRelationships(RequesterUserId,AddresseeUserId,UserLowId,UserHighId,State,LastActorUserId) OUTPUT inserted.Id VALUES(@Actor,@Target,@Low,@High,'Pending',@Actor)",args,tx);
        tx.Commit(); return id;
    }

    public async Task<PagedResult<FriendConnection>> GetFriendsAsync(long actorId, RelationshipState? state, string? cursor, int limit)
    {
        limit=SocialLeagueRules.ClampLimit(limit); using var db=context.CreateConnection();
        var rows=(await db.QueryAsync<FriendRow>(@"SELECT TOP (@Take) r.Id,u.Id UserId,u.Username,u.DisplayName,u.AvatarUrl,r.State,r.UpdatedAt,CAST(CASE WHEN r.AddresseeUserId=@Actor THEN 1 ELSE 0 END AS bit) Incoming
          FROM UserRelationships r JOIN Users u ON u.Id=CASE WHEN r.RequesterUserId=@Actor THEN r.AddresseeUserId ELSE r.RequesterUserId END
          WHERE (r.RequesterUserId=@Actor OR r.AddresseeUserId=@Actor) AND r.Id>@After AND (@State IS NULL OR r.State=@State)
          AND NOT EXISTS(SELECT 1 FROM UserBlocks b WHERE (b.BlockerUserId=@Actor AND b.BlockedUserId=u.Id) OR (b.BlockerUserId=u.Id AND b.BlockedUserId=@Actor)) ORDER BY r.Id",
          new{Actor=actorId,After=Cursor(cursor),State=state?.ToString(),Take=limit+1})).ToList();
        return Page(rows.Select(x=>new FriendConnection(x.Id,new(x.UserId,x.Username,x.DisplayName,x.AvatarUrl),Enum.Parse<RelationshipState>(x.State),x.Incoming,x.UpdatedAt)).ToList(),limit,x=>x.Id.ToString());
    }

    public async Task ChangeFriendRequestAsync(long actorId,long relationshipId,bool accept)
    { using var db=context.CreateConnection(); var n=await db.ExecuteAsync("UPDATE UserRelationships SET State=@State,LastActorUserId=@Actor,UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND AddresseeUserId=@Actor AND State='Pending'",new{State=accept?"Accepted":"Declined",Actor=actorId,Id=relationshipId}); if(n==0) throw new SocialConflictException("Request is unavailable or cannot be changed by this user."); }
    public async Task RemoveFriendAsync(long actorId,long otherUserId)
    { using var db=context.CreateConnection(); var n=await db.ExecuteAsync("UPDATE UserRelationships SET State='Removed',LastActorUserId=@Actor,UpdatedAt=SYSUTCDATETIME() WHERE UserLowId=@Low AND UserHighId=@High AND State IN('Pending','Accepted')",new{Actor=actorId,Low=Math.Min(actorId,otherUserId),High=Math.Max(actorId,otherUserId)}); if(n==0) throw new SocialConflictException("Friendship not found."); }

    public async Task BlockAsync(long actorId,long targetId)
    {
        if(actorId==targetId) throw new SocialConflictException("You cannot block yourself.");
        using var db=context.CreateConnection(); db.Open(); using var tx=db.BeginTransaction(System.Data.IsolationLevel.Serializable);
        if(!await ExistsUser(db,tx,targetId)) throw new SocialNotFoundException("User not found.");
        await db.ExecuteAsync("IF NOT EXISTS(SELECT 1 FROM UserBlocks WITH(UPDLOCK,HOLDLOCK) WHERE BlockerUserId=@Actor AND BlockedUserId=@Target) INSERT UserBlocks(BlockerUserId,BlockedUserId) VALUES(@Actor,@Target)",new{Actor=actorId,Target=targetId},tx);
        await db.ExecuteAsync("UPDATE UserRelationships SET State='Removed',LastActorUserId=@Actor,UpdatedAt=SYSUTCDATETIME() WHERE UserLowId=@Low AND UserHighId=@High AND State IN('Pending','Accepted')",new{Actor=actorId,Low=Math.Min(actorId,targetId),High=Math.Max(actorId,targetId)},tx);
        await db.ExecuteAsync(@"UPDATE gi SET State='Cancelled',RespondedAt=SYSUTCDATETIME() FROM GroupInvites gi JOIN Groups g ON g.Id=gi.GroupId WHERE gi.State='Pending' AND ((gi.InviterUserId=@Actor AND gi.InviteeUserId=@Target) OR (gi.InviterUserId=@Target AND gi.InviteeUserId=@Actor) OR (g.OwnerUserId IN(@Actor,@Target) AND gi.InviteeUserId IN(@Actor,@Target)))",new{Actor=actorId,Target=targetId},tx);
        tx.Commit();
    }
    public async Task UnblockAsync(long actorId,long targetId){using var db=context.CreateConnection();await db.ExecuteAsync("DELETE UserBlocks WHERE BlockerUserId=@Actor AND BlockedUserId=@Target",new{Actor=actorId,Target=targetId});}

    public Task<WeeklyLeaderboard> GetFriendsLeaderboardAsync(long actorId,DateTime week,string? cursor,int limit)=>Leaderboard(actorId,null,week,cursor,limit);
    public async Task<SocialGroup> CreateGroupAsync(long actorId,string name)
    {
        name=name.Trim(); if(name.Length is < 2 or > 80) throw new SocialConflictException("Group name must be 2-80 characters.");
        using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction();
        var id=await db.ExecuteScalarAsync<long>("INSERT Groups(OwnerUserId,Name) OUTPUT inserted.Id VALUES(@Actor,@Name)",new{Actor=actorId,Name=name},tx);
        await db.ExecuteAsync("INSERT GroupMemberships(GroupId,UserId,Role,State,JoinedAt) VALUES(@Group,@Actor,'Owner','Active',SYSUTCDATETIME())",new{Group=id,Actor=actorId},tx);tx.Commit();
        return new(id,name,actorId,"Approved",1,GroupRole.Owner,DateTime.UtcNow);
    }
    public async Task<PagedResult<SocialGroup>> GetGroupsAsync(long actorId,string? cursor,int limit)
    { limit=SocialLeagueRules.ClampLimit(limit);using var db=context.CreateConnection();var rows=(await db.QueryAsync<SocialGroup>(@"SELECT TOP(@Take) g.Id,g.Name,g.OwnerUserId,g.ModerationStatus,(SELECT COUNT(*) FROM GroupMemberships x WHERE x.GroupId=g.Id AND x.State='Active') MemberCount,m.Role,g.CreatedAt FROM GroupMemberships m JOIN Groups g ON g.Id=m.GroupId WHERE m.UserId=@Actor AND m.State='Active' AND g.Status='Active' AND g.Id>@After ORDER BY g.Id",new{Actor=actorId,After=Cursor(cursor),Take=limit+1})).ToList();return Page(rows,limit,x=>x.Id.ToString());}
    public async Task<SocialGroup?> GetGroupAsync(long actorId,long groupId){using var db=context.CreateConnection();return await db.QuerySingleOrDefaultAsync<SocialGroup>(@"SELECT g.Id,g.Name,g.OwnerUserId,g.ModerationStatus,(SELECT COUNT(*) FROM GroupMemberships x WHERE x.GroupId=g.Id AND x.State='Active') MemberCount,m.Role,g.CreatedAt FROM GroupMemberships m JOIN Groups g ON g.Id=m.GroupId WHERE g.Id=@Group AND m.UserId=@Actor AND m.State='Active' AND g.Status='Active'",new{Actor=actorId,Group=groupId});}

    public async Task<string> InviteToGroupAsync(long actorId,long groupId,long targetId)
    {
        if(actorId==targetId) throw new SocialConflictException("You are already in this group."); using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var owner=await db.ExecuteScalarAsync<long?>("SELECT OwnerUserId FROM Groups WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Group AND Status='Active'",new{Group=groupId},tx);if(owner!=actorId)throw new SocialForbiddenException("Only the group owner can invite members."); await EnsureNotBlocked(db,tx,actorId,targetId);
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupMemberships WHERE GroupId=@Group AND State='Active'",new{Group=groupId},tx)>=SocialLeagueRules.MaxGroupMembers)throw new SocialConflictException("Group is full.");
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupInvites WHERE InviterUserId=@Actor AND CreatedAt>DATEADD(hour,-1,SYSUTCDATETIME())",new{Actor=actorId},tx)>=20)throw new SocialRateLimitException("Group invite limit reached.");
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupInvites WHERE InviteeUserId=@Target AND CreatedAt>DATEADD(hour,-1,SYSUTCDATETIME())",new{Target=targetId},tx)>=5)throw new SocialRateLimitException("This user has received too many invites.");
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupInvites WHERE GroupId=@Group AND CreatedAt>DATEADD(hour,-1,SYSUTCDATETIME())",new{Group=groupId},tx)>=20)throw new SocialRateLimitException("This group has sent too many invites.");
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupInvites WHERE GroupId=@Group AND InviteeUserId=@Target AND State='Pending' AND ExpiresAt>SYSUTCDATETIME()",new{Group=groupId,Target=targetId},tx)>0)throw new SocialConflictException("An active invite already exists.");
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(24));await db.ExecuteAsync("INSERT GroupInvites(GroupId,InviterUserId,InviteeUserId,TokenHash,ExpiresAt) VALUES(@Group,@Actor,@Target,@Hash,DATEADD(day,7,SYSUTCDATETIME()))",new{Group=groupId,Actor=actorId,Target=targetId,Hash=Hash(token)},tx);tx.Commit();return token;
    }
    public async Task<GroupInviteSummary?> GetInviteAsync(long actorId,string token){using var db=context.CreateConnection();var row=await db.QuerySingleOrDefaultAsync<InviteRow>(@"SELECT gi.GroupId,g.Name GroupName,gi.ExpiresAt,u.Id UserId,u.Username,u.DisplayName,u.AvatarUrl FROM GroupInvites gi JOIN Groups g ON g.Id=gi.GroupId JOIN Users u ON u.Id=gi.InviterUserId WHERE gi.TokenHash=@Hash AND gi.InviteeUserId=@Actor AND gi.State='Pending' AND gi.ExpiresAt>SYSUTCDATETIME() AND NOT EXISTS(SELECT 1 FROM UserBlocks b WHERE (b.BlockerUserId=@Actor AND b.BlockedUserId=gi.InviterUserId) OR (b.BlockerUserId=gi.InviterUserId AND b.BlockedUserId=@Actor))",new{Hash=Hash(token),Actor=actorId});return row is null?null:new(token,row.GroupId,row.GroupName,new(row.UserId,row.Username,row.DisplayName,row.AvatarUrl),row.ExpiresAt);}
    public async Task RespondToInviteAsync(long actorId,string token,bool accept)
    {using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction(System.Data.IsolationLevel.Serializable);var invite=await db.QuerySingleOrDefaultAsync<(long Id,long GroupId,long InviterUserId)>("SELECT Id,GroupId,InviterUserId FROM GroupInvites WITH(UPDLOCK,HOLDLOCK) WHERE TokenHash=@Hash AND InviteeUserId=@Actor AND State='Pending' AND ExpiresAt>SYSUTCDATETIME()",new{Hash=Hash(token),Actor=actorId},tx);if(invite.Id==0)throw new SocialConflictException("Invite is invalid or expired.");await EnsureNotBlocked(db,tx,actorId,invite.InviterUserId);await db.ExecuteAsync("UPDATE GroupInvites SET State=@State,RespondedAt=SYSUTCDATETIME() WHERE Id=@Id",new{State=accept?"Accepted":"Declined",invite.Id},tx);if(accept)await db.ExecuteAsync(@"MERGE GroupMemberships AS t USING(SELECT @Group GroupId,@Actor UserId)s ON t.GroupId=s.GroupId AND t.UserId=s.UserId WHEN MATCHED THEN UPDATE SET State='Active',Role='Member',JoinedAt=SYSUTCDATETIME(),LeftAt=NULL WHEN NOT MATCHED THEN INSERT(GroupId,UserId,Role,State,JoinedAt) VALUES(@Group,@Actor,'Member','Active',SYSUTCDATETIME());",new{Group=invite.GroupId,Actor=actorId},tx);tx.Commit();}
    public async Task LeaveGroupAsync(long actorId,long groupId){using var db=context.CreateConnection();var role=await db.ExecuteScalarAsync<string?>("SELECT Role FROM GroupMemberships WHERE GroupId=@Group AND UserId=@Actor AND State='Active'",new{Group=groupId,Actor=actorId});if(role is null)throw new SocialNotFoundException("Membership not found.");if(role=="Owner")throw new SocialConflictException("The owner must transfer ownership before leaving.");await db.ExecuteAsync("UPDATE GroupMemberships SET State='Left',LeftAt=SYSUTCDATETIME() WHERE GroupId=@Group AND UserId=@Actor",new{Group=groupId,Actor=actorId});}
    public Task<WeeklyLeaderboard> GetGroupLeaderboardAsync(long actorId,long groupId,DateTime week,string? cursor,int limit)=>Leaderboard(actorId,groupId,week,cursor,limit);

    private async Task<WeeklyLeaderboard> Leaderboard(long actorId,long? groupId,DateTime week,string? cursor,int limit)
    {
        limit=SocialLeagueRules.ClampLimit(limit);var start=SocialLeagueRules.WeekStart(week);var end=start.AddDays(7);using var db=context.CreateConnection();
        if(groupId.HasValue && await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GroupMemberships WHERE GroupId=@Group AND UserId=@Actor AND State='Active'",new{Group=groupId,Actor=actorId})==0)throw new SocialNotFoundException("Group not found.");
        var rows=(await db.QueryAsync<BoardRow>(@"WITH Eligible AS (SELECT @Actor UserId,CAST(@Start AS datetime2) JoinedAt WHERE @Group IS NULL UNION ALL SELECT CASE WHEN r.RequesterUserId=@Actor THEN r.AddresseeUserId ELSE r.RequesterUserId END,NULL FROM UserRelationships r WHERE @Group IS NULL AND r.State='Accepted' AND (r.RequesterUserId=@Actor OR r.AddresseeUserId=@Actor) UNION ALL SELECT m.UserId,m.JoinedAt FROM GroupMemberships m WHERE @Group IS NOT NULL AND m.GroupId=@Group AND m.State='Active'), Scores AS (SELECT e.UserId,COALESCE(SUM(x.Amount),0) Xp,COUNT(x.Id) ActivityCount FROM Eligible e LEFT JOIN XpEvents x ON x.UserId=e.UserId AND x.IsSociallyEligible=1 AND x.OccurredAt>=CASE WHEN e.JoinedAt>@Start THEN e.JoinedAt ELSE @Start END AND x.OccurredAt<@End WHERE NOT EXISTS(SELECT 1 FROM UserBlocks b WHERE (b.BlockerUserId=@Actor AND b.BlockedUserId=e.UserId) OR (b.BlockerUserId=e.UserId AND b.BlockedUserId=@Actor)) GROUP BY e.UserId), Ranked AS (SELECT ROW_NUMBER() OVER(ORDER BY s.Xp DESC,s.ActivityCount DESC,s.UserId) Rank,s.* FROM Scores s) SELECT TOP(@Take) r.Rank,u.Id UserId,u.Username,u.DisplayName,u.AvatarUrl,r.Xp,r.ActivityCount FROM Ranked r JOIN Users u ON u.Id=r.UserId WHERE r.Rank>@After ORDER BY r.Rank",new{Actor=actorId,Group=groupId,Start=start,End=end,After=Cursor(cursor),Take=limit+1})).ToList();var more=rows.Count>limit;if(more)rows.RemoveAt(rows.Count-1);return new(start,end,rows.Select(x=>new WeeklyLeaderboardEntry(x.Rank,new(x.UserId,x.Username,x.DisplayName,x.AvatarUrl),x.Xp,x.ActivityCount)).ToList(),more?rows[^1].Rank.ToString():null);
    }
    private static async Task EnsureNotBlocked(System.Data.IDbConnection db,System.Data.IDbTransaction tx,long a,long b){if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserBlocks WHERE (BlockerUserId=@A AND BlockedUserId=@B) OR (BlockerUserId=@B AND BlockedUserId=@A)",new{A=a,B=b},tx)>0)throw new SocialNotFoundException("User not found.");}
    private static Task<bool> ExistsUser(System.Data.IDbConnection db,System.Data.IDbTransaction tx,long id)=>db.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Users WHERE Id=@Id) THEN 1 ELSE 0 END AS bit)",new{Id=id},tx);
    private static object Pair(long a,long b)=>new{Low=Math.Min(a,b),High=Math.Max(a,b)};
    private static PagedResult<T> Page<T>(List<T> rows,int limit,Func<T,string> cursor){var more=rows.Count>limit;if(more)rows.RemoveAt(rows.Count-1);return new(rows,more?cursor(rows[^1]):null);}
    private sealed class FriendRow { public long Id{get;set;} public long UserId{get;set;} public string Username{get;set;}="";public string DisplayName{get;set;}="";public string? AvatarUrl{get;set;}public string State{get;set;}="";public bool Incoming{get;set;}public DateTime UpdatedAt{get;set;} }
    private sealed class InviteRow {public long GroupId{get;set;}public string GroupName{get;set;}="";public DateTime ExpiresAt{get;set;}public long UserId{get;set;}public string Username{get;set;}="";public string DisplayName{get;set;}="";public string? AvatarUrl{get;set;}}
    private sealed class BoardRow {public int Rank{get;set;}public long UserId{get;set;}public string Username{get;set;}="";public string DisplayName{get;set;}="";public string? AvatarUrl{get;set;}public int Xp{get;set;}public int ActivityCount{get;set;}}
}
