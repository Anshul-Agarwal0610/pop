using System.Text.Json;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Extensions.Options;

namespace BackendAPI.Repository;

public sealed class PhaseOneTopic : TrendingTopic
{
    public Guid LeaseId { get; set; }
    public string? PhaseOnePayload { get; set; }
    public string? PhaseOneProvider { get; set; }
    public string? PhaseOneModel { get; set; }
}

public sealed class PhaseOnePipelineRepository(DapperContext context, IOptions<PollQualityOptions> policy, IConfiguration config)
{
    public async Task<PhaseOneTopic?> ClaimAsync(string? language = null)
    {
        using var db = context.CreateConnection();
        // Execute recovery separately. Dapper's single-row query must receive the
        // UPDATE ... OUTPUT result as its first result set, otherwise it returns
        // null after the recovery UPDATE's row-count result.
        await db.ExecuteAsync("""
            UPDATE TrendingTopics SET ConversionStatus='NeedsReview',GenerationStatus='Terminal',ProcessingStatus='Review',IsProcessed=1,
              LastFailureReason='Generation attempts exhausted after worker interruption',LeaseId=NULL,LeaseExpiresAtUtc=NULL
            WHERE IsProcessed=0 AND PhaseOnePayload IS NULL AND AttemptCount>=3
              AND ConversionStatus='Processing' AND LeaseExpiresAtUtc<SYSUTCDATETIME();
            """);
        return await db.QuerySingleOrDefaultAsync<PhaseOneTopic>("""
            ;WITH due AS (
              SELECT TOP(1) * FROM TrendingTopics WITH(UPDLOCK,READPAST,ROWLOCK)
              WHERE IsProcessed=0 AND FetchedAt>=DATEADD(HOUR,-72,SYSUTCDATETIME())
                AND (Language='en' OR Language='hi' OR Language LIKE 'en-%' OR Language LIKE 'hi-%')
                AND (@Language IS NULL OR Language=@Language OR Language LIKE @Language + '-%')
                AND (PublishedAt IS NULL OR PublishedAt>=DATEADD(HOUR,-72,SYSUTCDATETIME()))
                AND (ConversionStatus IN ('Pending','RetryPending') OR
                     (ConversionStatus='Processing' AND LeaseExpiresAtUtc<SYSUTCDATETIME()))
                AND (NextAttemptAt IS NULL OR NextAttemptAt<=SYSUTCDATETIME())
                AND (AttemptCount<3 OR PhaseOnePayload IS NOT NULL)
              -- Recover already-paid provider output before claiming new work.
              ORDER BY CASE WHEN PhaseOnePayload IS NOT NULL THEN 0 ELSE 1 END, FetchedAt,Id)
            UPDATE due SET ConversionStatus='Processing',GenerationStatus='InProgress',ProcessingStatus='Processing',LeaseId=NEWID(),
              LeaseExpiresAtUtc=DATEADD(MINUTE,10,SYSUTCDATETIME()),LastAttemptAt=SYSUTCDATETIME(),
              AttemptCount=AttemptCount+CASE WHEN PhaseOnePayload IS NULL THEN 1 ELSE 0 END
            OUTPUT inserted.*;
            """, new { Language = language });
    }

    public async Task SaveOutputAsync(PhaseOneTopic topic, TopicBatchResult result)
    {
        using var db = context.CreateConnection();
        var payload = JsonSerializer.Serialize(result.Batch);
        var count = await db.ExecuteAsync("""
            UPDATE TrendingTopics SET PhaseOnePayload=@Payload,PhaseOneProvider=@Provider,PhaseOneModel=@Model
            WHERE Id=@Id AND LeaseId=@LeaseId AND ConversionStatus='Processing' AND LeaseExpiresAtUtc>SYSUTCDATETIME()
            """, new { topic.Id,topic.LeaseId,Payload=payload,result.Provider.Provider,result.Provider.Model });
        if (count != 1) throw new InvalidOperationException("Generation lease expired");
        topic.PhaseOnePayload = payload; topic.PhaseOneProvider = result.Provider.Provider; topic.PhaseOneModel = result.Provider.Model;
    }

    public async Task FailAsync(PhaseOneTopic topic, string reason, bool retry, DateTimeOffset? retryAt = null)
    {
        using var db = context.CreateConnection();
        await db.ExecuteAsync("""
            UPDATE TrendingTopics SET ConversionStatus=CASE WHEN @Retry=1 AND AttemptCount<3 THEN 'RetryPending' ELSE 'NeedsReview' END,
              GenerationStatus=CASE WHEN @Retry=1 AND AttemptCount<3 THEN 'RetryScheduled' ELSE 'Terminal' END,
              ProcessingStatus=CASE WHEN @Retry=1 AND AttemptCount<3 THEN 'RetryPending' ELSE 'Review' END,
              IsProcessed=CASE WHEN @Retry=1 AND AttemptCount<3 THEN 0 ELSE 1 END,
              NextAttemptAt=@Next,NextAttemptAtUtc=@Next,LastFailureReason=LEFT(@Reason,1000),
              LeaseId=NULL,LeaseExpiresAtUtc=NULL WHERE Id=@Id AND LeaseId=@LeaseId
            """,new {topic.Id,topic.LeaseId,Retry=retry,Reason=reason,Next=(retryAt > DateTimeOffset.UtcNow.AddMinutes(15) ? retryAt.Value : DateTimeOffset.UtcNow.AddMinutes(15)).UtcDateTime});
    }

    public async Task<int> PublishAsync(PhaseOneTopic topic)
    {
        var batch = JsonSerializer.Deserialize<TopicPollBatch>(topic.PhaseOnePayload!) ?? throw new InvalidOperationException("Missing saved output");
        using var db = context.CreateConnection(); db.Open(); using var tx = db.BeginTransaction();
        try
        {
            // Serialize the short publication transaction, not the external AI request.
            await db.ExecuteAsync("""
                DECLARE @result int;
                EXEC @result=sp_getapplock @Resource='phase-one-publish',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                IF @result<0 THROW 51000,'Publication lock unavailable',1;
                """, transaction:tx);
            var valid = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TrendingTopics WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id AND LeaseId=@LeaseId AND ConversionStatus='Processing' AND LeaseExpiresAtUtc>SYSUTCDATETIME()",new {topic.Id,topic.LeaseId},tx);
            if (valid != 1) throw new InvalidOperationException("Publication lease lost");
            var recent = (await db.QueryAsync<string>("SELECT TOP(1000) Question FROM Polls WHERE ModerationStatus='Published' ORDER BY CreatedAt DESC", transaction:tx)).ToList();
            int published=0; var decisions=new List<string>();
            for(int angle=0;angle<batch.Polls.Count;angle++)
            {
                var poll=batch.Polls[angle];
                var reason=TopicBatchGenerator.Validate(topic,poll,policy.Value,Math.Clamp(config.GetValue("PhaseOne:MinimumConfidence", .60), 0d, 1d));
                if(reason is null && recent.Any(q=>PhaseOneDuplicate(q,poll.Proposition))) reason="Duplicate question";
                if(reason is not null) { decisions.Add($"{angle}: {reason}"); continue; }
                var id=await db.ExecuteScalarAsync<long>("""
                    INSERT Polls(Question,Description,Category,ExpiresAt,IsActive,IsTrending,CreatedAt,TotalVotes,
                      SourceType,SourceUrl,ThumbnailUrl,IsAIGenerated,GenerationMethod,TrendingTopicId,GenerationProvider,GenerationModel,
                      IsPrivate,IsWellness,PollMode,ModerationStatus,ModerationReason,ReportCount,PhaseOneAngle,PublishedAtUtc)
                    OUTPUT inserted.Id
                    VALUES(@Question,@Description,@Category,DATEADD(HOUR,@Hours,SYSUTCDATETIME()),1,0,SYSUTCDATETIME(),0,
                      @SourceType,@SourceUrl,@ThumbnailUrl,1,'Llm',@Id,@Provider,@Model,0,0,'Public','Published',@Reason,0,@Angle,SYSUTCDATETIME())
                    """,new {Question=poll.Proposition,Description=SourceContext(topic),poll.Category,Hours=Math.Clamp(config.GetValue("PhaseOne:LifetimeHours",48),1,168),
                        topic.SourceType,topic.SourceUrl,topic.ThumbnailUrl,topic.Id,Provider=topic.PhaseOneProvider,Model=topic.PhaseOneModel,
                        Reason="Phase one deterministic validation v1; source excerpts retained in topic batch. No independent AI evaluator.",Angle=angle},tx);
                await db.ExecuteAsync("INSERT PollOptions(PollId,Text,Side,VoteCount) VALUES(@Id,@Text,@Side,0)",new {Id=id,Text=poll.SupportStatement,Side=GeneratedPollContract.Up},tx);
                await db.ExecuteAsync("INSERT PollOptions(PollId,Text,Side,VoteCount) VALUES(@Id,@Text,@Side,0)",new {Id=id,Text=poll.AgainstStatement,Side=GeneratedPollContract.Against},tx);
                recent.Add(poll.Proposition); published++;
            }
            await db.ExecuteAsync("""
                UPDATE TrendingTopics SET ConversionStatus=@Status,GenerationStatus='Completed',ProcessingStatus=@Processing,IsProcessed=1,ProcessedAt=SYSUTCDATETIME(),
                  LastFailureReason=@Reasons,LastGenerationMethod='Llm',LeaseId=NULL,LeaseExpiresAtUtc=NULL
                WHERE Id=@Id AND LeaseId=@LeaseId
                """,new {topic.Id,topic.LeaseId,Status=published>0?"Converted":"NeedsReview",Processing=published>0?"Published":"Review",Reasons=string.Join("; ",decisions).Substring(0,Math.Min(1000,string.Join("; ",decisions).Length))},tx);
            tx.Commit(); return published;
        }
        catch { tx.Rollback(); throw; }
    }

    internal static bool PhaseOneDuplicate(string a,string b)
    {
        var x=GeneratedPollDuplicateDetector.Normalize(a).Split(' ',StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var y=GeneratedPollDuplicateDetector.Normalize(b).Split(' ',StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        return x.Count>0 && y.Count>0 && (double)x.Intersect(y).Count()/x.Union(y).Count()>=.85;
    }

    private static string SourceContext(TrendingTopic topic)
    {
        var title = TopicEnrichment.CleanText(topic.Title);
        var summary = TopicEnrichment.CleanText(topic.Summary);
        var context = string.IsNullOrWhiteSpace(summary) || summary.Equals(title, StringComparison.Ordinal)
            ? title
            : $"{title}\n\n{summary}";
        return context.Length <= 1000 ? context : context[..1000];
    }
}
