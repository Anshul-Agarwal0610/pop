ALTER TABLE Users ADD AnalyticsConsent varchar(10) NOT NULL CONSTRAINT DF_Users_AnalyticsConsent DEFAULT 'unknown', AnalyticsConsentUpdatedAt datetime2 NULL;
GO
CREATE TABLE AnalyticsEventOutbox (
  EventId uniqueidentifier NOT NULL PRIMARY KEY, EventName varchar(80) NOT NULL, SchemaVersion int NOT NULL,
  ActorKey varchar(80) NOT NULL, PayloadJson nvarchar(max) NOT NULL, OccurredAt datetime2 NOT NULL,
  SemanticKey varchar(180) NOT NULL, AttemptCount int NOT NULL DEFAULT 0, NextAttemptAt datetime2 NULL,
  DeliveredAt datetime2 NULL, ExpiresAt datetime2 NOT NULL,
  CONSTRAINT UQ_AnalyticsEventOutbox_SemanticKey UNIQUE (SemanticKey), CONSTRAINT CK_AnalyticsEventOutbox_PayloadJson CHECK (ISJSON(PayloadJson)=1)
);
GO
