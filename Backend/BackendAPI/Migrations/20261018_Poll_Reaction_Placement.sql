IF COL_LENGTH('dbo.Polls', 'ReactionX') IS NULL
    ALTER TABLE dbo.Polls ADD ReactionX decimal(5,3) NULL;

IF COL_LENGTH('dbo.Polls', 'ReactionY') IS NULL
    ALTER TABLE dbo.Polls ADD ReactionY decimal(5,3) NULL;

IF COL_LENGTH('dbo.Polls', 'ReactionScale') IS NULL
    ALTER TABLE dbo.Polls ADD ReactionScale decimal(5,3) NULL;
