-- US149: durable source-to-poll lifecycle and shared generation controls (idempotent)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF COL_LENGTH('dbo.TrendingTopics','ProcessingStatus') IS NULL ALTER TABLE dbo.TrendingTopics ADD ProcessingStatus varchar(20) NOT NULL CONSTRAINT DF_TrendingTopics_ProcessingStatus DEFAULT 'Queued';
IF COL_LENGTH('dbo.TrendingTopics','AttemptCount') IS NULL ALTER TABLE dbo.TrendingTopics ADD AttemptCount int NOT NULL CONSTRAINT DF_TrendingTopics_AttemptCount DEFAULT 0;
IF COL_LENGTH('dbo.TrendingTopics','NextAttemptAt') IS NULL ALTER TABLE dbo.TrendingTopics ADD NextAttemptAt datetime2 NULL;
IF COL_LENGTH('dbo.TrendingTopics','LastAttemptAt') IS NULL ALTER TABLE dbo.TrendingTopics ADD LastAttemptAt datetime2 NULL;
IF COL_LENGTH('dbo.TrendingTopics','LastFailureCode') IS NULL ALTER TABLE dbo.TrendingTopics ADD LastFailureCode varchar(64) NULL;
IF COL_LENGTH('dbo.TrendingTopics','CorrelationId') IS NULL ALTER TABLE dbo.TrendingTopics ADD CorrelationId varchar(64) NULL;
IF COL_LENGTH('dbo.TrendingTopics','GeneratedPollId') IS NULL ALTER TABLE dbo.TrendingTopics ADD GeneratedPollId bigint NULL;
IF COL_LENGTH('dbo.TrendingTopics','LeaseId') IS NULL ALTER TABLE dbo.TrendingTopics ADD LeaseId uniqueidentifier NULL;
IF COL_LENGTH('dbo.TrendingTopics','LeaseExpiresAt') IS NULL ALTER TABLE dbo.TrendingTopics ADD LeaseExpiresAt datetime2 NULL;
GO
UPDATE dbo.TrendingTopics SET ProcessingStatus=CASE WHEN IsProcessed=1 THEN 'Converted' ELSE 'Queued' END WHERE ProcessingStatus='Queued';
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_TrendingTopics_ProcessingStatus_NextAttemptAt') CREATE INDEX IX_TrendingTopics_ProcessingStatus_NextAttemptAt ON dbo.TrendingTopics(ProcessingStatus,NextAttemptAt,FetchedAt);
IF OBJECT_ID('dbo.PipelineControl','U') IS NULL BEGIN CREATE TABLE dbo.PipelineControl(Id tinyint NOT NULL PRIMARY KEY CHECK(Id=1),GenerationPaused bit NOT NULL,UpdatedAt datetime2 NOT NULL,UpdatedBy nvarchar(128) NULL); INSERT dbo.PipelineControl VALUES(1,0,GETUTCDATE(),NULL); END;
GO
