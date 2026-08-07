-- US145: safe deterministic fallback, generation provenance, and retry workflow.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF COL_LENGTH('Polls', 'GenerationMethod') IS NULL
    ALTER TABLE Polls ADD GenerationMethod varchar(32) NOT NULL CONSTRAINT DF_Polls_GenerationMethod DEFAULT 'ManualReview';
IF COL_LENGTH('Polls', 'TrendingTopicId') IS NULL ALTER TABLE Polls ADD TrendingTopicId bigint NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_Polls_GenerationMethod')
    ALTER TABLE Polls ADD CONSTRAINT CK_Polls_GenerationMethod CHECK (GenerationMethod IN ('Llm','DeterministicFallback','ManualReview'));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_Polls_TrendingTopicId' AND object_id=OBJECT_ID('Polls'))
    CREATE UNIQUE INDEX UX_Polls_TrendingTopicId ON Polls(TrendingTopicId) WHERE TrendingTopicId IS NOT NULL;

IF COL_LENGTH('TrendingTopics', 'ConversionStatus') IS NULL ALTER TABLE TrendingTopics ADD ConversionStatus varchar(24) NOT NULL CONSTRAINT DF_TrendingTopics_ConversionStatus DEFAULT 'Pending';
IF COL_LENGTH('TrendingTopics', 'AttemptCount') IS NULL ALTER TABLE TrendingTopics ADD AttemptCount int NOT NULL CONSTRAINT DF_TrendingTopics_AttemptCount DEFAULT 0;
IF COL_LENGTH('TrendingTopics', 'NextAttemptAt') IS NULL ALTER TABLE TrendingTopics ADD NextAttemptAt datetime2 NULL;
IF COL_LENGTH('TrendingTopics', 'LastAttemptAt') IS NULL ALTER TABLE TrendingTopics ADD LastAttemptAt datetime2 NULL;
IF COL_LENGTH('TrendingTopics', 'LastFailureKind') IS NULL ALTER TABLE TrendingTopics ADD LastFailureKind varchar(64) NULL;
IF COL_LENGTH('TrendingTopics', 'LastFailureReason') IS NULL ALTER TABLE TrendingTopics ADD LastFailureReason nvarchar(1000) NULL;
IF COL_LENGTH('TrendingTopics', 'LastGenerationMethod') IS NULL ALTER TABLE TrendingTopics ADD LastGenerationMethod varchar(32) NULL;
IF COL_LENGTH('TrendingTopics', 'GeneratedPollId') IS NULL ALTER TABLE TrendingTopics ADD GeneratedPollId bigint NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_TrendingTopics_ConversionStatus')
    ALTER TABLE TrendingTopics ADD CONSTRAINT CK_TrendingTopics_ConversionStatus CHECK (ConversionStatus IN ('Pending','RetryPending','Converted','NeedsReview','Unconvertible'));
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TrendingTopics_ConversionEligibility' AND object_id=OBJECT_ID('TrendingTopics'))
    CREATE INDEX IX_TrendingTopics_ConversionEligibility ON TrendingTopics(ConversionStatus,NextAttemptAt,AttemptCount) INCLUDE(FetchedAt);
