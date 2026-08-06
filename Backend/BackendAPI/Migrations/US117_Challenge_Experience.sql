/* US-117: additive, idempotent challenge definitions and occurrence metadata. */
IF OBJECT_ID('dbo.ChallengeDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChallengeDefinitions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        StableKey NVARCHAR(100) NOT NULL CONSTRAINT UQ_ChallengeDefinitions_StableKey UNIQUE,
        Title NVARCHAR(160) NOT NULL, Description NVARCHAR(400) NOT NULL,
        ChallengeType NVARCHAR(40) NOT NULL, Recurrence NVARCHAR(20) NOT NULL,
        RequirementType NVARCHAR(40) NOT NULL, RequirementText NVARCHAR(200) NOT NULL,
        TargetCount INT NOT NULL, Category NVARCHAR(100) NULL, RewardXp INT NOT NULL,
        RewardBadge NVARCHAR(120) NULL, RewardBadgeId BIGINT NULL,
        AllowPrivateVotes BIT NOT NULL CONSTRAINT DF_ChallengeDefinitions_Private DEFAULT 0,
        AllowWellnessVotes BIT NOT NULL CONSTRAINT DF_ChallengeDefinitions_Wellness DEFAULT 0,
        IsEnabled BIT NOT NULL CONSTRAINT DF_ChallengeDefinitions_Enabled DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ChallengeDefinitions_Created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_ChallengeDefinitions_Target CHECK (TargetCount > 0),
        CONSTRAINT CK_ChallengeDefinitions_Recurrence CHECK (Recurrence IN ('Daily','Weekly','None'))
    );
END;

IF COL_LENGTH('dbo.Challenges', 'DefinitionId') IS NULL ALTER TABLE dbo.Challenges ADD DefinitionId BIGINT NULL;
IF COL_LENGTH('dbo.Challenges', 'Description') IS NULL ALTER TABLE dbo.Challenges ADD Description NVARCHAR(400) NOT NULL CONSTRAINT DF_Challenges_Description DEFAULT '';
IF COL_LENGTH('dbo.Challenges', 'ChallengeType') IS NULL ALTER TABLE dbo.Challenges ADD ChallengeType NVARCHAR(40) NOT NULL CONSTRAINT DF_Challenges_Type DEFAULT 'Voting';
IF COL_LENGTH('dbo.Challenges', 'Recurrence') IS NULL ALTER TABLE dbo.Challenges ADD Recurrence NVARCHAR(20) NOT NULL CONSTRAINT DF_Challenges_Recurrence DEFAULT 'Daily';
IF COL_LENGTH('dbo.Challenges', 'RequirementType') IS NULL ALTER TABLE dbo.Challenges ADD RequirementType NVARCHAR(40) NOT NULL CONSTRAINT DF_Challenges_RequirementType DEFAULT 'VoteCount';
IF COL_LENGTH('dbo.Challenges', 'RequirementText') IS NULL ALTER TABLE dbo.Challenges ADD RequirementText NVARCHAR(200) NOT NULL CONSTRAINT DF_Challenges_RequirementText DEFAULT 'Cast votes';
IF COL_LENGTH('dbo.Challenges', 'RewardBadgeId') IS NULL ALTER TABLE dbo.Challenges ADD RewardBadgeId BIGINT NULL;
IF COL_LENGTH('dbo.Challenges', 'AllowPrivateVotes') IS NULL ALTER TABLE dbo.Challenges ADD AllowPrivateVotes BIT NOT NULL CONSTRAINT DF_Challenges_Private DEFAULT 0;
IF COL_LENGTH('dbo.Challenges', 'AllowWellnessVotes') IS NULL ALTER TABLE dbo.Challenges ADD AllowWellnessVotes BIT NOT NULL CONSTRAINT DF_Challenges_Wellness DEFAULT 0;

IF OBJECT_ID('dbo.ChallengeProgressEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChallengeProgressEvents (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY, UserId BIGINT NOT NULL,
        ChallengeId BIGINT NOT NULL, VoteId BIGINT NOT NULL, CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_ChallengeProgressEvents UNIQUE(UserId, ChallengeId, VoteId),
        CONSTRAINT FK_ChallengeProgressEvents_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_ChallengeProgressEvents_Challenge FOREIGN KEY(ChallengeId) REFERENCES dbo.Challenges(Id),
        CONSTRAINT FK_ChallengeProgressEvents_Vote FOREIGN KEY(VoteId) REFERENCES dbo.Votes(Id)
    );
END;

IF OBJECT_ID('dbo.AchievementBadges', 'U') IS NOT NULL
BEGIN
    MERGE dbo.AchievementBadges AS target
    USING (VALUES
      ('daily_voter', 'Daily Voter', 'Complete a daily voting challenge.', 'Sun', 'ChallengeReward', 1, 0),
      ('weekly_tech_voice', 'Tech Voice', 'Complete the weekly technology challenge.', 'Cpu', 'ChallengeReward', 1, 0)
    ) source(Code,Name,Description,Icon,RuleType,Threshold,RewardXp) ON target.Code=source.Code
    WHEN NOT MATCHED THEN INSERT(Code,Name,Description,Icon,RuleType,Threshold,RewardXp)
      VALUES(source.Code,source.Name,source.Description,source.Icon,source.RuleType,source.Threshold,source.RewardXp);
END;

MERGE dbo.ChallengeDefinitions AS target
USING (VALUES
 ('daily-pulse','Daily Pulse','Share your opinion on three public polls.','Voting','Daily','VoteCount','Cast 3 votes',3,NULL,75,'Daily Voter','daily_voter'),
 ('weekly-tech-voice','Weekly Tech Voice','Make your voice heard in Technology polls this week.','Category','Weekly','VoteCount','Cast 7 Technology votes',7,'Technology',200,'Tech Voice','weekly_tech_voice')
) source(StableKey,Title,Description,ChallengeType,Recurrence,RequirementType,RequirementText,TargetCount,Category,RewardXp,RewardBadge,BadgeCode)
ON target.StableKey=source.StableKey
WHEN MATCHED THEN UPDATE SET Title=source.Title,Description=source.Description,ChallengeType=source.ChallengeType,
 Recurrence=source.Recurrence,RequirementType=source.RequirementType,RequirementText=source.RequirementText,
 TargetCount=source.TargetCount,Category=source.Category,RewardXp=source.RewardXp,RewardBadge=source.RewardBadge
WHEN NOT MATCHED THEN INSERT(StableKey,Title,Description,ChallengeType,Recurrence,RequirementType,RequirementText,TargetCount,Category,RewardXp,RewardBadge,RewardBadgeId)
 VALUES(source.StableKey,source.Title,source.Description,source.ChallengeType,source.Recurrence,source.RequirementType,source.RequirementText,source.TargetCount,source.Category,source.RewardXp,source.RewardBadge,
   (SELECT Id FROM dbo.AchievementBadges WHERE Code=source.BadgeCode));

UPDATE d SET RewardBadgeId=b.Id FROM dbo.ChallengeDefinitions d JOIN dbo.AchievementBadges b
 ON b.Code=CASE d.StableKey WHEN 'daily-pulse' THEN 'daily_voter' WHEN 'weekly-tech-voice' THEN 'weekly_tech_voice' END
WHERE d.RewardBadgeId IS NULL;

/* Adopt legacy Daily Pulse occurrences instead of replacing user progress. */
;WITH legacy AS (
 SELECT c.Id, ROW_NUMBER() OVER(PARTITION BY c.StartAt,c.EndAt ORDER BY c.Id) AS occurrence_rank
 FROM dbo.Challenges c WHERE c.DefinitionId IS NULL AND c.Title='Daily Pulse'
)
UPDATE c SET DefinitionId=d.Id, Description=d.Description, ChallengeType=d.ChallengeType,
 Recurrence=d.Recurrence, RequirementType=d.RequirementType, RequirementText=d.RequirementText,
 RewardBadgeId=d.RewardBadgeId, AllowPrivateVotes=d.AllowPrivateVotes, AllowWellnessVotes=d.AllowWellnessVotes
FROM dbo.Challenges c JOIN legacy l ON l.Id=c.Id CROSS JOIN dbo.ChallengeDefinitions d
WHERE l.occurrence_rank=1 AND d.StableKey='daily-pulse';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_Challenges_DefinitionWindow' AND object_id=OBJECT_ID('dbo.Challenges'))
    CREATE UNIQUE INDEX UQ_Challenges_DefinitionWindow ON dbo.Challenges(DefinitionId, StartAt, EndAt) WHERE DefinitionId IS NOT NULL;
