IF OBJECT_ID('dbo.UserCategoryPreferences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserCategoryPreferences (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserCategoryPreferences_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_UserCategoryPreferences_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_UserCategoryPreferences_UserCategory UNIQUE (UserId, Category)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UserCategoryPreferences_UserId' AND object_id = OBJECT_ID('dbo.UserCategoryPreferences')
)
BEGIN
    CREATE INDEX IX_UserCategoryPreferences_UserId
    ON dbo.UserCategoryPreferences (UserId, Category);
END;
