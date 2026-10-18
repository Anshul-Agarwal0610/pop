-- Apply after the Creative Studio and Result Reveal migrations.
IF COL_LENGTH('dbo.Polls', 'RemixedFromPollId') IS NULL
    ALTER TABLE dbo.Polls ADD RemixedFromPollId bigint NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Polls_RemixedFromPollId' AND object_id=OBJECT_ID('dbo.Polls'))
    CREATE INDEX IX_Polls_RemixedFromPollId ON dbo.Polls(RemixedFromPollId) WHERE RemixedFromPollId IS NOT NULL;
GO
