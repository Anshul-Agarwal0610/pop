IF OBJECT_ID('dbo.Categories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        Id INT NOT NULL PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL UNIQUE,
        Slug NVARCHAR(80) NOT NULL UNIQUE,
        Icon NVARCHAR(80) NOT NULL,
        Color NVARCHAR(40) NOT NULL,
        SortOrder INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1
    );
END;

MERGE dbo.Categories AS target
USING (VALUES
    (1, 'General', 'general', 'sparkles', 'slate', 10, 1),
    (2, 'Technology', 'technology', 'cpu', 'blue', 20, 1),
    (3, 'Society', 'society', 'users', 'rose', 30, 1),
    (4, 'Work', 'work', 'briefcase', 'amber', 40, 1),
    (5, 'Environment', 'environment', 'leaf', 'emerald', 50, 1),
    (6, 'Culture', 'culture', 'palette', 'violet', 60, 1),
    (7, 'Sports', 'sports', 'trophy', 'orange', 70, 1),
    (8, 'Health', 'health', 'heart-pulse', 'teal', 80, 1),
    (9, 'Politics', 'politics', 'landmark', 'indigo', 90, 1)
) AS source (Id, Name, Slug, Icon, Color, SortOrder, IsActive)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Slug = source.Slug,
        Icon = source.Icon,
        Color = source.Color,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Slug, Icon, Color, SortOrder, IsActive)
    VALUES (source.Id, source.Name, source.Slug, source.Icon, source.Color, source.SortOrder, source.IsActive);

UPDATE dbo.Polls
SET Category = CASE
    WHEN Category IS NULL OR LTRIM(RTRIM(Category)) = '' THEN 'General'
    WHEN LOWER(LTRIM(RTRIM(Category))) = 'tech' THEN 'Technology'
    WHEN LOWER(LTRIM(RTRIM(Category))) IN ('business', 'career', 'jobs') THEN 'Work'
    WHEN LOWER(LTRIM(RTRIM(Category))) = 'climate' THEN 'Environment'
    WHEN LOWER(LTRIM(RTRIM(Category))) IN ('entertainment', 'arts', 'movies') THEN 'Culture'
    WHEN LOWER(LTRIM(RTRIM(Category))) IN ('wellness', 'medical', 'fitness') THEN 'Health'
    WHEN LOWER(LTRIM(RTRIM(Category))) IN ('news', 'government') THEN 'Politics'
    WHEN EXISTS (SELECT 1 FROM dbo.Categories c WHERE LOWER(c.Name) = LOWER(LTRIM(RTRIM(dbo.Polls.Category)))) THEN (
        SELECT TOP 1 c.Name FROM dbo.Categories c WHERE LOWER(c.Name) = LOWER(LTRIM(RTRIM(dbo.Polls.Category)))
    )
    ELSE 'General'
END;

IF OBJECT_ID('dbo.TrendingTopics', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.TrendingTopics
    SET Category = CASE
        WHEN Category IS NULL OR LTRIM(RTRIM(Category)) = '' THEN 'General'
        WHEN LOWER(LTRIM(RTRIM(Category))) = 'tech' THEN 'Technology'
        WHEN LOWER(LTRIM(RTRIM(Category))) IN ('business', 'career', 'jobs') THEN 'Work'
        WHEN LOWER(LTRIM(RTRIM(Category))) = 'climate' THEN 'Environment'
        WHEN LOWER(LTRIM(RTRIM(Category))) IN ('entertainment', 'arts', 'movies') THEN 'Culture'
        WHEN LOWER(LTRIM(RTRIM(Category))) IN ('wellness', 'medical', 'fitness') THEN 'Health'
        WHEN LOWER(LTRIM(RTRIM(Category))) IN ('news', 'government') THEN 'Politics'
        WHEN EXISTS (SELECT 1 FROM dbo.Categories c WHERE LOWER(c.Name) = LOWER(LTRIM(RTRIM(dbo.TrendingTopics.Category)))) THEN (
            SELECT TOP 1 c.Name FROM dbo.Categories c WHERE LOWER(c.Name) = LOWER(LTRIM(RTRIM(dbo.TrendingTopics.Category)))
        )
        ELSE 'General'
    END;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Polls_Category' AND object_id = OBJECT_ID('dbo.Polls')
)
BEGIN
    CREATE INDEX IX_Polls_Category ON dbo.Polls (Category);
END;
