SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.PollPacks','U') IS NULL
BEGIN
    CREATE TABLE dbo.PollPacks(
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_PollPacks PRIMARY KEY,
        OwnerUserId BIGINT NOT NULL, Name NVARCHAR(150) NOT NULL,
        IsPublished BIT NOT NULL CONSTRAINT DF_PollPacks_Published DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_PollPacks_Active DEFAULT 1,
        IsPublic BIT NOT NULL CONSTRAINT DF_PollPacks_Public DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PollPacks_Created DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PollPacks_Users FOREIGN KEY(OwnerUserId) REFERENCES dbo.Users(Id)
    );
END;
IF OBJECT_ID('dbo.PollPackPolls','U') IS NULL
BEGIN
    CREATE TABLE dbo.PollPackPolls(
        PollPackId BIGINT NOT NULL, PollId BIGINT NOT NULL, Position INT NOT NULL,
        CONSTRAINT PK_PollPackPolls PRIMARY KEY(PollPackId,Position),
        CONSTRAINT UQ_PollPackPolls_Poll UNIQUE(PollPackId,PollId),
        CONSTRAINT FK_PollPackPolls_Pack FOREIGN KEY(PollPackId) REFERENCES dbo.PollPacks(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PollPackPolls_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT CK_PollPackPolls_Position CHECK(Position>=0)
    );
END;

IF OBJECT_ID('dbo.LiveSessions','U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessions(
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_LiveSessions PRIMARY KEY,
        HostUserId BIGINT NOT NULL, Mode NVARCHAR(20) NOT NULL, ModeConfiguration NVARCHAR(MAX) NOT NULL,
        ContentType NVARCHAR(20) NOT NULL, PollId BIGINT NULL, PollPackId BIGINT NULL,
        Status NVARCHAR(20) NOT NULL, JoinCode CHAR(8) NOT NULL,
        CreatedAt DATETIME2 NOT NULL, StartedAt DATETIME2 NULL, LastActivityAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL, CompletedAt DATETIME2 NULL, TerminalReason NVARCHAR(100) NULL,
        RowVersion ROWVERSION,
        CONSTRAINT FK_LiveSessions_Host FOREIGN KEY(HostUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_LiveSessions_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_LiveSessions_Pack FOREIGN KEY(PollPackId) REFERENCES dbo.PollPacks(Id),
        CONSTRAINT CK_LiveSessions_Mode CHECK(Mode IN ('Clash','Relay','Bomb','Room')),
        CONSTRAINT CK_LiveSessions_Status CHECK(Status IN ('Lobby','Active','Completed','Expired','Abandoned')),
        CONSTRAINT CK_LiveSessions_Content CHECK((ContentType='Poll' AND PollId IS NOT NULL AND PollPackId IS NULL) OR (ContentType='PollPack' AND PollPackId IS NOT NULL AND PollId IS NULL)),
        CONSTRAINT CK_LiveSessions_ConfigJson CHECK(ISJSON(ModeConfiguration)=1)
    );
    CREATE UNIQUE INDEX UX_LiveSessions_JoinCode ON dbo.LiveSessions(JoinCode);
    CREATE INDEX IX_LiveSessions_HostStatus ON dbo.LiveSessions(HostUserId,Status,CreatedAt DESC);
    CREATE INDEX IX_LiveSessions_Expiry ON dbo.LiveSessions(Status,ExpiresAt) INCLUDE(LastActivityAt);
    CREATE INDEX IX_LiveSessions_Abandonment ON dbo.LiveSessions(Status,LastActivityAt);
END;

IF OBJECT_ID('dbo.LiveSessionParticipants','U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionParticipants(
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_LiveSessionParticipants PRIMARY KEY,
        SessionId BIGINT NOT NULL, UserId BIGINT NOT NULL, Status NVARCHAR(20) NOT NULL,
        JoinedAt DATETIME2 NOT NULL, LastActivityAt DATETIME2 NOT NULL, LeftAt DATETIME2 NULL,
        CONSTRAINT FK_LiveParticipants_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveParticipants_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT UQ_LiveParticipants_User UNIQUE(SessionId,UserId),
        CONSTRAINT CK_LiveParticipants_Status CHECK(Status IN ('Joined','Ready','Active','Left','Removed'))
    );
    CREATE INDEX IX_LiveParticipants_User ON dbo.LiveSessionParticipants(UserId,SessionId);
END;

IF OBJECT_ID('dbo.LiveSessionRounds','U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionRounds(
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_LiveSessionRounds PRIMARY KEY,
        SessionId BIGINT NOT NULL, RoundNumber INT NOT NULL, PollId BIGINT NOT NULL,
        Status NVARCHAR(20) NOT NULL, StartsAt DATETIME2 NULL, EndsAt DATETIME2 NULL, CompletedAt DATETIME2 NULL,
        RulesSnapshot NVARCHAR(MAX) NULL,
        CONSTRAINT FK_LiveRounds_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveRounds_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT UQ_LiveRounds_Number UNIQUE(SessionId,RoundNumber),
        CONSTRAINT CK_LiveRounds_Status CHECK(Status IN ('Pending','Active','Completed','Cancelled'))
    );
END;

IF OBJECT_ID('dbo.LiveSessionResponses','U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionResponses(
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_LiveSessionResponses PRIMARY KEY,
        SessionId BIGINT NOT NULL, RoundId BIGINT NOT NULL, ParticipantId BIGINT NOT NULL,
        PollId BIGINT NOT NULL, OptionId BIGINT NOT NULL, SubmittedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_LiveResponses_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id),
        CONSTRAINT FK_LiveResponses_Round FOREIGN KEY(RoundId) REFERENCES dbo.LiveSessionRounds(Id),
        CONSTRAINT FK_LiveResponses_Participant FOREIGN KEY(ParticipantId) REFERENCES dbo.LiveSessionParticipants(Id),
        CONSTRAINT FK_LiveResponses_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_LiveResponses_Option FOREIGN KEY(OptionId) REFERENCES dbo.PollOptions(Id),
        CONSTRAINT UQ_LiveResponses_Replay UNIQUE(RoundId,ParticipantId)
    );
END;

IF OBJECT_ID('dbo.LiveSessionEvents','U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionEvents(
        SessionId BIGINT NOT NULL, Sequence BIGINT NOT NULL, EventType NVARCHAR(60) NOT NULL,
        ActorUserId BIGINT NULL, Payload NVARCHAR(MAX) NOT NULL, SchemaVersion INT NOT NULL CONSTRAINT DF_LiveEvents_Schema DEFAULT 1,
        OccurredAt DATETIME2 NOT NULL,
        CONSTRAINT PK_LiveSessionEvents PRIMARY KEY(SessionId,Sequence),
        CONSTRAINT FK_LiveEvents_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveEvents_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_LiveEvents_Payload CHECK(ISJSON(Payload)=1)
    );
END;

IF OBJECT_ID('dbo.RewardRules','U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='live.session.complete' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,EffectiveFrom) VALUES('live.session.complete',1,100,'PoP Live session completed',1,'2000-01-01');
IF OBJECT_ID('dbo.RewardRules','U') IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RewardRules WHERE Code='live.round.win' AND Version=1)
    INSERT dbo.RewardRules(Code,Version,Value,Reason,PerActionLimit,EffectiveFrom) VALUES('live.round.win',1,25,'PoP Live round won',1,'2000-01-01');

COMMIT;
