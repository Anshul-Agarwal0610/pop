-- Apply after US150. Existing polls and votes are preserved. Phase-one uses
-- TrendingTopicId + PhaseOneAngle; the legacy SourceTopicId index stays intact.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_TrendingTopics_ConversionStatus')
ALTER TABLE TrendingTopics DROP CONSTRAINT CK_TrendingTopics_ConversionStatus;
ALTER TABLE TrendingTopics ADD CONSTRAINT CK_TrendingTopics_ConversionStatus
CHECK(ConversionStatus IN ('Pending','RetryPending','Processing','Converted','NeedsReview','Unconvertible'));
GO
IF COL_LENGTH('TrendingTopics','PhaseOnePayload') IS NULL ALTER TABLE TrendingTopics ADD PhaseOnePayload nvarchar(max) NULL;
IF COL_LENGTH('TrendingTopics','PhaseOneProvider') IS NULL ALTER TABLE TrendingTopics ADD PhaseOneProvider nvarchar(80) NULL;
IF COL_LENGTH('TrendingTopics','PhaseOneModel') IS NULL ALTER TABLE TrendingTopics ADD PhaseOneModel nvarchar(120) NULL;
IF COL_LENGTH('Polls','PhaseOneAngle') IS NULL ALTER TABLE Polls ADD PhaseOneAngle int NULL;
IF COL_LENGTH('Polls','PublishedAtUtc') IS NULL ALTER TABLE Polls ADD PublishedAtUtc datetime2 NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('Polls') AND name='UX_Polls_PhaseOneAngle')
CREATE UNIQUE INDEX UX_Polls_PhaseOneAngle ON Polls(TrendingTopicId,PhaseOneAngle) WHERE PhaseOneAngle IS NOT NULL;
IF OBJECT_ID('PipelineSchedule','U') IS NULL
CREATE TABLE PipelineSchedule(Name varchar(80) NOT NULL PRIMARY KEY, NextRunAtUtc datetime2 NOT NULL);
IF NOT EXISTS(SELECT 1 FROM PipelineSchedule WHERE Name='ingestion')
INSERT PipelineSchedule VALUES('ingestion',SYSUTCDATETIME());
GO
IF OBJECT_ID('TopicSourceItems','U') IS NULL
CREATE TABLE TopicSourceItems(
 SourceKey char(64) NOT NULL PRIMARY KEY,
 TopicId bigint NOT NULL REFERENCES TrendingTopics(Id),
 SourceType nvarchar(40) NOT NULL, SourceUrl nvarchar(2048) NOT NULL,
 Title nvarchar(max) NOT NULL, Summary nvarchar(max) NULL, Publisher nvarchar(512) NULL,
 PublishedAt datetime2 NULL, FirstSeenAtUtc datetime2 NOT NULL, LastSeenAtUtc datetime2 NOT NULL
);
GO
