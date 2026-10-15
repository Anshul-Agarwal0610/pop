IF COL_LENGTH('dbo.Users', 'TermsAcceptedAt') IS NULL
    ALTER TABLE dbo.Users ADD TermsAcceptedAt DATETIME2 NULL;
GO

IF COL_LENGTH('dbo.Users', 'TermsVersion') IS NULL
    ALTER TABLE dbo.Users ADD TermsVersion NVARCHAR(20) NULL;
GO
