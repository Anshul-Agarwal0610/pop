IF COL_LENGTH('dbo.Users', 'AccountType') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD AccountType NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Users_AccountType DEFAULT 'Personal';
END;
GO

IF COL_LENGTH('dbo.BusinessAccounts', 'SubscriptionPlan') IS NULL
BEGIN
    ALTER TABLE dbo.BusinessAccounts ADD SubscriptionPlan NVARCHAR(40) NOT NULL
        CONSTRAINT DF_BusinessAccounts_SubscriptionPlan DEFAULT 'Starter';
END;
GO

IF COL_LENGTH('dbo.BusinessAccounts', 'SubscriptionStatus') IS NULL
BEGIN
    ALTER TABLE dbo.BusinessAccounts ADD SubscriptionStatus NVARCHAR(40) NOT NULL
        CONSTRAINT DF_BusinessAccounts_SubscriptionStatus DEFAULT 'Trialing';
END;
GO

IF COL_LENGTH('dbo.BusinessAccounts', 'TrialEndsAt') IS NULL
BEGIN
    ALTER TABLE dbo.BusinessAccounts ADD TrialEndsAt DATETIME2 NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_AccountType')
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_AccountType
        CHECK (AccountType IN ('Personal', 'Business'));
END;
