-- Stable binary identity is additive: custom and legacy poll options remain NULL.
ALTER TABLE PollOptions ADD Side NVARCHAR(16) NULL;
GO
ALTER TABLE PollOptions ADD CONSTRAINT CK_PollOptions_Side
    CHECK (Side IS NULL OR Side IN ('Up', 'Against'));
GO
CREATE UNIQUE INDEX UX_PollOptions_PollId_Side
    ON PollOptions(PollId, Side) WHERE Side IS NOT NULL;
GO

-- SQL Server requires the referenced column order to be unique for a composite FK.
ALTER TABLE PollOptions ADD CONSTRAINT UQ_PollOptions_PollId_Id UNIQUE (PollId, Id);
GO
ALTER TABLE Votes ADD CONSTRAINT FK_Votes_PollOptions
    FOREIGN KEY (PollId, OptionId) REFERENCES PollOptions(PollId, Id);
GO
