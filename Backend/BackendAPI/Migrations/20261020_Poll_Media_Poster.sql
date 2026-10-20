IF COL_LENGTH('dbo.Polls', 'MediaPosterUrl') IS NULL
    ALTER TABLE dbo.Polls ADD MediaPosterUrl nvarchar(2048) NULL;
