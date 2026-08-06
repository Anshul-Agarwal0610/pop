SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('UserRelationships','U') IS NULL CREATE TABLE UserRelationships(
 Id BIGINT IDENTITY PRIMARY KEY, RequesterUserId BIGINT NOT NULL REFERENCES Users(Id), AddresseeUserId BIGINT NOT NULL REFERENCES Users(Id),
 UserLowId BIGINT NOT NULL, UserHighId BIGINT NOT NULL, State VARCHAR(12) NOT NULL DEFAULT 'Pending', LastActorUserId BIGINT NOT NULL REFERENCES Users(Id),
 CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Relationship_State CHECK(State IN('Pending','Accepted','Declined','Removed')), CONSTRAINT CK_Relationship_Order CHECK(UserLowId<UserHighId),
 CONSTRAINT UQ_Relationship_Pair UNIQUE(UserLowId,UserHighId));

IF OBJECT_ID('UserBlocks','U') IS NULL CREATE TABLE UserBlocks(
 BlockerUserId BIGINT NOT NULL REFERENCES Users(Id), BlockedUserId BIGINT NOT NULL REFERENCES Users(Id), CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT PK_UserBlocks PRIMARY KEY(BlockerUserId,BlockedUserId), CONSTRAINT CK_UserBlocks_NoSelf CHECK(BlockerUserId<>BlockedUserId));

IF OBJECT_ID('Groups','U') IS NULL CREATE TABLE Groups(
 Id BIGINT IDENTITY PRIMARY KEY, OwnerUserId BIGINT NOT NULL REFERENCES Users(Id), Name NVARCHAR(80) NOT NULL, Visibility VARCHAR(10) NOT NULL DEFAULT 'Private',
 Status VARCHAR(12) NOT NULL DEFAULT 'Active', ModerationStatus VARCHAR(16) NOT NULL DEFAULT 'Approved', CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_Groups_Private CHECK(Visibility='Private'), CONSTRAINT CK_Groups_Status CHECK(Status IN('Active','Deleted')),
 CONSTRAINT CK_Groups_Moderation CHECK(ModerationStatus IN('Pending','Approved','Rejected')));

IF OBJECT_ID('GroupMemberships','U') IS NULL CREATE TABLE GroupMemberships(
 GroupId BIGINT NOT NULL REFERENCES Groups(Id), UserId BIGINT NOT NULL REFERENCES Users(Id), Role VARCHAR(10) NOT NULL, State VARCHAR(12) NOT NULL,
 JoinedAt DATETIME2 NULL, LeftAt DATETIME2 NULL, CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT PK_GroupMemberships PRIMARY KEY(GroupId,UserId),
 CONSTRAINT CK_GroupMember_Role CHECK(Role IN('Owner','Member')), CONSTRAINT CK_GroupMember_State CHECK(State IN('Invited','Active','Declined','Left','Removed')));

IF OBJECT_ID('GroupInvites','U') IS NULL CREATE TABLE GroupInvites(
 Id BIGINT IDENTITY PRIMARY KEY, GroupId BIGINT NOT NULL REFERENCES Groups(Id), InviterUserId BIGINT NOT NULL REFERENCES Users(Id), InviteeUserId BIGINT NOT NULL REFERENCES Users(Id),
 TokenHash CHAR(64) NOT NULL, State VARCHAR(12) NOT NULL DEFAULT 'Pending', CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), ExpiresAt DATETIME2 NOT NULL, RespondedAt DATETIME2 NULL,
 CONSTRAINT UQ_GroupInvite_Token UNIQUE(TokenHash), CONSTRAINT CK_GroupInvite_State CHECK(State IN('Pending','Accepted','Declined','Cancelled','Expired')));

IF OBJECT_ID('XpEvents','U') IS NULL CREATE TABLE XpEvents(
 Id BIGINT IDENTITY PRIMARY KEY, UserId BIGINT NOT NULL REFERENCES Users(Id), Amount INT NOT NULL, SourceType VARCHAR(32) NOT NULL, SourceId BIGINT NOT NULL,
 OccurredAt DATETIME2 NOT NULL, IsSociallyEligible BIT NOT NULL, CONSTRAINT UQ_XpEvents_Source UNIQUE(UserId,SourceType,SourceId), CONSTRAINT CK_XpEvents_Amount CHECK(Amount>=0));

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Relationship_UserState' AND object_id=OBJECT_ID('UserRelationships')) CREATE INDEX IX_Relationship_UserState ON UserRelationships(RequesterUserId,AddresseeUserId,State);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_GroupMembers_UserState' AND object_id=OBJECT_ID('GroupMemberships')) CREATE INDEX IX_GroupMembers_UserState ON GroupMemberships(UserId,State,GroupId) INCLUDE(JoinedAt,LeftAt,Role);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_GroupInvites_RateLimit' AND object_id=OBJECT_ID('GroupInvites')) CREATE INDEX IX_GroupInvites_RateLimit ON GroupInvites(InviterUserId,CreatedAt) INCLUDE(GroupId,InviteeUserId,State,ExpiresAt);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_XpEvents_Weekly' AND object_id=OBJECT_ID('XpEvents')) CREATE INDEX IX_XpEvents_Weekly ON XpEvents(UserId,IsSociallyEligible,OccurredAt) INCLUDE(Amount);

COMMIT;
