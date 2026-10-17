-- Optional presentation settings for user-created polls. Existing polls keep their current layout.
IF COL_LENGTH('dbo.Polls', 'CreativeTemplate') IS NULL
    ALTER TABLE dbo.Polls ADD CreativeTemplate nvarchar(24) NULL;
GO
IF COL_LENGTH('dbo.Polls', 'ReactionId') IS NULL
    ALTER TABLE dbo.Polls ADD ReactionId nvarchar(24) NULL;
GO
