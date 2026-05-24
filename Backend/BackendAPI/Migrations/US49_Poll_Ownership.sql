-- ============================================================
-- US-49: Poll ownership migration
-- Adds nullable creator ownership to Polls and keeps existing
-- system/AI-generated polls valid with NULL CreatedByUserId.
-- Idempotent - safe to run multiple times.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Polls' AND COLUMN_NAME = 'CreatedByUserId'
)
BEGIN
    ALTER TABLE Polls ADD CreatedByUserId BIGINT NULL;
    PRINT 'Polls.CreatedByUserId column added.';
END
ELSE
    PRINT 'Polls.CreatedByUserId already exists - skipped.';

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_NAME = 'Polls' AND CONSTRAINT_NAME = 'FK_Polls_CreatedByUser'
)
BEGIN
    EXEC('ALTER TABLE Polls ADD CONSTRAINT FK_Polls_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)');
    PRINT 'FK_Polls_CreatedByUser constraint added.';
END
ELSE
    PRINT 'FK_Polls_CreatedByUser already exists - skipped.';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Polls_CreatedByUserId' AND object_id = OBJECT_ID('Polls')
)
BEGIN
    EXEC('CREATE INDEX IX_Polls_CreatedByUserId ON Polls(CreatedByUserId) WHERE CreatedByUserId IS NOT NULL');
    PRINT 'IX_Polls_CreatedByUserId index added.';
END
ELSE
    PRINT 'IX_Polls_CreatedByUserId already exists - skipped.';
