IF OBJECT_ID('dbo.PollPredictions','U') IS NULL
BEGIN
  CREATE TABLE dbo.PollPredictions(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY, UserId BIGINT NOT NULL, PollId BIGINT NOT NULL,
    PredictedOptionId BIGINT NOT NULL, WinningOptionId BIGINT NULL,
    Status NVARCHAR(20) NOT NULL CONSTRAINT DF_PollPredictions_Status DEFAULT 'Pending',
    XpAwarded INT NOT NULL CONSTRAINT DF_PollPredictions_Xp DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL, LocksAt DATETIME2 NOT NULL, SettledAt DATETIME2 NULL,
    CONSTRAINT FK_PollPredictions_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_PollPredictions_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
    CONSTRAINT FK_PollPredictions_Option FOREIGN KEY(PredictedOptionId) REFERENCES dbo.PollOptions(Id),
    CONSTRAINT FK_PollPredictions_Winner FOREIGN KEY(WinningOptionId) REFERENCES dbo.PollOptions(Id),
    CONSTRAINT UQ_PollPredictions_UserPoll UNIQUE(UserId,PollId),
    CONSTRAINT CK_PollPredictions_Status CHECK(Status IN ('Pending','Correct','Incorrect','NoContest'))
  );
  CREATE INDEX IX_PollPredictions_UserStatus ON dbo.PollPredictions(UserId,Status,CreatedAt DESC);
END;

IF NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='prediction.correct' AND Version=1)
  INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom,IsEnabled)
  VALUES('prediction.correct',1,150,'Correct poll outcome prediction',1,1500,'week',1,SYSUTCDATETIME(),1);
