-- ============================================================
-- US-50: Daily streak support
-- Adds LastVoteDate so XP can be awarded on every unique vote
-- while streaks advance at most once per UTC day.
-- Idempotent - safe to run multiple times.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastVoteDate'
)
BEGIN
    ALTER TABLE Users ADD LastVoteDate DATE NULL;
    PRINT 'Users.LastVoteDate column added.';
END
ELSE
    PRINT 'Users.LastVoteDate already exists - skipped.';
