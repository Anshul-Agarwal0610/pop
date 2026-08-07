-- US-155: authoritative multiplayer sessions. Additive and safe to rerun.
IF OBJECT_ID('dbo.LiveSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        PublicId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_LiveSessions_PublicId DEFAULT NEWSEQUENTIALID(),
        HostUserId BIGINT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LiveSessions_Status DEFAULT 'Lobby',
        CurrentRound INT NOT NULL CONSTRAINT DF_LiveSessions_Round DEFAULT 0,
        StateVersion BIGINT NOT NULL CONSTRAINT DF_LiveSessions_Version DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_LiveSessions_Created DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_LiveSessions_Updated DEFAULT SYSUTCDATETIME(),
        CompletedAt DATETIME2 NULL,
        RowVersion ROWVERSION,
        CONSTRAINT UQ_LiveSessions_PublicId UNIQUE(PublicId),
        CONSTRAINT FK_LiveSessions_Host FOREIGN KEY(HostUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_LiveSessions_Status CHECK(Status IN ('Lobby','Voting','Revealed','Completed','Expired'))
    );
END;

IF OBJECT_ID('dbo.LiveSessionMembers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionMembers (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY, SessionId BIGINT NOT NULL, UserId BIGINT NOT NULL,
        Role NVARCHAR(10) NOT NULL CONSTRAINT DF_LiveMembers_Role DEFAULT 'Member',
        Status NVARCHAR(12) NOT NULL CONSTRAINT DF_LiveMembers_Status DEFAULT 'Active',
        IsReady BIT NOT NULL CONSTRAINT DF_LiveMembers_Ready DEFAULT 0,
        EligibleFromRound INT NOT NULL CONSTRAINT DF_LiveMembers_Eligible DEFAULT 1,
        JoinedAt DATETIME2 NOT NULL CONSTRAINT DF_LiveMembers_Joined DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_LiveMembers_Updated DEFAULT SYSUTCDATETIME(), LeftAt DATETIME2 NULL,
        CONSTRAINT UQ_LiveMembers_SessionUser UNIQUE(SessionId,UserId),
        CONSTRAINT FK_LiveMembers_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveMembers_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id)
    );
END;

IF OBJECT_ID('dbo.LiveSessionRounds', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionRounds (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY, SessionId BIGINT NOT NULL, RoundNumber INT NOT NULL, PollId BIGINT NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_LiveRounds_Status DEFAULT 'Waiting',
        VotingStartedAt DATETIME2 NULL, RevealAt DATETIME2 NULL, RevealedAt DATETIME2 NULL,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_LiveRounds_Updated DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_LiveRounds_SessionRound UNIQUE(SessionId,RoundNumber),
        CONSTRAINT FK_LiveRounds_Session FOREIGN KEY(SessionId) REFERENCES dbo.LiveSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveRounds_Poll FOREIGN KEY(PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT CK_LiveRounds_Status CHECK(Status IN ('Waiting','Voting','Revealed','Completed'))
    );
END;

IF OBJECT_ID('dbo.LiveSessionVotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LiveSessionVotes (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY, RoundId BIGINT NOT NULL, MemberId BIGINT NOT NULL,
        OptionId BIGINT NOT NULL, IdempotencyKey UNIQUEIDENTIFIER NOT NULL, LockedAt DATETIME2 NOT NULL,
        RewardProcessedAt DATETIME2 NULL,
        CONSTRAINT UQ_LiveVotes_RoundMember UNIQUE(RoundId,MemberId),
        CONSTRAINT UQ_LiveVotes_RoundKey UNIQUE(RoundId,IdempotencyKey),
        CONSTRAINT FK_LiveVotes_Round FOREIGN KEY(RoundId) REFERENCES dbo.LiveSessionRounds(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LiveVotes_Member FOREIGN KEY(MemberId) REFERENCES dbo.LiveSessionMembers(Id),
        CONSTRAINT FK_LiveVotes_Option FOREIGN KEY(OptionId) REFERENCES dbo.PollOptions(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LiveMembers_User' AND object_id=OBJECT_ID('dbo.LiveSessionMembers'))
    CREATE INDEX IX_LiveMembers_User ON dbo.LiveSessionMembers(UserId,Status) INCLUDE(SessionId);
