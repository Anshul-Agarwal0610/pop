-- Persistent media metadata for custom poll attachments.
IF COL_LENGTH('dbo.Polls', 'MediaType') IS NULL
    ALTER TABLE dbo.Polls ADD MediaType nvarchar(32) NULL;
GO
