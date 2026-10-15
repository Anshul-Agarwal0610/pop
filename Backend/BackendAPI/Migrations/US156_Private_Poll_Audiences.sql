SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('PollAudienceUsers','U') IS NULL CREATE TABLE PollAudienceUsers(
 PollId BIGINT NOT NULL REFERENCES Polls(Id) ON DELETE CASCADE,
 UserId BIGINT NOT NULL REFERENCES Users(Id),
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_PollAudienceUsers PRIMARY KEY(PollId,UserId));

IF OBJECT_ID('PollAudienceGroups','U') IS NULL CREATE TABLE PollAudienceGroups(
 PollId BIGINT NOT NULL REFERENCES Polls(Id) ON DELETE CASCADE,
 GroupId BIGINT NOT NULL REFERENCES Groups(Id),
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_PollAudienceGroups PRIMARY KEY(PollId,GroupId));

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_PollAudienceUsers_User' AND object_id=OBJECT_ID('PollAudienceUsers'))
 CREATE INDEX IX_PollAudienceUsers_User ON PollAudienceUsers(UserId,PollId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_PollAudienceGroups_Group' AND object_id=OBJECT_ID('PollAudienceGroups'))
 CREATE INDEX IX_PollAudienceGroups_Group ON PollAudienceGroups(GroupId,PollId);

COMMIT;
