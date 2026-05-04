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

    PRINT 'Users table created.';
END
ELSE
    PRINT 'Users table already exists — skipped.';

-- Unique username index (outside IF so it's idempotent separately)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Users_Username' AND object_id = OBJECT_ID('Users')
)
    CREATE UNIQUE INDEX UQ_Users_Username ON Users(Username);

-- Unique email — filtered so multiple NULLs are allowed
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Users_Email' AND object_id = OBJECT_ID('Users')
)
    CREATE UNIQUE INDEX UQ_Users_Email ON Users(Email) WHERE Email IS NOT NULL;

-- ── Add UserId column to Votes ────────────────────────────────
-- Step 1: add the column first (no FK yet)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Votes' AND COLUMN_NAME = 'UserId'
)
BEGIN
    ALTER TABLE Votes ADD UserId BIGINT NULL;
    PRINT 'Votes.UserId column added.';
END
ELSE
    PRINT 'Votes.UserId already exists — skipped.';

-- Step 2: add FK constraint separately (only if column now exists and constraint doesn't)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_NAME = 'FK_Votes_Users' AND TABLE_NAME = 'Votes'
)
BEGIN
    ALTER TABLE Votes
        ADD CONSTRAINT FK_Votes_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id);
    PRINT 'FK_Votes_Users constraint added.';
END
ELSE
    PRINT 'FK_Votes_Users already exists — skipped.';

-- Step 3: unique index — one vote per (poll, user), NULLs excluded
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Votes_PollUser' AND object_id = OBJECT_ID('Votes')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Votes_PollUser
        ON Votes(PollId, UserId)
        WHERE UserId IS NOT NULL;
    PRINT 'UQ_Votes_PollUser index added.';
END
ELSE
    PRINT 'UQ_Votes_PollUser already exists — skipped.';
