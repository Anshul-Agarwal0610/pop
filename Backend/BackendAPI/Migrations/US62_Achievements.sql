IF OBJECT_ID('dbo.AchievementBadges', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AchievementBadges (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(80) NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Description NVARCHAR(300) NOT NULL,
        Icon NVARCHAR(40) NOT NULL,
        RuleType NVARCHAR(60) NOT NULL,
        Threshold INT NOT NULL,
        RewardXp INT NOT NULL CONSTRAINT DF_AchievementBadges_RewardXp DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AchievementBadges_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_AchievementBadges_Code UNIQUE (Code)
    );
END;

IF OBJECT_ID('dbo.UserBadges', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserBadges (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        BadgeId BIGINT NOT NULL,
        AwardedAt DATETIME2 NOT NULL CONSTRAINT DF_UserBadges_AwardedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_UserBadges_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_UserBadges_AchievementBadges FOREIGN KEY (BadgeId) REFERENCES dbo.AchievementBadges(Id),
        CONSTRAINT UQ_UserBadges_UserBadge UNIQUE (UserId, BadgeId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UserBadges_User_AwardedAt'
      AND object_id = OBJECT_ID('dbo.UserBadges')
)
BEGIN
    CREATE INDEX IX_UserBadges_User_AwardedAt
    ON dbo.UserBadges (UserId, AwardedAt DESC);
END;

MERGE dbo.AchievementBadges AS target
USING (VALUES
    ('first_vote', 'First Vote', 'Cast your first public vote.', 'Vote', 'VoteCount', 1, 25),
    ('pulse_10', 'Pulse 10', 'Cast 10 public votes.', 'Zap', 'VoteCount', 10, 50),
    ('pulse_50', 'Pulse 50', 'Cast 50 public votes.', 'Trophy', 'VoteCount', 50, 150),
    ('streak_3', 'Three Day Spark', 'Keep a 3 day voting streak.', 'Flame', 'Streak', 3, 50),
    ('streak_7', 'Weekly Fire', 'Keep a 7 day voting streak.', 'Flame', 'Streak', 7, 125),
    ('creator_1', 'Poll Starter', 'Create your first poll.', 'PlusCircle', 'PollCreation', 1, 50),
    ('creator_5', 'Conversation Maker', 'Create 5 polls.', 'MessagesSquare', 'PollCreation', 5, 125),
    ('challenge_1', 'Challenge Finisher', 'Complete your first challenge.', 'Target', 'ChallengeCompletion', 1, 75)
) AS source (Code, Name, Description, Icon, RuleType, Threshold, RewardXp)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description,
        Icon = source.Icon,
        RuleType = source.RuleType,
        Threshold = source.Threshold,
        RewardXp = source.RewardXp
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, Icon, RuleType, Threshold, RewardXp, CreatedAt)
    VALUES (source.Code, source.Name, source.Description, source.Icon, source.RuleType, source.Threshold, source.RewardXp, GETUTCDATE());
