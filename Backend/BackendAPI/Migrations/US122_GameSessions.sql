IF OBJECT_ID('dbo.GameSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GameSessions (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Mode NVARCHAR(40) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        PollCount INT NOT NULL,
        TimeLimitSeconds INT NULL,
        CompletionXp INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        StartedAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NULL,
        CompletedAt DATETIME2 NULL,
        CurrentPosition INT NOT NULL CONSTRAINT DF_GameSessions_Position DEFAULT 0,
        VotesCast INT NOT NULL CONSTRAINT DF_GameSessions_Votes DEFAULT 0,
        VoteXpEarned INT NOT NULL CONSTRAINT DF_GameSessions_VoteXp DEFAULT 0,
        CompletionXpAwarded INT NOT NULL CONSTRAINT DF_GameSessions_CompletionXp DEFAULT 0,
        RewardGrantedAt DATETIME2 NULL,
        CompletionSummary NVARCHAR(MAX) NULL,
        UpdatedAt DATETIME2 NOT NULL,
        RowVersion ROWVERSION,
        CONSTRAINT FK_GameSessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_GameSessions_Status CHECK (Status IN ('Active','Completed','Expired','Abandoned')),
        CONSTRAINT CK_GameSessions_Position CHECK (CurrentPosition >= 0 AND CurrentPosition <= PollCount)
    );
END;

IF OBJECT_ID('dbo.GameSessionPolls', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GameSessionPolls (
        SessionId BIGINT NOT NULL,
        PollId BIGINT NOT NULL,
        Position INT NOT NULL,
        VotedOptionId BIGINT NULL,
        VotedAt DATETIME2 NULL,
        CONSTRAINT PK_GameSessionPolls PRIMARY KEY (SessionId, Position),
        CONSTRAINT UQ_GameSessionPolls_Poll UNIQUE (SessionId, PollId),
        CONSTRAINT FK_GameSessionPolls_Session FOREIGN KEY (SessionId) REFERENCES dbo.GameSessions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_GameSessionPolls_Poll FOREIGN KEY (PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_GameSessionPolls_Option FOREIGN KEY (VotedOptionId) REFERENCES dbo.PollOptions(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_GameSessions_ActiveMode' AND object_id = OBJECT_ID('dbo.GameSessions'))
    CREATE UNIQUE INDEX UQ_GameSessions_ActiveMode ON dbo.GameSessions(UserId, Mode) WHERE Status = 'Active';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GameSessions_UserStatus' AND object_id = OBJECT_ID('dbo.GameSessions'))
    CREATE INDEX IX_GameSessions_UserStatus ON dbo.GameSessions(UserId, Status, UpdatedAt DESC);
