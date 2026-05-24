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
    CreatedByUserId BIGINT      NULL
);
GO

-- ============================================================
-- Poll Options
-- ============================================================
CREATE TABLE PollOptions (
    Id             BIGINT IDENTITY(1,1) PRIMARY KEY,
    PollId         BIGINT          NOT NULL REFERENCES Polls(Id) ON DELETE CASCADE,
    Text           NVARCHAR(300)   NOT NULL,
    VoteCount      INT             NOT NULL DEFAULT 0,
    VotePercentage FLOAT           NOT NULL DEFAULT 0
);
GO

-- ============================================================
-- Votes
-- ============================================================
CREATE TABLE Votes (
    Id        BIGINT IDENTITY(1,1) PRIMARY KEY,
    PollId    BIGINT    NOT NULL REFERENCES Polls(Id),
    OptionId  BIGINT    NOT NULL REFERENCES PollOptions(Id),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

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
    TotalVotes   INT            NOT NULL DEFAULT 0,
    PollsCreated INT            NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE()
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
