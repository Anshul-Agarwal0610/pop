IF COL_LENGTH('TrendingTopics','GenerationStatus') IS NULL
BEGIN
  ALTER TABLE TrendingTopics ADD GenerationStatus varchar(20) NOT NULL CONSTRAINT DF_TrendingTopics_GenerationStatus DEFAULT 'Pending',
    AttemptCount int NOT NULL CONSTRAINT DF_TrendingTopics_AttemptCount DEFAULT 0, NextAttemptAtUtc datetime2 NULL,
    LastFailureClass varchar(40) NULL, LastFailureProvider varchar(80) NULL, LastFailureAtUtc datetime2 NULL,
    LastFailureDetail nvarchar(500) NULL, LeaseId uniqueidentifier NULL, LeaseExpiresAtUtc datetime2 NULL,
    TerminalDecision nvarchar(500) NULL;
END;

IF COL_LENGTH('Polls','SourceTopicId') IS NULL ALTER TABLE Polls ADD SourceTopicId bigint NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Polls_SourceTopicId')
  CREATE UNIQUE INDEX UX_Polls_SourceTopicId ON Polls(SourceTopicId) WHERE SourceTopicId IS NOT NULL;

IF OBJECT_ID('LlmProviderHealth','U') IS NULL
CREATE TABLE LlmProviderHealth(
  ProviderName varchar(80) NOT NULL PRIMARY KEY, CircuitState varchar(20) NOT NULL DEFAULT 'Closed',
  ConsecutiveFailures int NOT NULL DEFAULT 0, CooldownUntilUtc datetime2 NULL, ProbeLeaseId uniqueidentifier NULL,
  ProbeLeaseExpiresAtUtc datetime2 NULL, LastSuccessAtUtc datetime2 NULL, LastFailureAtUtc datetime2 NULL);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TrendingTopics_GenerationDue')
  CREATE INDEX IX_TrendingTopics_GenerationDue ON TrendingTopics(GenerationStatus,NextAttemptAtUtc,LeaseExpiresAtUtc) INCLUDE(IsProcessed,FetchedAt);
