IF COL_LENGTH('dbo.Polls', 'ModerationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Polls
        ADD ModerationStatus NVARCHAR(40) NOT NULL
            CONSTRAINT DF_Polls_ModerationStatus DEFAULT 'Published';
END;

IF COL_LENGTH('dbo.Polls', 'ModerationReason') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD ModerationReason NVARCHAR(1000) NULL;
END;

IF COL_LENGTH('dbo.Polls', 'ModeratedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD ModeratedByUserId BIGINT NULL;
END;

IF COL_LENGTH('dbo.Polls', 'ModeratedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD ModeratedAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.Polls', 'ReportCount') IS NULL
BEGIN
    ALTER TABLE dbo.Polls
        ADD ReportCount INT NOT NULL
            CONSTRAINT DF_Polls_ReportCount DEFAULT 0;
END;

IF COL_LENGTH('dbo.Polls', 'LastReportedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Polls ADD LastReportedAt DATETIME2 NULL;
END;

EXEC(N'
    UPDATE dbo.Polls
    SET ModerationStatus = ''Published''
    WHERE ModerationStatus IS NULL OR LTRIM(RTRIM(ModerationStatus)) = '''';
');

IF OBJECT_ID('dbo.PollReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PollReports (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        PollId BIGINT NOT NULL,
        ReportedByUserId BIGINT NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PollReports_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_PollReports_Polls FOREIGN KEY (PollId) REFERENCES dbo.Polls(Id),
        CONSTRAINT FK_PollReports_Users FOREIGN KEY (ReportedByUserId) REFERENCES dbo.Users(Id)
    );
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Polls_ModerationStatus' AND object_id = OBJECT_ID('dbo.Polls')
)
BEGIN
    EXEC(N'
        CREATE INDEX IX_Polls_ModerationStatus
        ON dbo.Polls (ModerationStatus, IsActive, CreatedAt DESC);
    ');
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PollReports_PollId' AND object_id = OBJECT_ID('dbo.PollReports')
)
BEGIN
    CREATE INDEX IX_PollReports_PollId ON dbo.PollReports (PollId, CreatedAt DESC);
END;
