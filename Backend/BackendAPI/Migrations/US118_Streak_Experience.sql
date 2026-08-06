-- US-118: longest streak, auditable limited recovery, and notification idempotency.
IF COL_LENGTH('Users', 'LongestStreak') IS NULL
    ALTER TABLE Users ADD LongestStreak INT NOT NULL CONSTRAINT DF_Users_LongestStreak DEFAULT 0;
GO
UPDATE Users SET LongestStreak = Streak WHERE LongestStreak < Streak;
GO
IF OBJECT_ID('StreakRecoveries', 'U') IS NULL
BEGIN
    CREATE TABLE StreakRecoveries (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserId BIGINT NOT NULL,
        MissedUtcDate DATE NOT NULL,
        AppliedAt DATETIME2 NOT NULL,
        PollId BIGINT NOT NULL,
        CONSTRAINT FK_StreakRecoveries_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
        CONSTRAINT FK_StreakRecoveries_Polls FOREIGN KEY (PollId) REFERENCES Polls(Id),
        CONSTRAINT UQ_StreakRecoveries_UserMissedDate UNIQUE (UserId, MissedUtcDate)
    );
    CREATE INDEX IX_StreakRecoveries_UserAppliedAt ON StreakRecoveries(UserId, AppliedAt DESC);
END
GO
