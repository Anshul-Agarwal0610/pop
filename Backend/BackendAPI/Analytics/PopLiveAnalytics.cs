using BackendAPI.Data;
using Dapper;

namespace BackendAPI.Analytics;

// Called only after the product transition commits. This facade deliberately owns a
// separate connection/transaction and swallows failures so analytics cannot block play.
public sealed class PopLiveAnalytics(DapperContext context, IAnalyticsOutbox outbox, IFeatureFlagService flags, ILogger<PopLiveAnalytics> logger) : IPopLiveAnalytics
{
    public async Task TrackAsync(int actorId, string eventName, string semanticKey, PopLiveEventProperties properties, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = context.CreateConnection();
            connection.Open();
            if (await connection.ExecuteScalarAsync<string>(new CommandDefinition("SELECT AnalyticsConsent FROM Users WHERE Id=@actorId", new { actorId }, cancellationToken: cancellationToken)) != "granted") return;
            var actorKey = $"usr_{actorId}";
            var enriched = properties with { ExperimentVariant = flags.Variant(PopLiveAnalyticsContract.ExperimentId, actorKey) };
            PopLiveAnalyticsContract.Validate(eventName, enriched);
            using var transaction = connection.BeginTransaction();
            await outbox.EnqueueAsync(connection, transaction, new AnalyticsEvent(Guid.NewGuid(), eventName, actorKey,
                PopLiveAnalyticsContract.Serialize(enriched), DateTime.UtcNow, semanticKey, PopLiveAnalyticsContract.SchemaVersion));
            transaction.Commit();
        }
        catch (Exception exception) { logger.LogWarning(exception, "PoP Live analytics event {EventName} was dropped", eventName); }
    }
}
