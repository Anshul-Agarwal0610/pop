IF COL_LENGTH('dbo.Polls', 'IsPrivate') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD IsPrivate BIT NOT NULL CONSTRAINT DF_Polls_IsPrivate DEFAULT 0;
END;

IF COL_LENGTH('dbo.Polls', 'IsWellness') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD IsWellness BIT NOT NULL CONSTRAINT DF_Polls_IsWellness DEFAULT 0;
END;

IF COL_LENGTH('dbo.Polls', 'PollMode') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD PollMode NVARCHAR(40) NOT NULL CONSTRAINT DF_Polls_PollMode DEFAULT 'Public';
END;
GO

UPDATE dbo.Polls
SET IsPrivate = 1,
    IsWellness = 1,
    PollMode = 'Wellness',
    ModerationStatus = 'Published'
WHERE Category = 'Health';

IF OBJECT_ID('dbo.WellnessResponses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WellnessResponses (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        PollId BIGINT NOT NULL,
        OptionId BIGINT NOT NULL,
        Note NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_WellnessResponses_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_WellnessResponses_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_WellnessResponses_Polls FOREIGN KEY (PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_WellnessResponses_PollOptions FOREIGN KEY (OptionId) REFERENCES dbo.PollOptions(Id)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_WellnessResponses_User_CreatedAt'
      AND object_id = OBJECT_ID('dbo.WellnessResponses')
)
BEGIN
    CREATE INDEX IX_WellnessResponses_User_CreatedAt
    ON dbo.WellnessResponses (UserId, CreatedAt DESC);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Polls_Wellness_Private'
      AND object_id = OBJECT_ID('dbo.Polls')
)
BEGIN
    CREATE INDEX IX_Polls_Wellness_Private
    ON dbo.Polls (IsWellness, IsPrivate, IsActive, ExpiresAt);
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Polls
    WHERE PollMode = 'Wellness'
      AND Question = 'How are you feeling today?'
)
BEGIN
    DECLARE @MoodPollId BIGINT;

    INSERT INTO dbo.Polls
        (Question, Description, Category, ExpiresAt, IsActive, IsTrending, CreatedByUserId,
         CreatedAt, TotalVotes, SourceType, SourceUrl, ThumbnailUrl, IsAIGenerated,
         IsPrivate, IsWellness, PollMode, ModerationStatus, ReportCount)
    VALUES
        ('How are you feeling today?', 'Private daily wellness check-in.', 'Health',
         DATEADD(year, 5, GETUTCDATE()), 1, 0, NULL,
         GETUTCDATE(), 0, 'manual', NULL, NULL, 0,
         1, 1, 'Wellness', 'Published', 0);

    SET @MoodPollId = CAST(SCOPE_IDENTITY() AS BIGINT);

    INSERT INTO dbo.PollOptions (PollId, Text, VoteCount, VotePercentage)
    VALUES
        (@MoodPollId, 'Great', 0, 0),
        (@MoodPollId, 'Okay', 0, 0),
        (@MoodPollId, 'Stressed', 0, 0),
        (@MoodPollId, 'Low', 0, 0);
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Polls
    WHERE PollMode = 'Wellness'
      AND Question = 'What does your body need right now?'
)
BEGIN
    DECLARE @BodyPollId BIGINT;

    INSERT INTO dbo.Polls
        (Question, Description, Category, ExpiresAt, IsActive, IsTrending, CreatedByUserId,
         CreatedAt, TotalVotes, SourceType, SourceUrl, ThumbnailUrl, IsAIGenerated,
         IsPrivate, IsWellness, PollMode, ModerationStatus, ReportCount)
    VALUES
        ('What does your body need right now?', 'Private wellness reflection.', 'Health',
         DATEADD(year, 5, GETUTCDATE()), 1, 0, NULL,
         GETUTCDATE(), 0, 'manual', NULL, NULL, 0,
         1, 1, 'Wellness', 'Published', 0);

    SET @BodyPollId = CAST(SCOPE_IDENTITY() AS BIGINT);

    INSERT INTO dbo.PollOptions (PollId, Text, VoteCount, VotePercentage)
    VALUES
        (@BodyPollId, 'Rest', 0, 0),
        (@BodyPollId, 'Water', 0, 0),
        (@BodyPollId, 'Movement', 0, 0),
        (@BodyPollId, 'A calmer moment', 0, 0);
END;
