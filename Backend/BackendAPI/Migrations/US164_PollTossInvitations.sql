CREATE TABLE PollTossInvitations (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    TokenHash BINARY(32) NOT NULL UNIQUE,
    PollId BIGINT NOT NULL REFERENCES Polls(Id),
    CreatorUserId BIGINT NOT NULL REFERENCES Users(Id),
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    ConsumedAt DATETIME2 NULL,
    RevokedAt DATETIME2 NULL
);
CREATE INDEX IX_PollTossInvitations_Expiry ON PollTossInvitations(ExpiresAt);
-- Deliberately excludes receiver/device/endpoint/location fields. Purge rows shortly after expiry.
