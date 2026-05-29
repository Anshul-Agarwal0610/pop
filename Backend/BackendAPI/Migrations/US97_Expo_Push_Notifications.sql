IF OBJECT_ID('dbo.MobileDeviceTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MobileDeviceTokens (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Token NVARCHAR(256) NOT NULL,
        Platform NVARCHAR(32) NOT NULL CONSTRAINT DF_MobileDeviceTokens_Platform DEFAULT 'android',
        DeviceId NVARCHAR(128) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_MobileDeviceTokens_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MobileDeviceTokens_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_MobileDeviceTokens_UpdatedAt DEFAULT GETUTCDATE(),
        LastSeenAt DATETIME2 NOT NULL CONSTRAINT DF_MobileDeviceTokens_LastSeenAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_MobileDeviceTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_MobileDeviceTokens_Token UNIQUE (Token)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_MobileDeviceTokens_UserActive'
      AND object_id = OBJECT_ID('dbo.MobileDeviceTokens')
)
BEGIN
    CREATE INDEX IX_MobileDeviceTokens_UserActive
    ON dbo.MobileDeviceTokens (UserId, IsActive, LastSeenAt DESC);
END;

IF OBJECT_ID('dbo.NotificationPushDeliveries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationPushDeliveries (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        NotificationId BIGINT NOT NULL,
        DeviceTokenId BIGINT NOT NULL,
        Status NVARCHAR(32) NOT NULL,
        ProviderMessageId NVARCHAR(256) NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        AttemptedAt DATETIME2 NOT NULL CONSTRAINT DF_NotificationPushDeliveries_AttemptedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_NotificationPushDeliveries_Notifications FOREIGN KEY (NotificationId) REFERENCES dbo.Notifications(Id),
        CONSTRAINT FK_NotificationPushDeliveries_MobileDeviceTokens FOREIGN KEY (DeviceTokenId) REFERENCES dbo.MobileDeviceTokens(Id),
        CONSTRAINT UQ_NotificationPushDeliveries_NotificationDevice UNIQUE (NotificationId, DeviceTokenId)
    );
END;
