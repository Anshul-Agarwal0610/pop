-- ============================================================
-- Pollify Database Setup
-- Run this script once against your SQL Server instance
-- ============================================================

CREATE DATABASE PollifyDB;
GO

USE PollifyDB;
GO

-- ============================================================
-- Polls
-- ============================================================
CREATE TABLE Polls (
    Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
    Question    NVARCHAR(500)   NOT NULL,
    Description NVARCHAR(1000)  NOT NULL DEFAULT '',
    Category    NVARCHAR(100)   NOT NULL DEFAULT 'General',
    IsTrending  BIT             NOT NULL DEFAULT 0,
    IsActive    BIT             NOT NULL DEFAULT 1,
    ExpiresAt   DATETIME2       NOT NULL,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    TotalVotes  INT             NOT NULL DEFAULT 0,
    CreatedByUserId BIGINT      NULL,
    GenerationProvider NVARCHAR(50) NULL,
    GenerationModel NVARCHAR(200) NULL
);
GO

-- ============================================================
-- Poll Options
-- ============================================================
CREATE TABLE PollOptions (
    Id             BIGINT IDENTITY(1,1) PRIMARY KEY,
    PollId         BIGINT          NOT NULL REFERENCES Polls(Id) ON DELETE CASCADE,
    Text           NVARCHAR(300)   NOT NULL,
    Side           NVARCHAR(16)    NULL CHECK (Side IS NULL OR Side IN ('Up', 'Against')),
    VoteCount      INT             NOT NULL DEFAULT 0,
    VotePercentage FLOAT           NOT NULL DEFAULT 0
);
GO
CREATE UNIQUE INDEX UX_PollOptions_PollId_Side ON PollOptions(PollId, Side) WHERE Side IS NOT NULL;
ALTER TABLE PollOptions ADD CONSTRAINT UQ_PollOptions_PollId_Id UNIQUE (PollId, Id);

-- ============================================================
-- Votes
-- ============================================================
CREATE TABLE Votes (
    Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    PollId    BIGINT    NOT NULL,
    OptionId  BIGINT    NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO
ALTER TABLE Votes ADD CONSTRAINT FK_Votes_PollOptions
    FOREIGN KEY (PollId, OptionId) REFERENCES PollOptions(PollId, Id);

-- ============================================================
-- Users
-- ============================================================
CREATE TABLE Users (
    Id           BIGINT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(100)  NOT NULL UNIQUE,
    DisplayName  NVARCHAR(200)  NOT NULL,
    Email        NVARCHAR(255)  NULL,
    PasswordHash NVARCHAR(500)  NULL,
    AvatarUrl    NVARCHAR(500)  NULL,
    AuthProvider NVARCHAR(20)   NOT NULL DEFAULT 'local',
    Xp           INT            NOT NULL DEFAULT 0,
    Streak       INT            NOT NULL DEFAULT 0,
    LongestStreak INT           NOT NULL DEFAULT 0,
    TotalVotes   INT            NOT NULL DEFAULT 0,
    PollsCreated INT            NOT NULL DEFAULT 0,
    LastVoteDate DATE           NULL,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE StreakRecoveries (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL REFERENCES Users(Id),
    MissedUtcDate DATE NOT NULL,
    AppliedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PollId BIGINT NOT NULL REFERENCES Polls(Id),
    CONSTRAINT UQ_StreakRecoveries_UserMissedDate UNIQUE (UserId, MissedUtcDate)
);
CREATE INDEX IX_StreakRecoveries_UserAppliedAt ON StreakRecoveries(UserId, AppliedAt DESC);
GO

-- Authoritative exactly-once audit ledger for progression awards.
CREATE TABLE ProgressionRewardEvents (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId BIGINT NOT NULL REFERENCES Users(Id),
    EventType NVARCHAR(32) NOT NULL,
    SourceId NVARCHAR(128) NOT NULL,
    AwardedXp INT NOT NULL CHECK (AwardedXp >= 0),
    TotalXp INT NULL,
    Level INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_ProgressionRewardEvents_Source UNIQUE (UserId, EventType, SourceId)
);
GO

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX IX_Polls_IsTrending  ON Polls(IsTrending) WHERE IsActive = 1;
CREATE INDEX IX_Polls_CreatedAt   ON Polls(CreatedAt DESC);
CREATE INDEX IX_Polls_CreatedByUserId ON Polls(CreatedByUserId) WHERE CreatedByUserId IS NOT NULL;
CREATE INDEX IX_PollOptions_PollId ON PollOptions(PollId);
CREATE INDEX IX_Votes_PollId       ON Votes(PollId);
CREATE INDEX IX_Users_Xp           ON Users(Xp DESC);
GO

ALTER TABLE Polls
    ADD CONSTRAINT FK_Polls_CreatedByUser
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id);
GO

-- ============================================================
-- Seed Data (optional dev data)
-- ============================================================
INSERT INTO Polls (Question, Description, Category, ExpiresAt, IsTrending)
VALUES
    ('Should remote work become the default?', 'Share your opinion on the future of work.', 'Work', DATEADD(DAY, 2, GETUTCDATE()), 1),
    ('Best programming language in 2026?',     'Vote for your favourite language.',           'Tech', DATEADD(DAY, 5, GETUTCDATE()), 1),
    ('Favourite streaming platform?',           'Netflix, Prime, Disney+ or something else?', 'Entertainment', DATEADD(DAY, 1, GETUTCDATE()), 0);
GO

INSERT INTO PollOptions (PollId, Text) VALUES
    (1, 'Yes, fully remote'),
    (1, 'Hybrid is better'),
    (1, 'Office is best');

INSERT INTO PollOptions (PollId, Text) VALUES
    (2, 'TypeScript'),
    (2, 'Python'),
    (2, 'Rust'),
    (2, 'Go');

INSERT INTO PollOptions (PollId, Text) VALUES
    (3, 'Netflix'),
    (3, 'Prime Video'),
    (3, 'Disney+'),
    (3, 'Other');
GO
