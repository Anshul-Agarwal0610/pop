using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Analytics;
using Dapper;

namespace BackendAPI.Repository;

public class AchievementsRepository(DapperContext context, IAnalyticsOutbox analytics) : IAchievementsRepository
{
    private const string BadgeSelectSql = @"SELECT ub.Id, ub.UserId, ub.BadgeId, b.Code, b.Name, b.Description, b.Icon,
        b.RewardXp, b.RewardTitle, ub.AwardedAt FROM UserBadges ub JOIN AchievementBadges b ON b.Id = ub.BadgeId";

    public async Task<IEnumerable<UserBadge>> GetUserBadgesAsync(long userId)
    {
        using var conn = context.CreateConnection();
        return await conn.QueryAsync<UserBadge>(BadgeSelectSql + " WHERE ub.UserId=@UserId ORDER BY ub.AwardedAt DESC", new { UserId = userId });
    }

    public async Task<Dictionary<long, List<UserBadge>>> GetBadgesForUsersAsync(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        using var conn = context.CreateConnection();
        var rows = await conn.QueryAsync<UserBadge>(BadgeSelectSql + " WHERE ub.UserId IN @UserIds AND b.IsPublic=1 ORDER BY ub.AwardedAt DESC", new { UserIds = ids });
        return rows.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.ToList());
    }

    public async Task<AchievementCollectionResponse> GetCollectionAsync(long userId)
    {
        using var conn = context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(MetricsSql + @"
            SELECT * FROM AchievementBadges WHERE IsActive=1 ORDER BY Category,SortOrder,Id;
            " + BadgeSelectSql + @" WHERE ub.UserId=@UserId;
            SELECT u.SelectedTitleBadgeId, b.RewardTitle AS SelectedTitle FROM Users u LEFT JOIN AchievementBadges b ON b.Id=u.SelectedTitleBadgeId WHERE u.Id=@UserId;", new { UserId = userId });
        var metrics = await multi.ReadSingleAsync<AchievementMetrics>();
        var definitions = (await multi.ReadAsync<AchievementBadge>()).ToList();
        var earned = (await multi.ReadAsync<UserBadge>()).ToDictionary(x => x.BadgeId);
        var title = await multi.ReadSingleOrDefaultAsync<SelectedTitleRow>();
        var items = definitions.Select(x => AchievementPresenter.Present(x, earned.GetValueOrDefault(x.Id), metrics)).ToList();
        return new AchievementCollectionResponse { Achievements = items, EarnedCount = items.Count(x => x.Status == AchievementStatus.Earned),
            TotalCount = items.Count, SelectedTitle = title?.SelectedTitle, SelectedTitleBadgeId = title?.SelectedTitleBadgeId };
    }

    public async Task<AchievementOverview> GetOverviewAsync(long userId)
    {
        var collection = await GetCollectionAsync(userId);
        var recent = (await GetUserBadgesAsync(userId)).Take(3).ToList();
        var next = collection.Achievements
            .Where(item => item.Status != AchievementStatus.Earned && item.CurrentProgress.HasValue && item.TargetProgress.HasValue)
            .OrderByDescending(item => item.ProgressPercent ?? 0)
            .ThenBy(item => item.TargetProgress!.Value - item.CurrentProgress!.Value)
            .Take(3)
            .Select(item => new AchievementProgress
            {
                BadgeId = item.BadgeId, Code = item.Code, Name = item.Name, Description = item.Description,
                Icon = item.Icon, CurrentValue = item.CurrentProgress!.Value, Threshold = item.TargetProgress!.Value,
                ProgressPercent = item.ProgressPercent ?? 0, RewardXp = item.RewardXp
            }).ToList();
        return new AchievementOverview { RecentlyEarned = recent, NextAchievable = next, AllEarned = collection.EarnedCount == collection.TotalCount };
    }

    public async Task<PublicAchievementsResponse> GetPublicAchievementsAsync(long userId)
    {
        using var conn = context.CreateConnection();
        using var multi = await conn.QueryMultipleAsync(@"SELECT b.Id BadgeId,b.Name,b.Description,b.Icon,b.Category,ub.AwardedAt,b.RewardTitle
            FROM UserBadges ub JOIN AchievementBadges b ON b.Id=ub.BadgeId WHERE ub.UserId=@UserId AND b.IsPublic=1 ORDER BY ub.AwardedAt DESC;
            SELECT b.RewardTitle FROM Users u LEFT JOIN AchievementBadges b ON b.Id=u.SelectedTitleBadgeId
            WHERE u.Id=@UserId AND EXISTS(SELECT 1 FROM UserBadges ub WHERE ub.UserId=u.Id AND ub.BadgeId=b.Id);", new { UserId = userId });
        return new PublicAchievementsResponse { Achievements = await multi.ReadAsync<PublicAchievement>(), SelectedTitle = await multi.ReadSingleOrDefaultAsync<string>() };
    }

    public async Task<IEnumerable<AchievementCelebration>> ClaimPendingCelebrationsAsync(long userId, DateTime utcNow)
    {
        using var conn = context.CreateConnection();
        return await conn.QueryAsync<AchievementCelebration>(@"UPDATE ub WITH (UPDLOCK,READPAST,ROWLOCK) SET CelebrationClaimedAt=@UtcNow
            OUTPUT inserted.Id,inserted.UserId,inserted.BadgeId,b.Code,b.Name,b.Description,b.Icon,b.RewardXp,b.RewardTitle,inserted.AwardedAt
            FROM UserBadges ub JOIN AchievementBadges b ON b.Id=ub.BadgeId WHERE ub.UserId=@UserId AND ub.CelebrationClaimedAt IS NULL;", new { UserId = userId, UtcNow = utcNow });
    }

    public async Task<bool> SelectTitleAsync(long userId, long badgeId)
    {
        using var conn = context.CreateConnection();
        return await conn.ExecuteAsync(@"UPDATE Users SET SelectedTitleBadgeId=@BadgeId WHERE Id=@UserId AND EXISTS(
            SELECT 1 FROM UserBadges ub JOIN AchievementBadges b ON b.Id=ub.BadgeId WHERE ub.UserId=@UserId AND ub.BadgeId=@BadgeId AND b.RewardTitle IS NOT NULL)", new { UserId = userId, BadgeId = badgeId }) == 1;
    }

    public async Task ClearTitleAsync(long userId)
    { using var conn = context.CreateConnection(); await conn.ExecuteAsync("UPDATE Users SET SelectedTitleBadgeId=NULL WHERE Id=@UserId", new { UserId = userId }); }

    public async Task<AchievementAwardResult> AwardEligibleBadgesAsync(long userId, DateTime utcNow)
    {
        using var conn = context.CreateConnection(); conn.Open(); using var tx = conn.BeginTransaction();
        try
        {
            var metrics = await conn.QuerySingleAsync<AchievementMetrics>(MetricsSql, new { UserId = userId }, tx);
            var definitions = (await conn.QueryAsync<AchievementBadge>("SELECT * FROM AchievementBadges WITH (HOLDLOCK) WHERE IsActive=1", transaction: tx)).Where(x => metrics.ForRule(x.RuleType) >= x.Threshold).ToList();
            var awarded = new List<UserBadge>();
            foreach (var b in definitions)
            {
                var id = await conn.ExecuteScalarAsync<long?>(@"INSERT INTO UserBadges(UserId,BadgeId,AwardedAt)
                    OUTPUT inserted.Id SELECT @UserId,@BadgeId,@UtcNow WHERE NOT EXISTS(SELECT 1 FROM UserBadges WITH (UPDLOCK,HOLDLOCK) WHERE UserId=@UserId AND BadgeId=@BadgeId);",
                    new { UserId = userId, BadgeId = b.Id, UtcNow = utcNow }, tx);
                if (id is null) continue;
                if (b.RewardXp > 0)
                    await conn.ExecuteAsync(@"
                        INSERT INTO XpEvents (UserId, Amount, SourceType, BadgeId, OccurredAt, IsValid, IsLeaderboardEligible)
                        VALUES (@UserId, @RewardXp, 'Achievement', @BadgeId, @UtcNow, 1, @Eligible)",
                        new { UserId = userId, b.RewardXp, BadgeId = b.Id, UtcNow = utcNow,
                            Eligible = b.RuleType != AchievementRuleType.PollCreation }, tx);
                awarded.Add(new UserBadge { Id=id.Value,UserId=userId,BadgeId=b.Id,Code=b.Code,Name=b.Name,Description=b.Description,Icon=b.Icon,RewardXp=b.RewardXp,RewardTitle=b.RewardTitle,AwardedAt=utcNow });
                var consent = await conn.ExecuteScalarAsync<string>("SELECT AnalyticsConsent FROM Users WHERE Id=@UserId", new { UserId = userId }, tx);
                if (consent == "granted")
                    await analytics.EnqueueAsync(conn, tx, new AnalyticsEvent(Guid.NewGuid(), AnalyticsEventNames.AchievementUnlocked, $"usr_{userId}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["achievement_code"] = b.Code, ["reward_xp"] = b.RewardXp }, "achievement_code", "reward_xp"), utcNow, $"achievement:{userId}:{b.Id}"));
            }
            var xp = awarded.Sum(x => x.RewardXp);
            if (xp > 0) await conn.ExecuteAsync("UPDATE Users SET Xp=Xp+@Xp WHERE Id=@UserId", new { Xp=xp,UserId=userId }, tx);
            tx.Commit(); return new AchievementAwardResult { AwardedBadges=awarded,BonusXpAwarded=xp };
        }
        catch { tx.Rollback(); throw; }
    }

    private const string MetricsSql = @"SELECT u.TotalVotes,u.Streak,u.PollsCreated,
        (SELECT COUNT(1) FROM UserChallengeProgress c WHERE c.UserId=u.Id AND c.IsCompleted=1) CompletedChallenges,
        (SELECT COUNT(DISTINCT p.Category) FROM Votes v JOIN Polls p ON p.Id=v.PollId WHERE v.UserId=u.Id AND COALESCE(p.IsPrivate,0)=0 AND COALESCE(p.IsWellness,0)=0) DistinctCategoriesVoted
        FROM Users u WHERE u.Id=@UserId;";
    private class SelectedTitleRow { public long? SelectedTitleBadgeId { get; set; } public string? SelectedTitle { get; set; } }
}
