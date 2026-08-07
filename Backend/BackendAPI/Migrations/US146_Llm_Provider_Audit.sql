IF COL_LENGTH('dbo.Polls', 'GenerationProvider') IS NULL
    ALTER TABLE dbo.Polls ADD GenerationProvider NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Polls', 'GenerationModel') IS NULL
    ALTER TABLE dbo.Polls ADD GenerationModel NVARCHAR(200) NULL;
GO
