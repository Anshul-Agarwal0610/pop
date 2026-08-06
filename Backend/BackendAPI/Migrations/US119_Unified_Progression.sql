-- US-119: Durable exactly-once XP event ledger.
-- This migration intentionally does not update Users.Xp, preserving every balance.
IF OBJECT_ID('dbo.ProgressionRewardEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProgressionRewardEvents (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId BIGINT NOT NULL,
        EventType NVARCHAR(32) NOT NULL,
        SourceId NVARCHAR(128) NOT NULL,
        AwardedXp INT NOT NULL,
        TotalXp INT NULL,
        Level INT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProgressionRewardEvents_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ProgressionRewardEvents_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ProgressionRewardEvents_AwardedXp CHECK (AwardedXp >= 0),
        CONSTRAINT UQ_ProgressionRewardEvents_Source UNIQUE (UserId, EventType, SourceId)
    );
    CREATE INDEX IX_ProgressionRewardEvents_UserCreated
        ON dbo.ProgressionRewardEvents(UserId, CreatedAt DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Challenges_DailyIdentity')
    CREATE UNIQUE INDEX UQ_Challenges_DailyIdentity
        ON dbo.Challenges(Title, StartAt, EndAt);
GO
