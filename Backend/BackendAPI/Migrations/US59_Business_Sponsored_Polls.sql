IF OBJECT_ID('dbo.BusinessAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessAccounts (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        OwnerUserId BIGINT NOT NULL,
        Name NVARCHAR(160) NOT NULL,
        WebsiteUrl NVARCHAR(500) NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_BusinessAccounts_Status DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_BusinessAccounts_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_BusinessAccounts_Users FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(Id)
    );
END;

IF OBJECT_ID('dbo.BusinessCampaigns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessCampaigns (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        BusinessId BIGINT NOT NULL,
        Name NVARCHAR(180) NOT NULL,
        Objective NVARCHAR(500) NOT NULL CONSTRAINT DF_BusinessCampaigns_Objective DEFAULT '',
        StartsAt DATETIME2 NULL,
        EndsAt DATETIME2 NULL,
        Status NVARCHAR(40) NOT NULL CONSTRAINT DF_BusinessCampaigns_Status DEFAULT 'Draft',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_BusinessCampaigns_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_BusinessCampaigns_BusinessAccounts FOREIGN KEY (BusinessId) REFERENCES dbo.BusinessAccounts(Id)
    );
END;

IF COL_LENGTH('dbo.Polls', 'IsSponsored') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD IsSponsored BIT NOT NULL CONSTRAINT DF_Polls_IsSponsored DEFAULT 0;
END;

IF COL_LENGTH('dbo.Polls', 'BusinessId') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD BusinessId BIGINT NULL;
END;

IF COL_LENGTH('dbo.Polls', 'CampaignId') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD CampaignId BIGINT NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Polls_BusinessAccounts'
)
BEGIN
    ALTER TABLE dbo.Polls
    ADD CONSTRAINT FK_Polls_BusinessAccounts FOREIGN KEY (BusinessId) REFERENCES dbo.BusinessAccounts(Id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Polls_BusinessCampaigns'
)
BEGIN
    ALTER TABLE dbo.Polls
    ADD CONSTRAINT FK_Polls_BusinessCampaigns FOREIGN KEY (CampaignId) REFERENCES dbo.BusinessCampaigns(Id);
END;

IF OBJECT_ID('dbo.SponsoredPollMetrics', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SponsoredPollMetrics (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        CampaignId BIGINT NOT NULL,
        PollId BIGINT NOT NULL,
        Impressions INT NOT NULL CONSTRAINT DF_SponsoredPollMetrics_Impressions DEFAULT 0,
        Votes INT NOT NULL CONSTRAINT DF_SponsoredPollMetrics_Votes DEFAULT 0,
        Completions INT NOT NULL CONSTRAINT DF_SponsoredPollMetrics_Completions DEFAULT 0,
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_SponsoredPollMetrics_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_SponsoredPollMetrics_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.BusinessCampaigns(Id),
        CONSTRAINT FK_SponsoredPollMetrics_Polls FOREIGN KEY (PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT UQ_SponsoredPollMetrics_Poll UNIQUE (PollId)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BusinessAccounts_Owner'
      AND object_id = OBJECT_ID('dbo.BusinessAccounts')
)
BEGIN
    CREATE INDEX IX_BusinessAccounts_Owner ON dbo.BusinessAccounts (OwnerUserId, Status);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BusinessCampaigns_Business'
      AND object_id = OBJECT_ID('dbo.BusinessCampaigns')
)
BEGIN
    CREATE INDEX IX_BusinessCampaigns_Business ON dbo.BusinessCampaigns (BusinessId, Status, CreatedAt DESC);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Polls_Sponsored'
      AND object_id = OBJECT_ID('dbo.Polls')
)
BEGIN
    CREATE INDEX IX_Polls_Sponsored ON dbo.Polls (IsSponsored, BusinessId, CampaignId)
    WHERE IsSponsored = 1;
END;
