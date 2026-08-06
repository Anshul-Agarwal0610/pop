-- US-121: Auditable XP ledger used by weekly and all-time leaderboards.
-- Existing XP cannot be safely classified by source/privacy and is intentionally not backfilled.
IF OBJECT_ID('dbo.XpEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.XpEvents (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_XpEvents PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Amount INT NOT NULL CONSTRAINT CK_XpEvents_PositiveAmount CHECK (Amount > 0),
        SourceType VARCHAR(20) NOT NULL CONSTRAINT CK_XpEvents_SourceType CHECK (SourceType IN ('Vote','Challenge','Achievement')),
        PollId BIGINT NULL,
        ChallengeId BIGINT NULL,
        BadgeId BIGINT NULL,
        OccurredAt DATETIME2(7) NOT NULL,
        IsValid BIT NOT NULL CONSTRAINT DF_XpEvents_IsValid DEFAULT (1),
        IsLeaderboardEligible BIT NOT NULL CONSTRAINT DF_XpEvents_Eligible DEFAULT (1),
        InvalidatedAt DATETIME2(7) NULL,
        InvalidReason NVARCHAR(500) NULL,
        CONSTRAINT FK_XpEvents_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_XpEvents_SourceId CHECK (
            (SourceType = 'Vote' AND PollId IS NOT NULL AND ChallengeId IS NULL AND BadgeId IS NULL) OR
            (SourceType = 'Challenge' AND ChallengeId IS NOT NULL AND PollId IS NULL AND BadgeId IS NULL) OR
            (SourceType = 'Achievement' AND BadgeId IS NOT NULL AND PollId IS NULL AND ChallengeId IS NULL)
        )
    );

    CREATE UNIQUE INDEX UX_XpEvents_Vote ON dbo.XpEvents(UserId, PollId) WHERE SourceType = 'Vote';
    CREATE UNIQUE INDEX UX_XpEvents_Challenge ON dbo.XpEvents(UserId, ChallengeId) WHERE SourceType = 'Challenge';
    CREATE UNIQUE INDEX UX_XpEvents_Achievement ON dbo.XpEvents(UserId, BadgeId) WHERE SourceType = 'Achievement';
    CREATE INDEX IX_XpEvents_Ranking ON dbo.XpEvents(IsValid, IsLeaderboardEligible, OccurredAt, UserId) INCLUDE (Amount);
    CREATE INDEX IX_XpEvents_UserTime ON dbo.XpEvents(UserId, OccurredAt) INCLUDE (Amount, IsValid, IsLeaderboardEligible);
END;
