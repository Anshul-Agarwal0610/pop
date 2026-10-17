-- RewardService writes the canonical reward to RewardEvents and mirrors it into
-- XpEvents so weekly/social leaderboards can include every eligible reward.
-- XpEvents originally allowed only the three legacy source types.
IF OBJECT_ID('dbo.XpEvents', 'U') IS NOT NULL
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
    IF OBJECT_ID('dbo.CK_XpEvents_SourceId', 'C') IS NOT NULL
        ALTER TABLE dbo.XpEvents DROP CONSTRAINT CK_XpEvents_SourceId;
    IF OBJECT_ID('dbo.CK_XpEvents_SourceType', 'C') IS NOT NULL
        ALTER TABLE dbo.XpEvents DROP CONSTRAINT CK_XpEvents_SourceType;
    ALTER TABLE dbo.XpEvents ADD CONSTRAINT CK_XpEvents_SourceType
        CHECK (SourceType IN ('Vote','Challenge','Achievement','RewardLedger'));
    ALTER TABLE dbo.XpEvents ADD CONSTRAINT CK_XpEvents_SourceId CHECK (
        (SourceType = 'Vote' AND PollId IS NOT NULL AND ChallengeId IS NULL AND BadgeId IS NULL) OR
        (SourceType = 'Challenge' AND ChallengeId IS NOT NULL AND PollId IS NULL AND BadgeId IS NULL) OR
        (SourceType = 'Achievement' AND BadgeId IS NOT NULL AND PollId IS NULL AND ChallengeId IS NULL) OR
        (SourceType = 'RewardLedger' AND PollId IS NULL AND ChallengeId IS NULL AND BadgeId IS NULL)
    );
    COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
