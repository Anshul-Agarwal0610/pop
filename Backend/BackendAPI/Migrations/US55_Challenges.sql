IF OBJECT_ID('dbo.Challenges', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Challenges (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(160) NOT NULL,
        Category NVARCHAR(100) NULL,
        RequiredVotes INT NOT NULL,
        RewardXp INT NOT NULL,
        RewardBadge NVARCHAR(120) NULL,
        StartAt DATETIME2 NOT NULL,
        EndAt DATETIME2 NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Challenges_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Challenges_CreatedAt DEFAULT GETUTCDATE()
    );
END;

IF OBJECT_ID('dbo.UserChallengeProgress', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserChallengeProgress (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        ChallengeId BIGINT NOT NULL,
        CurrentVotes INT NOT NULL CONSTRAINT DF_UserChallengeProgress_CurrentVotes DEFAULT 0,
        IsCompleted BIT NOT NULL CONSTRAINT DF_UserChallengeProgress_IsCompleted DEFAULT 0,
        RewardGranted BIT NOT NULL CONSTRAINT DF_UserChallengeProgress_RewardGranted DEFAULT 0,
        CompletedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserChallengeProgress_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserChallengeProgress_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_UserChallengeProgress_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_UserChallengeProgress_Challenges FOREIGN KEY (ChallengeId) REFERENCES dbo.Challenges(Id),
        CONSTRAINT UQ_UserChallengeProgress_UserChallenge UNIQUE (UserId, ChallengeId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Challenges_ActiveWindow' AND object_id = OBJECT_ID('dbo.Challenges')
)
BEGIN
    CREATE INDEX IX_Challenges_ActiveWindow
    ON dbo.Challenges (IsActive, StartAt, EndAt, Category);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UserChallengeProgress_User' AND object_id = OBJECT_ID('dbo.UserChallengeProgress')
)
BEGIN
    CREATE INDEX IX_UserChallengeProgress_User
    ON dbo.UserChallengeProgress (UserId, IsCompleted, UpdatedAt DESC);
END;

DECLARE @Today DATETIME2 = CONVERT(date, GETUTCDATE());
DECLARE @Tomorrow DATETIME2 = DATEADD(day, 1, @Today);

IF NOT EXISTS (
    SELECT 1 FROM dbo.Challenges
    WHERE Title = 'Daily Pulse'
      AND StartAt = @Today
      AND EndAt = @Tomorrow
)
BEGIN
    INSERT INTO dbo.Challenges
        (Title, Category, RequiredVotes, RewardXp, RewardBadge, StartAt, EndAt, IsActive, CreatedAt)
    VALUES
        ('Daily Pulse', NULL, 3, 75, 'Daily Voter', @Today, @Tomorrow, 1, GETUTCDATE());
END;
