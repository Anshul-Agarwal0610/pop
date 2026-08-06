-- Supports the weekly public-vote leaderboard used by the Game Hub.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Votes_CreatedAt_UserId'
      AND object_id = OBJECT_ID('dbo.Votes')
)
BEGIN
    CREATE INDEX IX_Votes_CreatedAt_UserId
    ON dbo.Votes (CreatedAt, UserId)
    INCLUDE (PollId);
END;
