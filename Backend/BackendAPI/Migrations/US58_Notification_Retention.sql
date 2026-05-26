IF COL_LENGTH('dbo.Notifications', 'DedupKey') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD DedupKey NVARCHAR(160) NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Notifications_User_DedupKey'
      AND object_id = OBJECT_ID('dbo.Notifications')
)
BEGIN
    CREATE UNIQUE INDEX IX_Notifications_User_DedupKey
    ON dbo.Notifications (UserId, DedupKey)
    WHERE DedupKey IS NOT NULL;
END;

IF OBJECT_ID('dbo.NotificationPreferences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationPreferences (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Type NVARCHAR(40) NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_NotificationPreferences_IsEnabled DEFAULT 1,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_NotificationPreferences_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_NotificationPreferences_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_NotificationPreferences_UserType UNIQUE (UserId, Type)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_NotificationPreferences_User'
      AND object_id = OBJECT_ID('dbo.NotificationPreferences')
)
BEGIN
    CREATE INDEX IX_NotificationPreferences_User
    ON dbo.NotificationPreferences (UserId, Type, IsEnabled);
END;
