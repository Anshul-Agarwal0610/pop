using Dapper; using System.Data;
namespace BackendAPI.Analytics;
public sealed class AnalyticsOutbox : IAnalyticsOutbox
{
    public Task EnqueueAsync(IDbConnection connection, IDbTransaction transaction, AnalyticsEvent e) => connection.ExecuteAsync(@"IF NOT EXISTS (SELECT 1 FROM AnalyticsEventOutbox WHERE SemanticKey=@SemanticKey) INSERT INTO AnalyticsEventOutbox(EventId,EventName,SchemaVersion,ActorKey,PayloadJson,OccurredAt,SemanticKey,ExpiresAt) VALUES(@EventId,@Name,@SchemaVersion,@ActorKey,@PayloadJson,@OccurredAt,@SemanticKey,DATEADD(day,30,@OccurredAt))", e, transaction);
}
