IF COL_LENGTH('TrendingTopics', 'Publisher') IS NULL
    ALTER TABLE TrendingTopics ADD Publisher NVARCHAR(200) NULL;
GO
IF COL_LENGTH('TrendingTopics', 'PublishedAt') IS NULL
    ALTER TABLE TrendingTopics ADD PublishedAt DATETIME2 NULL;
GO
