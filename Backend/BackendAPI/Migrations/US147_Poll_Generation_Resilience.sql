SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF COL_LENGTH('TrendingTopics','GenerationStatus') IS NULL ALTER TABLE TrendingTopics ADD GenerationStatus varchar(20) NOT NULL CONSTRAINT DF_TrendingTopics_GenerationStatus DEFAULT 'Pending';
IF COL_LENGTH('TrendingTopics','NextAttemptAtUtc') IS NULL ALTER TABLE TrendingTopics ADD NextAttemptAtUtc datetime2 NULL;
IF COL_LENGTH('TrendingTopics','LastFailureClass') IS NULL ALTER TABLE TrendingTopics ADD LastFailureClass varchar(40) NULL;
IF COL_LENGTH('TrendingTopics','LastFailureProvider') IS NULL ALTER TABLE TrendingTopics ADD LastFailureProvider varchar(80) NULL;
IF COL_LENGTH('TrendingTopics','LastFailureAtUtc') IS NULL ALTER TABLE TrendingTopics ADD LastFailureAtUtc datetime2 NULL;
IF COL_LENGTH('TrendingTopics','LastFailureDetail') IS NULL ALTER TABLE TrendingTopics ADD LastFailureDetail nvarchar(500) NULL;
IF COL_LENGTH('TrendingTopics','LeaseId') IS NULL ALTER TABLE TrendingTopics ADD LeaseId uniqueidentifier NULL;
IF COL_LENGTH('TrendingTopics','LeaseExpiresAtUtc') IS NULL ALTER TABLE TrendingTopics ADD LeaseExpiresAtUtc datetime2 NULL;
IF COL_LENGTH('TrendingTopics','TerminalDecision') IS NULL ALTER TABLE TrendingTopics ADD TerminalDecision nvarchar(500) NULL;
GO

IF COL_LENGTH('Polls','SourceTopicId') IS NULL ALTER TABLE Polls ADD SourceTopicId bigint NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Polls_SourceTopicId')
  CREATE UNIQUE INDEX UX_Polls_SourceTopicId ON Polls(SourceTopicId) WHERE SourceTopicId IS NOT NULL;

IF OBJECT_ID('LlmProviderHealth','U') IS NULL
CREATE TABLE LlmProviderHealth(
  ProviderName varchar(80) NOT NULL PRIMARY KEY, CircuitState varchar(20) NOT NULL DEFAULT 'Closed',
  ConsecutiveFailures int NOT NULL DEFAULT 0, CooldownUntilUtc datetime2 NULL, ProbeLeaseId uniqueidentifier NULL,
  ProbeLeaseExpiresAtUtc datetime2 NULL, LastSuccessAtUtc datetime2 NULL, LastFailureAtUtc datetime2 NULL);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TrendingTopics_GenerationDue')
  CREATE INDEX IX_TrendingTopics_GenerationDue ON TrendingTopics(GenerationStatus,NextAttemptAtUtc,LeaseExpiresAtUtc) INCLUDE(IsProcessed,FetchedAt);
