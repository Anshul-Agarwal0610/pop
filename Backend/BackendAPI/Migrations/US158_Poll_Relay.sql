/* US-158 Poll Relay. Rerunnable SQL Server migration. Raw bearer tokens are never stored. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Polls','PollMode') IS NULL
    ALTER TABLE dbo.Polls ADD PollMode nvarchar(20) NOT NULL CONSTRAINT DF_Polls_PollMode_Relay DEFAULT 'Public';

IF OBJECT_ID('dbo.RelayChains','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayChains(
        Id bigint IDENTITY PRIMARY KEY, PollId bigint NOT NULL, CreatedByUserId bigint NOT NULL,
        Status varchar(16) NOT NULL CONSTRAINT DF_RelayChains_Status DEFAULT 'Active',
        HandoffTtlMinutes int NOT NULL, MaxLength int NOT NULL, CreatedAt datetime2 NOT NULL,
        CompletedAt datetime2 NULL, ExpiredAt datetime2 NULL, FinalizedAt datetime2 NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_RelayChains_Polls FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_RelayChains_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_RelayChains_Status CHECK(Status IN('Active','Completed','Expired','Cancelled')),
        CONSTRAINT CK_RelayChains_Ttl CHECK(HandoffTtlMinutes BETWEEN 5 AND 10080),
        CONSTRAINT CK_RelayChains_Length CHECK(MaxLength BETWEEN 2 AND 100)
    );
END;

IF OBJECT_ID('dbo.RelayHandoffs','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayHandoffs(
        Id bigint IDENTITY PRIMARY KEY, ChainId bigint NOT NULL, Position int NOT NULL,
        SenderUserId bigint NOT NULL, ReceiverUserId bigint NULL, TokenHash varbinary(32) NOT NULL,
        TransferMethod varchar(16) NOT NULL, Status varchar(16) NOT NULL,
        CreatedAt datetime2 NOT NULL, ExpiresAt datetime2 NOT NULL, AcceptedAt datetime2 NULL, CompletedAt datetime2 NULL,
        CONSTRAINT FK_RelayHandoffs_Chain FOREIGN KEY(ChainId) REFERENCES dbo.RelayChains(Id),
        CONSTRAINT FK_RelayHandoffs_Sender FOREIGN KEY(SenderUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_RelayHandoffs_Receiver FOREIGN KEY(ReceiverUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_RelayHandoffs_Position UNIQUE(ChainId,Position),
        CONSTRAINT UQ_RelayHandoffs_Token UNIQUE(TokenHash),
        CONSTRAINT CK_RelayHandoffs_Method CHECK(TransferMethod IN('Link','NativeShare','CopyLink')),
        CONSTRAINT CK_RelayHandoffs_Status CHECK(Status IN('Pending','Accepted','Completed','Expired','Cancelled')),
        CONSTRAINT CK_RelayHandoffs_Users CHECK(ReceiverUserId IS NULL OR ReceiverUserId<>SenderUserId)
    );
    CREATE INDEX IX_RelayHandoffs_Expiry ON dbo.RelayHandoffs(Status,ExpiresAt) INCLUDE(ChainId);
END;

IF OBJECT_ID('dbo.RelayParticipants','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayParticipants(
        ChainId bigint NOT NULL, UserId bigint NOT NULL, Position int NOT NULL, VoteId bigint NULL,
        AcceptedHandoffId bigint NULL, ReceiveFinalOutcome bit NOT NULL CONSTRAINT DF_RelayParticipants_Outcome DEFAULT 0,
        JoinedAt datetime2 NOT NULL, VotedAt datetime2 NULL,
        CONSTRAINT PK_RelayParticipants PRIMARY KEY(ChainId,UserId),
        CONSTRAINT UQ_RelayParticipants_Position UNIQUE(ChainId,Position),
        CONSTRAINT UQ_RelayParticipants_Handoff UNIQUE(AcceptedHandoffId),
        CONSTRAINT FK_RelayParticipants_Chain FOREIGN KEY(ChainId) REFERENCES dbo.RelayChains(Id),
        CONSTRAINT FK_RelayParticipants_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_RelayParticipants_Vote FOREIGN KEY(VoteId) REFERENCES dbo.Votes(Id),
        CONSTRAINT FK_RelayParticipants_Handoff FOREIGN KEY(AcceptedHandoffId) REFERENCES dbo.RelayHandoffs(Id)
    );
END;

IF OBJECT_ID('dbo.RelayMilestones','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayMilestones(Id bigint IDENTITY PRIMARY KEY, Length int NOT NULL UNIQUE, RewardRuleCode varchar(80) NULL, BadgeCode varchar(80) NULL, IsEnabled bit NOT NULL DEFAULT 1);
    INSERT dbo.RelayMilestones(Length,RewardRuleCode,BadgeCode) VALUES(3,'relay.milestone','relay-3'),(5,'relay.milestone','relay-5'),(10,'relay.milestone','relay-10'),(25,'relay.milestone','relay-25');
END;

IF OBJECT_ID('dbo.RelayMilestoneAwards','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayMilestoneAwards(Id bigint IDENTITY PRIMARY KEY,ChainId bigint NOT NULL,MilestoneId bigint NOT NULL,UserId bigint NOT NULL,XpDeliveredAt datetime2 NULL,BadgeDeliveredAt datetime2 NULL,CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),CONSTRAINT UQ_RelayMilestoneAwards UNIQUE(ChainId,MilestoneId,UserId),CONSTRAINT FK_RelayAwards_Chain FOREIGN KEY(ChainId) REFERENCES dbo.RelayChains(Id),CONSTRAINT FK_RelayAwards_Milestone FOREIGN KEY(MilestoneId) REFERENCES dbo.RelayMilestones(Id),CONSTRAINT FK_RelayAwards_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id));
END;

IF OBJECT_ID('dbo.AchievementBadges','U') IS NOT NULL
BEGIN
    MERGE dbo.AchievementBadges AS target
    USING (VALUES
      ('relay-3','Relay Spark','Complete the action that carries a Relay to 3 votes.','Link','RelayLength',3,0),
      ('relay-5','Relay Five','Complete the action that carries a Relay to 5 votes.','Link','RelayLength',5,0),
      ('relay-10','Relay Ten','Complete the action that carries a Relay to 10 votes.','Link','RelayLength',10,0),
      ('relay-25','Relay Wave','Complete the action that carries a Relay to 25 votes.','Link','RelayLength',25,0)
    ) AS source(Code,Name,Description,Icon,RuleType,Threshold,RewardXp)
    ON target.Code=source.Code
    WHEN MATCHED THEN UPDATE SET Name=source.Name,Description=source.Description,Icon=source.Icon,RuleType=source.RuleType,Threshold=source.Threshold,RewardXp=source.RewardXp
    WHEN NOT MATCHED THEN INSERT(Code,Name,Description,Icon,RuleType,Threshold,RewardXp) VALUES(source.Code,source.Name,source.Description,source.Icon,source.RuleType,source.Threshold,source.RewardXp);
END;

IF OBJECT_ID('dbo.RelayAbuseSignals','U') IS NULL
BEGIN
    CREATE TABLE dbo.RelayAbuseSignals(Id bigint IDENTITY PRIMARY KEY,ChainId bigint NOT NULL,ActorUserId bigint NOT NULL,RelatedUserId bigint NULL,SignalType varchar(40) NOT NULL,Severity tinyint NOT NULL,Details nvarchar(500) NULL,DetectedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),RewardsSuppressed bit NOT NULL DEFAULT 1,CONSTRAINT FK_RelayAbuse_Chain FOREIGN KEY(ChainId) REFERENCES dbo.RelayChains(Id));
END;

/* Reward rules use the ledger's semantic source uniqueness and configured period caps. */
IF OBJECT_ID('dbo.RewardRules','U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='relay.completed' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom,IsEnabled)
    VALUES('relay.completed',1,15,'Relay transfer completed',1,150,'day',1,SYSUTCDATETIME(),1);
IF OBJECT_ID('dbo.RewardRules','U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='relay.milestone' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom,IsEnabled)
    VALUES('relay.milestone',1,25,'Relay milestone reached',1,100,'week',1,SYSUTCDATETIME(),1);

COMMIT TRANSACTION;
