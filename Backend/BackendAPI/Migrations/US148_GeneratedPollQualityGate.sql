-- US148: auditable, provider-independent generated-poll quality decisions.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.GeneratedPollQualityDecisions', 'U') IS NULL
BEGIN
CREATE TABLE GeneratedPollQualityDecisions (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PollId BIGINT NULL,
    TrendingTopicId BIGINT NULL,
    Disposition NVARCHAR(20) NOT NULL,
    OverallScore FLOAT NOT NULL,
    GroundingScore FLOAT NOT NULL, NeutralityScore FLOAT NOT NULL, ClarityScore FLOAT NOT NULL,
    AnswerabilityScore FLOAT NOT NULL, BalancedSidesScore FLOAT NOT NULL,
    DuplicationScore FLOAT NOT NULL, SafetyScore FLOAT NOT NULL,
    IsSensitive BIT NOT NULL,
    SensitivityPolicyCode NVARCHAR(100) NULL,
    ReasonCodes NVARCHAR(1000) NOT NULL,
    GenerationProvider NVARCHAR(50) NULL,
    ProviderConfidence FLOAT NULL,
    GenerationPromptVersion NVARCHAR(80) NOT NULL,
    GenerationSchemaVersion NVARCHAR(80) NOT NULL,
    EvaluatorPromptVersion NVARCHAR(80) NOT NULL,
    EvaluatorSchemaVersion NVARCHAR(80) NOT NULL,
    RulesVersion NVARCHAR(80) NOT NULL,
    DuplicatePollId BIGINT NULL,
    DuplicateSimilarity FLOAT NULL,
    DuplicateMatchType NVARCHAR(20) NULL,
    ExactFingerprint CHAR(64) NULL,
    EvaluatedAt DATETIME2 NOT NULL CONSTRAINT DF_GPQD_EvaluatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_GPQD_Disposition CHECK (Disposition IN ('Accepted','NeedsReview','Rejected')),
    CONSTRAINT FK_GPQD_Poll FOREIGN KEY (PollId) REFERENCES Polls(Id),
    CONSTRAINT FK_GPQD_Topic FOREIGN KEY (TrendingTopicId) REFERENCES TrendingTopics(Id),
    CONSTRAINT FK_GPQD_DuplicatePoll FOREIGN KEY (DuplicatePollId) REFERENCES Polls(Id)
);
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GPQD_PollId' AND object_id = OBJECT_ID('dbo.GeneratedPollQualityDecisions'))
CREATE UNIQUE INDEX UX_GPQD_PollId ON GeneratedPollQualityDecisions(PollId) WHERE PollId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GPQD_ExactFingerprint' AND object_id = OBJECT_ID('dbo.GeneratedPollQualityDecisions'))
CREATE UNIQUE INDEX UX_GPQD_ExactFingerprint ON GeneratedPollQualityDecisions(ExactFingerprint)
WHERE ExactFingerprint IS NOT NULL AND PollId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GPQD_Topic' AND object_id = OBJECT_ID('dbo.GeneratedPollQualityDecisions'))
CREATE INDEX IX_GPQD_Topic ON GeneratedPollQualityDecisions(TrendingTopicId, EvaluatedAt DESC);
