SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.PollClashes','U') IS NULL CREATE TABLE dbo.PollClashes(
 Id BIGINT IDENTITY PRIMARY KEY, CreatorUserId BIGINT NOT NULL REFERENCES dbo.Users(Id), InviteCode VARCHAR(16) NOT NULL,
 Status VARCHAR(12) NOT NULL DEFAULT 'Lobby', Source VARCHAR(20) NOT NULL, RoundCount TINYINT NOT NULL, CurrentPosition TINYINT NOT NULL DEFAULT 0,
 RootClashId BIGINT NULL, PreviousClashId BIGINT NULL, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), StartedAt DATETIME2 NULL, CompletedAt DATETIME2 NULL, ExpiresAt DATETIME2 NOT NULL,
 CONSTRAINT UQ_PollClashes_InviteCode UNIQUE(InviteCode), CONSTRAINT CK_PollClashes_Status CHECK(Status IN('Lobby','Active','Completed','Expired')),
 CONSTRAINT CK_PollClashes_Source CHECK(Source IN('Poll','GeneratedPack')), CONSTRAINT CK_PollClashes_Rounds CHECK(RoundCount IN(1,3,5)),
 CONSTRAINT FK_PollClashes_Root FOREIGN KEY(RootClashId) REFERENCES dbo.PollClashes(Id), CONSTRAINT FK_PollClashes_Previous FOREIGN KEY(PreviousClashId) REFERENCES dbo.PollClashes(Id));

IF OBJECT_ID('dbo.PollClashPlayers','U') IS NULL CREATE TABLE dbo.PollClashPlayers(
 ClashId BIGINT NOT NULL REFERENCES dbo.PollClashes(Id), UserId BIGINT NOT NULL REFERENCES dbo.Users(Id), Position TINYINT NOT NULL, JoinedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_PollClashPlayers PRIMARY KEY(ClashId,UserId), CONSTRAINT UQ_PollClashPlayers_Position UNIQUE(ClashId,Position), CONSTRAINT CK_PollClashPlayers_Position CHECK(Position IN(0,1)));

IF OBJECT_ID('dbo.PollClashRounds','U') IS NULL CREATE TABLE dbo.PollClashRounds(
 Id BIGINT IDENTITY PRIMARY KEY, ClashId BIGINT NOT NULL REFERENCES dbo.PollClashes(Id), PollId BIGINT NOT NULL REFERENCES dbo.Polls(Id), Position TINYINT NOT NULL,
 Status VARCHAR(12) NOT NULL DEFAULT 'Pending', FirstOptionVotes INT NULL, SecondOptionVotes INT NULL, ResolvedMajorityOptionId BIGINT NULL REFERENCES dbo.PollOptions(Id), RevealedAt DATETIME2 NULL,
 CONSTRAINT UQ_PollClashRounds_Position UNIQUE(ClashId,Position), CONSTRAINT UQ_PollClashRounds_Poll UNIQUE(ClashId,PollId), CONSTRAINT CK_PollClashRounds_Status CHECK(Status IN('Pending','Active','Revealed')));

IF OBJECT_ID('dbo.PollClashResponses','U') IS NULL CREATE TABLE dbo.PollClashResponses(
 Id BIGINT IDENTITY PRIMARY KEY, RoundId BIGINT NOT NULL REFERENCES dbo.PollClashRounds(Id), UserId BIGINT NOT NULL REFERENCES dbo.Users(Id), OpinionOptionId BIGINT NOT NULL REFERENCES dbo.PollOptions(Id),
 PredictedMajorityOptionId BIGINT NULL REFERENCES dbo.PollOptions(Id), PredictionPoint TINYINT NOT NULL DEFAULT 0, SubmittedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT UQ_PollClashResponses_Player UNIQUE(RoundId,UserId), CONSTRAINT CK_PollClashResponses_Point CHECK(PredictionPoint IN(0,1)));

IF OBJECT_ID('dbo.PollClashRematches','U') IS NULL CREATE TABLE dbo.PollClashRematches(
 Id BIGINT IDENTITY PRIMARY KEY, ClashId BIGINT NOT NULL REFERENCES dbo.PollClashes(Id), RequestedByUserId BIGINT NOT NULL REFERENCES dbo.Users(Id), Status VARCHAR(12) NOT NULL DEFAULT 'Pending',
 RequestedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), RespondedAt DATETIME2 NULL, ResultingClashId BIGINT NULL REFERENCES dbo.PollClashes(Id),
 CONSTRAINT CK_PollClashRematches_Status CHECK(Status IN('Pending','Accepted','Declined')));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_PollClashRematches_Active') CREATE UNIQUE INDEX UX_PollClashRematches_Active ON dbo.PollClashRematches(ClashId) WHERE Status='Pending';

IF NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='clash.participation' AND Version=1)
 INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom) VALUES('clash.participation',1,20,'Completed a Poll Clash',1,100,'day',1,'2000-01-01');
IF NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='clash.prediction' AND Version=1)
 INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,PeriodLimit,PeriodUnit,PeriodValue,EffectiveFrom) VALUES('clash.prediction',1,10,'Correct Poll Clash prediction',1,100,'day',1,'2000-01-01');

COMMIT;
