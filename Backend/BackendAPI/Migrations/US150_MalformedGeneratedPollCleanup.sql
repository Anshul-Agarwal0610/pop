-- US150: auditable, idempotent malformed generated-poll cleanup.
-- Apply only after US148_GeneratedPollQualityGate.sql and its publication gate are deployed.
CREATE TABLE GeneratedPollCleanupRecords (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PollId BIGINT NOT NULL,
    TrendingTopicId BIGINT NULL,
    DetectionVersion NVARCHAR(80) NOT NULL,
    ReasonCode NVARCHAR(1000) NOT NULL,
    GenerationSource NVARCHAR(100) NOT NULL,
    VoteCountAtCleanup BIGINT NOT NULL,
    Disposition NVARCHAR(40) NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    DetectedAt DATETIME2 NOT NULL,
    CleanedAt DATETIME2 NULL,
    LastAttemptAt DATETIME2 NULL,
    ReplacementPollId BIGINT NULL,
    AttemptCount INT NOT NULL CONSTRAINT DF_GPCRC_AttemptCount DEFAULT 0,
    LastError NVARCHAR(2000) NULL,
    RunId UNIQUEIDENTIFIER NULL,
    CONSTRAINT UX_GPCRC_Poll UNIQUE(PollId),
    CONSTRAINT CK_GPCRC_Disposition CHECK (Disposition IN ('DeactivateAndRegenerate','PreserveAndHide')),
    CONSTRAINT CK_GPCRC_Status CHECK (Status IN ('Identified','Deactivated','RegenerationQueued','Regenerating','Completed','Failed')),
    CONSTRAINT FK_GPCRC_Poll FOREIGN KEY(PollId) REFERENCES Polls(Id),
    CONSTRAINT FK_GPCRC_Topic FOREIGN KEY(TrendingTopicId) REFERENCES TrendingTopics(Id),
    CONSTRAINT FK_GPCRC_Replacement FOREIGN KEY(ReplacementPollId) REFERENCES Polls(Id)
);
GO
CREATE UNIQUE INDEX UX_GPCRC_Replacement ON GeneratedPollCleanupRecords(ReplacementPollId) WHERE ReplacementPollId IS NOT NULL;
CREATE INDEX IX_GPCRC_Status ON GeneratedPollCleanupRecords(Status, PollId);
GO
CREATE TABLE GeneratedPollRegenerationQueue (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CleanupRecordId BIGINT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    AvailableAt DATETIME2 NOT NULL,
    LeaseExpiresAt DATETIME2 NULL,
    AttemptCount INT NOT NULL CONSTRAINT DF_GPRQ_AttemptCount DEFAULT 0,
    LastError NVARCHAR(2000) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UX_GPRQ_Cleanup UNIQUE(CleanupRecordId),
    CONSTRAINT CK_GPRQ_Status CHECK(Status IN ('Queued','Processing','Completed','Failed')),
    CONSTRAINT FK_GPRQ_Cleanup FOREIGN KEY(CleanupRecordId) REFERENCES GeneratedPollCleanupRecords(Id)
);
GO
CREATE INDEX IX_GPRQ_Claim ON GeneratedPollRegenerationQueue(Status, AvailableAt, LeaseExpiresAt, Id);

