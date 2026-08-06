IF OBJECT_ID('dbo.LiveSessions','U') IS NULL
BEGIN
 CREATE TABLE dbo.LiveSessions(
  Id BIGINT IDENTITY PRIMARY KEY, PublicId CHAR(32) NOT NULL, HostUserId BIGINT NOT NULL,
  Mode NVARCHAR(20) NOT NULL, Status NVARCHAR(20) NOT NULL, PollId BIGINT NOT NULL,
  TargetVoteCount INT NOT NULL, ValidLockedVoteCount INT NOT NULL CONSTRAINT DF_LiveSessions_Locked DEFAULT 0,
  DurationSeconds INT NOT NULL, Capacity INT NOT NULL, ExpiryPolicy NVARCHAR(40) NOT NULL,
  StateVersion INT NOT NULL CONSTRAINT DF_LiveSessions_Version DEFAULT 1, ExpiresAt DATETIME2 NOT NULL,
  RevealedAt DATETIME2 NULL, CompletedAt DATETIME2 NULL, TerminalReason NVARCHAR(80) NULL,
  CreatedAt DATETIME2 NOT NULL, UpdatedAt DATETIME2 NOT NULL, RowVersion ROWVERSION,
  CONSTRAINT UQ_LiveSessions_PublicId UNIQUE(PublicId), CONSTRAINT FK_LiveSessions_Host FOREIGN KEY(HostUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT FK_LiveSessions_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
  CONSTRAINT CK_LiveSessions_Mode CHECK(Mode='Bomb'), CONSTRAINT CK_LiveSessions_Status CHECK(Status IN('Voting','Revealed','Expired')),
  CONSTRAINT CK_LiveSessions_Target CHECK(TargetVoteCount IN(3,5,10,20)),
  CONSTRAINT CK_LiveSessions_Duration CHECK(DurationSeconds IN(900,3600,21600,86400)),
  CONSTRAINT CK_LiveSessions_Expiry CHECK(ExpiryPolicy='ExpireWithoutReveal'), CONSTRAINT CK_LiveSessions_Capacity CHECK(Capacity<=25)
 );
END;
IF OBJECT_ID('dbo.LiveSessionParticipants','U') IS NULL
BEGIN
 CREATE TABLE dbo.LiveSessionParticipants(Id BIGINT IDENTITY PRIMARY KEY,SessionId BIGINT NOT NULL,UserId BIGINT NOT NULL,Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LiveParticipants_Status DEFAULT 'Active',NotificationsEnabled BIT NOT NULL CONSTRAINT DF_LiveParticipants_Notify DEFAULT 0,ReminderCount INT NOT NULL CONSTRAINT DF_LiveParticipants_Reminders DEFAULT 0,LastReminderAt DATETIME2 NULL,JoinedAt DATETIME2 NOT NULL,RemovedAt DATETIME2 NULL,
 CONSTRAINT UQ_LiveParticipants_User UNIQUE(SessionId,UserId),CONSTRAINT FK_LiveParticipants_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,CONSTRAINT FK_LiveParticipants_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_LiveParticipants_Status CHECK(Status IN('Active','Removed')));
END;
IF OBJECT_ID('dbo.LiveSessionResponses','U') IS NULL
BEGIN
 CREATE TABLE dbo.LiveSessionResponses(Id BIGINT IDENTITY PRIMARY KEY,SessionId BIGINT NOT NULL,ParticipantId BIGINT NOT NULL,OptionId BIGINT NOT NULL,IdempotencyKey NVARCHAR(100) NOT NULL,LockedAt DATETIME2 NOT NULL,
 CONSTRAINT UQ_LiveResponses_Participant UNIQUE(SessionId,ParticipantId),CONSTRAINT UQ_LiveResponses_Key UNIQUE(SessionId,IdempotencyKey),CONSTRAINT FK_LiveResponses_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,CONSTRAINT FK_LiveResponses_Participant FOREIGN KEY(ParticipantId) REFERENCES dbo.LiveSessionParticipants(Id),CONSTRAINT FK_LiveResponses_Option FOREIGN KEY(OptionId) REFERENCES dbo.PollOptions(Id));
END;
IF OBJECT_ID('dbo.LiveSessionEvents','U') IS NULL
BEGIN
 CREATE TABLE dbo.LiveSessionEvents(Id BIGINT IDENTITY PRIMARY KEY,SessionId BIGINT NOT NULL,Sequence BIGINT NOT NULL,Type NVARCHAR(50) NOT NULL,StateVersion INT NOT NULL,Payload NVARCHAR(MAX) NOT NULL,CreatedAt DATETIME2 NOT NULL,
 CONSTRAINT UQ_LiveEvents_Sequence UNIQUE(SessionId,Sequence),CONSTRAINT FK_LiveEvents_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE);
 CREATE UNIQUE INDEX UQ_LiveEvents_Reveal ON dbo.LiveSessionEvents(SessionId,Type) WHERE Type='BombRevealed';
 CREATE UNIQUE INDEX UQ_LiveEvents_Expiry ON dbo.LiveSessionEvents(SessionId,Type) WHERE Type='BombExpired';
END;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_LiveSessions_Expiry' AND object_id=OBJECT_ID('dbo.LiveSessions')) CREATE INDEX IX_LiveSessions_Expiry ON dbo.LiveSessions(Status,ExpiresAt);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_LiveParticipants_Reminders' AND object_id=OBJECT_ID('dbo.LiveSessionParticipants')) CREATE INDEX IX_LiveParticipants_Reminders ON dbo.LiveSessionParticipants(NotificationsEnabled,LastReminderAt) INCLUDE(SessionId,UserId,ReminderCount,Status);
