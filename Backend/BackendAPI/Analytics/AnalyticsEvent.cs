namespace BackendAPI.Analytics;
public sealed record AnalyticsEvent(Guid EventId, string Name, string ActorKey, string PayloadJson, DateTime OccurredAt, string SemanticKey, int SchemaVersion = 1);
