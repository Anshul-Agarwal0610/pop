CREATE TABLE MultiplayerResultCards (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    PublicToken VARCHAR(64) NOT NULL,
    SessionId BIGINT NOT NULL,
    OwnerUserId BIGINT NOT NULL,
    SchemaVersion INT NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    PayloadHash CHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    CONSTRAINT FK_MultiplayerResultCards_User FOREIGN KEY (OwnerUserId) REFERENCES Users(Id),
    CONSTRAINT CK_MultiplayerResultCards_PayloadJson CHECK (ISJSON(PayloadJson)=1),
    CONSTRAINT UQ_MultiplayerResultCards_Token UNIQUE (PublicToken),
    CONSTRAINT UQ_MultiplayerResultCards_OwnerVersion UNIQUE (SessionId,OwnerUserId,SchemaVersion)
);
CREATE INDEX IX_MultiplayerResultCards_Collection ON MultiplayerResultCards(OwnerUserId,CreatedAt DESC);
