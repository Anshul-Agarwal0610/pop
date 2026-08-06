/* US162 canonical PoP Live trust schema. Secrets are stored only as SHA-256 hashes. */
CREATE TABLE LiveSessions(
 Id bigint IDENTITY PRIMARY KEY, PublicId uniqueidentifier NOT NULL DEFAULT NEWID() UNIQUE,
 HostUserId bigint NOT NULL REFERENCES Users(Id), Mode varchar(20) NOT NULL,
 State varchar(16) NOT NULL DEFAULT 'Lobby', JoinCodeHash char(64) NULL,
 StateVersion rowversion, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 ExpiresAt datetime2 NOT NULL, EndedAt datetime2 NULL,
 CONSTRAINT CK_LiveSessions_State CHECK(State IN('Lobby','Active','Ended','Expired'))
);
CREATE INDEX IX_LiveSessions_Expiry ON LiveSessions(State,ExpiresAt);

CREATE TABLE LiveSessionParticipants(
 Id bigint IDENTITY PRIMARY KEY, PublicId uniqueidentifier NOT NULL DEFAULT NEWID() UNIQUE,
 SessionId bigint NOT NULL REFERENCES LiveSessions(Id), UserId bigint NULL REFERENCES Users(Id),
 Pseudonym nvarchar(40) NOT NULL, ReconnectTokenHash char(64) NULL,
 State varchar(12) NOT NULL DEFAULT 'Joined', NotificationsEnabled bit NOT NULL DEFAULT 1,
 JoinedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), LeftAt datetime2 NULL,
 EligibleFrom datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_LiveParticipants_State CHECK(State IN('Joined','Left','Removed'))
);
CREATE UNIQUE INDEX UX_LiveParticipants_User ON LiveSessionParticipants(SessionId,UserId) WHERE UserId IS NOT NULL;
CREATE UNIQUE INDEX UX_LiveParticipants_Reconnect ON LiveSessionParticipants(SessionId,ReconnectTokenHash) WHERE ReconnectTokenHash IS NOT NULL;

CREATE TABLE LiveSessionInvitations(
 Id bigint IDENTITY PRIMARY KEY, SessionId bigint NOT NULL REFERENCES LiveSessions(Id),
 InviterUserId bigint NOT NULL REFERENCES Users(Id), IntendedUserId bigint NULL REFERENCES Users(Id),
 TokenHash char(64) NOT NULL UNIQUE, State varchar(12) NOT NULL DEFAULT 'Pending',
 CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), ExpiresAt datetime2 NOT NULL,
 AcceptedAt datetime2 NULL, ConsumedAt datetime2 NULL,
 CONSTRAINT CK_LiveInvites_State CHECK(State IN('Pending','Accepted','Declined','Cancelled','Expired'))
);
CREATE INDEX IX_LiveInvites_Target ON LiveSessionInvitations(IntendedUserId,State,ExpiresAt);

CREATE TABLE LiveSessionPolls(SessionId bigint NOT NULL REFERENCES LiveSessions(Id), PollId bigint NOT NULL REFERENCES Polls(Id), RoundNumber int NOT NULL, PRIMARY KEY(SessionId,PollId), UNIQUE(SessionId,RoundNumber));
CREATE TABLE LiveSessionResponses(
 Id bigint IDENTITY PRIMARY KEY, SessionId bigint NOT NULL REFERENCES LiveSessions(Id), RoundNumber int NOT NULL,
 ParticipantId bigint NOT NULL REFERENCES LiveSessionParticipants(Id), IdempotencyKey uniqueidentifier NOT NULL,
 ChoiceId bigint NULL, SubmittedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 UNIQUE(SessionId,RoundNumber,ParticipantId), UNIQUE(SessionId,ParticipantId,IdempotencyKey)
);

CREATE TABLE SafetyReports(
 Id bigint IDENTITY PRIMARY KEY, ReceiptId uniqueidentifier NOT NULL UNIQUE,
 ReporterUserId bigint NULL REFERENCES Users(Id), ReporterParticipantId uniqueidentifier NULL,
 TargetType varchar(16) NOT NULL, SessionId uniqueidentifier NOT NULL, TargetParticipantId uniqueidentifier NULL,
 PollId bigint NULL, ReasonCode varchar(30) NOT NULL, Comment nvarchar(500) NULL,
 Status varchar(16) NOT NULL DEFAULT 'Open', CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 ModeratorUserId bigint NULL REFERENCES Users(Id), ActionedAt datetime2 NULL,
 CONSTRAINT CK_SafetyReport_Target CHECK(TargetType IN('Session','Poll','Participant'))
);
CREATE INDEX IX_SafetyReports_Status ON SafetyReports(Status,CreatedAt);
CREATE TABLE SafetyReportAuditEvents(
 Id bigint IDENTITY PRIMARY KEY, ReportId bigint NOT NULL REFERENCES SafetyReports(Id), ActorUserId bigint NULL REFERENCES Users(Id),
 Action varchar(32) NOT NULL, PreviousStatus varchar(16) NULL, NewStatus varchar(16) NULL,
 Metadata nvarchar(500) NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE MultiplayerPrivacySettings(
 UserId bigint PRIMARY KEY REFERENCES Users(Id), DiscloseIdentity bit NOT NULL DEFAULT 0,
 DiscloseIndividualVote bit NOT NULL DEFAULT 0, ShareCoarseRegion bit NOT NULL DEFAULT 0,
 AllowPublicResultCard bit NOT NULL DEFAULT 0, UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
CREATE TABLE MultiplayerNotificationSettings(
 UserId bigint PRIMARY KEY REFERENCES Users(Id), Invitations bit NOT NULL DEFAULT 1, SessionActivity bit NOT NULL DEFAULT 1,
 Reminders bit NOT NULL DEFAULT 1, Results bit NOT NULL DEFAULT 1, QuietHoursStart time NULL, QuietHoursEnd time NULL,
 TimeZoneId nvarchar(80) NOT NULL DEFAULT 'UTC', AllowCritical bit NOT NULL DEFAULT 0,
 UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE MultiplayerRewardRiskDecisions(
 Id bigint IDENTITY PRIMARY KEY, SessionId uniqueidentifier NOT NULL, ParticipantId uniqueidentifier NOT NULL,
 Rule varchar(40) NOT NULL, SourceKey varchar(160) NOT NULL UNIQUE, Signals nvarchar(500) NOT NULL,
 Score int NOT NULL, PolicyVersion varchar(40) NOT NULL, Outcome varchar(12) NOT NULL,
 EvaluatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(), CONSTRAINT CK_RiskOutcome CHECK(Outcome IN('Allow','Cap','Hold','Suppress'))
);
CREATE TABLE MultiplayerCorrelationHashes(
 Id bigint IDENTITY PRIMARY KEY, ParticipantId uniqueidentifier NOT NULL, Kind varchar(10) NOT NULL,
 KeyVersion smallint NOT NULL, KeyedHash char(64) NOT NULL, CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
 ExpiresAt datetime2 NOT NULL, CONSTRAINT CK_CorrelationKind CHECK(Kind IN('Device','Network'))
);
CREATE INDEX IX_Correlation_Expiry ON MultiplayerCorrelationHashes(ExpiresAt);
CREATE INDEX IX_Correlation_Match ON MultiplayerCorrelationHashes(Kind,KeyVersion,KeyedHash,ExpiresAt);
