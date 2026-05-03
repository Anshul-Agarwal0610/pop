-- ============================================================
-- US-13: Auth Schema Migration
-- Creates the Users table and adds UserId to the Votes table.
-- Idempotent — safe to run multiple times.
-- ============================================================

-- ── Users table ──────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'
)
BEGIN
    CREATE TABLE Users (
        Id           BIGINT        IDENTITY(1,1) PRIMARY KEY,
        Username     NVARCHAR(100) NOT NULL,
        DisplayName  NVARCHAR(100) NOT NULL DEFAULT '',
        Email        NVARCHAR(255) NULL,
        PasswordHash NVARCHAR(256) NULL,          -- NULL for OAuth users
        AvatarUrl    NVARCHAR(500) NULL,
        AuthProvider NVARCHAR(20)  NOT NULL DEFAULT 'local',
        Xp           INT           NOT NULL DEFAULT 0,
        Streak       INT           NOT NULL DEFAULT 0,
        TotalVotes   INT           NOT NULL DEFAULT 0,
        PollsCreated INT           NOT NULL DEFAULT 0,
        CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE()
    );

    -- Unique username (all users)
    CREATE UNIQUE INDEX UQ_Users_Username
        ON Users(Username);

    -- Unique email — filtered so multiple NULLs are allowed (local users without email)
    CREATE UNIQUE INDEX UQ_Users_Email
        ON Users(Email)
        WHERE Email IS NOT NULL;

    PRINT 'Users table created.';
END
ELSE
    PRINT 'Users table already exists — skipped.';

-- ── Add UserId to Votes ───────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Votes' AND COLUMN_NAME = 'UserId'
)
BEGIN
    ALTER TABLE Votes
        ADD UserId BIGINT NULL
            CONSTRAINT FK_Votes_Users FOREIGN KEY REFERENCES Users(Id);

    -- One vote per poll per user; NULLs (anonymous) are excluded from uniqueness
    CREATE UNIQUE INDEX UQ_Votes_PollUser
        ON Votes(PollId, UserId)
        WHERE UserId IS NOT NULL;

    PRINT 'Votes.UserId column and unique index added.';
END
ELSE
    PRINT 'Votes.UserId already exists — skipped.';
