-- US-28: Notifications schema
-- Adds user notifications for vote milestones, level-ups, trending alerts,
-- and daily reminders.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications'
)
BEGIN
    CREATE TABLE Notifications (
        Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId    BIGINT         NOT NULL,
        Type      NVARCHAR(40)   NOT NULL,
        Title     NVARCHAR(160)  NOT NULL,
        Body      NVARCHAR(500)  NOT NULL,
        PollId    BIGINT         NULL,
        IsRead    BIT            NOT NULL DEFAULT 0,
        CreatedAt DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Notifications_Users
            FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_Notifications_Polls
            FOREIGN KEY (PollId) REFERENCES Polls(Id)
    );
    PRINT 'Notifications table created.';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists - skipped.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Notifications_User_CreatedAt'
      AND object_id = OBJECT_ID('Notifications')
)
BEGIN
    CREATE INDEX IX_Notifications_User_CreatedAt
        ON Notifications(UserId, CreatedAt DESC);
    PRINT 'IX_Notifications_User_CreatedAt index created.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Notifications_User_IsRead'
      AND object_id = OBJECT_ID('Notifications')
)
BEGIN
    CREATE INDEX IX_Notifications_User_IsRead
        ON Notifications(UserId, IsRead);
    PRINT 'IX_Notifications_User_IsRead index created.';
END
