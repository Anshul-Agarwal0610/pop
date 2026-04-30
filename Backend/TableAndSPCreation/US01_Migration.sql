-- ============================================================
-- US-01 Migration: Ingestion Pipeline DB Changes
-- Run once against PollifyDB
-- ============================================================

USE PollifyDB;
GO

-- ============================================================
-- 1. Add source / AI columns to Polls
-- ============================================================
ALTER TABLE Polls
    ADD SourceType      NVARCHAR(50)   NULL,   -- 'rss' | 'youtube' | 'gnews' | 'manual'
        SourceUrl       NVARCHAR(1000) NULL,   -- original article / video URL
        ThumbnailUrl    NVARCHAR(1000) NULL,   -- image to display on poll card
        IsAIGenerated   BIT            NOT NULL DEFAULT 0;
GO

-- ============================================================
-- 2. TrendingTopics staging table
--    Ingestion jobs write here; poll-generation job reads here
-- ============================================================
CREATE TABLE TrendingTopics (
    Id           BIGINT IDENTITY(1,1) PRIMARY KEY,
    Title        NVARCHAR(500)   NOT NULL,
    Summary      NVARCHAR(2000)  NOT NULL DEFAULT '',
    SourceType   NVARCHAR(50)    NOT NULL,   -- 'rss' | 'youtube' | 'gnews'
    SourceUrl    NVARCHAR(1000)  NOT NULL DEFAULT '',
    ThumbnailUrl NVARCHAR(1000)  NULL,
    Category     NVARCHAR(100)   NOT NULL DEFAULT 'General',
    FetchedAt    DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    IsProcessed  BIT             NOT NULL DEFAULT 0,  -- set to 1 after poll is generated
    ProcessedAt  DATETIME2       NULL
);
GO

-- Index: fast lookup of unprocessed topics
CREATE INDEX IX_TrendingTopics_IsProcessed_FetchedAt
    ON TrendingTopics(IsProcessed, FetchedAt DESC);
GO

PRINT 'US-01 migration complete.';
GO
