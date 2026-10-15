-- Apply after US151, before deploying expanded ingestion.
SET XACT_ABORT ON;
GO
IF COL_LENGTH('TrendingTopics','SourceItemId') IS NULL ALTER TABLE TrendingTopics ADD SourceItemId nvarchar(256) NULL;
IF COL_LENGTH('TrendingTopics','SourceTopic') IS NULL ALTER TABLE TrendingTopics ADD SourceTopic nvarchar(512) NULL;
IF COL_LENGTH('TrendingTopics','Language') IS NULL ALTER TABLE TrendingTopics ADD Language nvarchar(32) NULL;
IF COL_LENGTH('TrendingTopics','Region') IS NULL ALTER TABLE TrendingTopics ADD Region nvarchar(128) NULL;
IF COL_LENGTH('TrendingTopics','EventStartUtc') IS NULL ALTER TABLE TrendingTopics ADD EventStartUtc datetime2 NULL;
IF COL_LENGTH('TopicSourceItems','SourceItemId') IS NULL ALTER TABLE TopicSourceItems ADD SourceItemId nvarchar(256) NULL;
IF COL_LENGTH('TopicSourceItems','SourceTopic') IS NULL ALTER TABLE TopicSourceItems ADD SourceTopic nvarchar(512) NULL;
IF COL_LENGTH('TopicSourceItems','Language') IS NULL ALTER TABLE TopicSourceItems ADD Language nvarchar(32) NULL;
IF COL_LENGTH('TopicSourceItems','Region') IS NULL ALTER TABLE TopicSourceItems ADD Region nvarchar(128) NULL;
IF COL_LENGTH('TopicSourceItems','EventStartUtc') IS NULL ALTER TABLE TopicSourceItems ADD EventStartUtc datetime2 NULL;
GO
