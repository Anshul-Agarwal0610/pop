IF OBJECT_ID('dbo.PollTossInvitations','U') IS NULL
BEGIN
 CREATE TABLE dbo.PollTossInvitations(
  Id uniqueidentifier NOT NULL PRIMARY KEY, PollId bigint NOT NULL, SenderUserId bigint NOT NULL, RecipientUserId bigint NULL,
  TokenHash char(64) NOT NULL, RoomCode varchar(8) NOT NULL, Status varchar(16) NOT NULL,
  CreatedAt datetime2 NOT NULL, ExpiresAt datetime2 NOT NULL, AcceptedAt datetime2 NULL, CancelledAt datetime2 NULL, StateVersion bigint NOT NULL,
  CONSTRAINT FK_PollToss_Poll FOREIGN KEY(PollId) REFERENCES Polls(Id), CONSTRAINT FK_PollToss_Sender FOREIGN KEY(SenderUserId) REFERENCES Users(Id),
  CONSTRAINT FK_PollToss_Recipient FOREIGN KEY(RecipientUserId) REFERENCES Users(Id), CONSTRAINT CK_PollToss_Status CHECK(Status IN('Pending','Accepted','Cancelled','Expired'))
 );
 CREATE UNIQUE INDEX UX_PollToss_TokenHash ON dbo.PollTossInvitations(TokenHash);
 CREATE UNIQUE INDEX UX_PollToss_RoomCode ON dbo.PollTossInvitations(RoomCode);
 CREATE INDEX IX_PollToss_Expiry ON dbo.PollTossInvitations(Status,ExpiresAt);
END
