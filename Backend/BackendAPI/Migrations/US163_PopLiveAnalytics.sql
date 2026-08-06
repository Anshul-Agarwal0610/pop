CREATE INDEX IX_AnalyticsEventOutbox_Pending
ON AnalyticsEventOutbox (DeliveredAt, NextAttemptAt, ExpiresAt, OccurredAt)
INCLUDE (EventName, SchemaVersion, ActorKey, SemanticKey);
GO
