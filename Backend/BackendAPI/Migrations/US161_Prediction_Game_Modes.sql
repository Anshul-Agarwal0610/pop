-- Allow a user to play both prediction modes on the same poll and store split accuracy.
IF COL_LENGTH('dbo.PollPredictions','PredictionType') IS NULL
    ALTER TABLE dbo.PollPredictions ADD PredictionType VARCHAR(12) NOT NULL
        CONSTRAINT DF_PollPredictions_PredictionType DEFAULT 'Winner';
IF COL_LENGTH('dbo.PollPredictions','PredictedPercentage') IS NULL
    ALTER TABLE dbo.PollPredictions ADD PredictedPercentage DECIMAL(5,1) NULL;
IF COL_LENGTH('dbo.PollPredictions','ActualPercentage') IS NULL
    ALTER TABLE dbo.PollPredictions ADD ActualPercentage DECIMAL(5,1) NULL;
IF COL_LENGTH('dbo.PollPredictions','AccuracyDifference') IS NULL
    ALTER TABLE dbo.PollPredictions ADD AccuracyDifference DECIMAL(5,1) NULL;
GO

IF EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID('dbo.PollPredictions') AND name='UQ_PollPredictions_UserPoll')
    ALTER TABLE dbo.PollPredictions DROP CONSTRAINT UQ_PollPredictions_UserPoll;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.PollPredictions') AND name='UX_PollPredictions_UserPollType')
    CREATE UNIQUE INDEX UX_PollPredictions_UserPollType ON dbo.PollPredictions(UserId,PollId,PredictionType);

IF OBJECT_ID('dbo.CK_PollPredictions_Type','C') IS NULL
    ALTER TABLE dbo.PollPredictions ADD CONSTRAINT CK_PollPredictions_Type CHECK(PredictionType IN('Winner','Split'));
IF OBJECT_ID('dbo.CK_PollPredictions_Percentage','C') IS NULL
    ALTER TABLE dbo.PollPredictions ADD CONSTRAINT CK_PollPredictions_Percentage CHECK(
        (PredictionType='Winner' AND PredictedPercentage IS NULL) OR
        (PredictionType='Split' AND PredictedPercentage BETWEEN 50 AND 95));

DECLARE @Now DATETIME2=SYSUTCDATETIME();
DECLARE @Rules TABLE(Code VARCHAR(80),Value INT,Reason NVARCHAR(160),PeriodLimit INT);
INSERT @Rules VALUES
 ('prediction.split.exact',250,'Exact split prediction',2500),
 ('prediction.split.close3',175,'Split prediction within 3%',1750),
 ('prediction.split.close5',100,'Split prediction within 5%',1000),
 ('prediction.split.close10',50,'Split prediction within 10%',500);
INSERT RewardRules(Code,Version,Value,Reason,PeriodLimit,PeriodUnit,PeriodValue,IsEnabled,EffectiveFrom)
SELECT r.Code,1,r.Value,r.Reason,r.PeriodLimit,'week',1,1,@Now FROM @Rules r
WHERE NOT EXISTS(SELECT 1 FROM RewardRules x WHERE x.Code=r.Code AND x.Version=1);
