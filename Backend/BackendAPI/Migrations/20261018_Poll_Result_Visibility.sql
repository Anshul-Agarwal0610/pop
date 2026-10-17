-- Apply after 20261017_Poll_Creative_Style.sql and before deploying the Result Reveal API.
IF COL_LENGTH('dbo.Polls', 'ResultVisibility') IS NULL
    ALTER TABLE dbo.Polls ADD ResultVisibility nvarchar(24) NOT NULL
        CONSTRAINT DF_Polls_ResultVisibility DEFAULT 'AfterVote';
GO
