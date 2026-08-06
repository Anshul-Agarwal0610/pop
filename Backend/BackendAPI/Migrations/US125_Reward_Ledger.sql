SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Users','IsAdmin') IS NULL
    ALTER TABLE dbo.Users ADD IsAdmin BIT NOT NULL CONSTRAINT DF_Users_IsAdmin DEFAULT 0;

IF OBJECT_ID('dbo.RewardRules','U') IS NULL
BEGIN
    CREATE TABLE dbo.RewardRules(
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RewardRules PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        Version INT NOT NULL,
        Value INT NOT NULL,
        Reason NVARCHAR(300) NOT NULL,
        PerActionLimit INT NOT NULL CONSTRAINT DF_RewardRules_PerActionLimit DEFAULT 1,
        PeriodLimit INT NULL,
        PeriodUnit NVARCHAR(16) NULL,
        PeriodValue INT NULL,
        EffectiveFrom DATETIME2 NOT NULL,
        EffectiveTo DATETIME2 NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_RewardRules_IsEnabled DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RewardRules_CreatedAt DEFAULT GETUTCDATE(),
        CreatedByUserId BIGINT NULL,
        CONSTRAINT UQ_RewardRules_CodeVersion UNIQUE(Code,Version),
        CONSTRAINT FK_RewardRules_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_RewardRules_Value CHECK(Value>0),
        CONSTRAINT CK_RewardRules_ActionLimit CHECK(PerActionLimit>0),
        CONSTRAINT CK_RewardRules_Period CHECK((PeriodLimit IS NULL AND PeriodUnit IS NULL AND PeriodValue IS NULL) OR (PeriodLimit>0 AND PeriodUnit IN ('hour','day','week','month') AND PeriodValue>0)),
        CONSTRAINT CK_RewardRules_Window CHECK(EffectiveTo IS NULL OR EffectiveTo>EffectiveFrom)
    );
END;

IF OBJECT_ID('dbo.RewardEvents','U') IS NULL
BEGIN
    CREATE TABLE dbo.RewardEvents(
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RewardEvents PRIMARY KEY,
        UserId BIGINT NOT NULL,
        RuleId BIGINT NULL,
        RuleCode NVARCHAR(100) NOT NULL,
        RuleVersion INT NOT NULL,
        Reason NVARCHAR(300) NOT NULL,
        SourceType NVARCHAR(80) NOT NULL,
        SourceReference NVARCHAR(200) NOT NULL,
        SourceKey NVARCHAR(300) NOT NULL,
        Value INT NOT NULL,
        EventType NVARCHAR(16) NOT NULL,
        ReversesEventId BIGINT NULL,
        ActorUserId BIGINT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_RewardEvents_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_RewardEvents_Rule FOREIGN KEY(RuleId) REFERENCES dbo.RewardRules(Id),
        CONSTRAINT FK_RewardEvents_Reverses FOREIGN KEY(ReversesEventId) REFERENCES dbo.RewardEvents(Id),
        CONSTRAINT FK_RewardEvents_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_RewardEvents_UserSource UNIQUE(UserId,SourceKey),
        CONSTRAINT CK_RewardEvents_Value CHECK(Value<>0),
        CONSTRAINT CK_RewardEvents_Type CHECK(EventType IN ('Grant','Adjustment','Reversal')),
        CONSTRAINT CK_RewardEvents_Sign CHECK((EventType='Grant' AND Value>0 AND ReversesEventId IS NULL) OR (EventType='Reversal' AND Value<0 AND ReversesEventId IS NOT NULL) OR EventType='Adjustment'),
        CONSTRAINT CK_RewardEvents_NoSelfReverse CHECK(ReversesEventId IS NULL OR ReversesEventId<>Id)
    );
    CREATE UNIQUE INDEX UX_RewardEvents_Reversal ON dbo.RewardEvents(ReversesEventId) WHERE ReversesEventId IS NOT NULL;
    CREATE INDEX IX_RewardEvents_UserCreated ON dbo.RewardEvents(UserId,CreatedAt DESC);
    CREATE INDEX IX_RewardEvents_RuleCreated ON dbo.RewardEvents(RuleCode,CreatedAt DESC);
    CREATE INDEX IX_RewardEvents_Source ON dbo.RewardEvents(SourceType,SourceReference);
    CREATE INDEX IX_RewardEvents_Created ON dbo.RewardEvents(CreatedAt DESC);
END;

-- Existing balances become immutable snapshots. The source key makes this rerunnable.
INSERT dbo.RewardEvents(UserId,RuleCode,RuleVersion,Reason,SourceType,SourceReference,SourceKey,Value,EventType,CreatedAt)
SELECT u.Id,'legacy.opening_balance',1,'Opening balance migrated to reward ledger','migration',CONVERT(nvarchar(30),u.Id),'legacy:opening_balance',u.Xp,'Adjustment',GETUTCDATE()
FROM dbo.Users u WHERE u.Xp<>0 AND NOT EXISTS(SELECT 1 FROM dbo.RewardEvents e WHERE e.UserId=u.Id AND e.SourceKey='legacy:opening_balance');

IF NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='vote.standard' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom)
    VALUES('vote.standard',1,25,'Vote cast',1,500,'day',1,'2000-01-01');
IF NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='vote.trending' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom)
    VALUES('vote.trending',1,35,'Trending poll vote cast',1,700,'day',1,'2000-01-01');

COMMIT;
GO

CREATE OR ALTER TRIGGER dbo.TR_RewardEvents_AppendOnly ON dbo.RewardEvents
INSTEAD OF UPDATE, DELETE AS
BEGIN
    THROW 51000,'RewardEvents is append-only; append an adjustment or reversal.',1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_RewardRules_NoOverlap ON dbo.RewardRules
AFTER INSERT, UPDATE AS
BEGIN
    IF EXISTS(SELECT 1 FROM dbo.RewardRules a JOIN dbo.RewardRules b ON a.Code=b.Code AND a.Id<>b.Id
              WHERE a.IsEnabled=1 AND b.IsEnabled=1 AND a.EffectiveFrom<COALESCE(b.EffectiveTo,'9999-12-31') AND b.EffectiveFrom<COALESCE(a.EffectiveTo,'9999-12-31'))
    BEGIN
        ROLLBACK; THROW 51001,'Enabled reward rule versions may not overlap.',1;
    END
END;
GO
