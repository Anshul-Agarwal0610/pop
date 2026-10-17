-- Concrete daily lifestyle check-ins. Responses remain private wellness data.
IF NOT EXISTS (SELECT 1 FROM dbo.Polls WHERE PollMode='Wellness' AND Question='Are you feeling healthy today?')
BEGIN
    DECLARE @HealthyPollId BIGINT;
    INSERT INTO dbo.Polls (Question,Description,Category,ExpiresAt,IsActive,IsTrending,CreatedByUserId,CreatedAt,TotalVotes,SourceType,SourceUrl,ThumbnailUrl,IsAIGenerated,IsPrivate,IsWellness,PollMode,ModerationStatus,ReportCount)
    VALUES ('Are you feeling healthy today?','A private daily wellbeing check-in.','Health',DATEADD(year,10,GETUTCDATE()),1,0,NULL,GETUTCDATE(),0,'manual',NULL,NULL,0,1,1,'Wellness','Published',0);
    SET @HealthyPollId=CAST(SCOPE_IDENTITY() AS BIGINT);
    INSERT INTO dbo.PollOptions(PollId,Text,VoteCount,VotePercentage) VALUES (@HealthyPollId,'Yes',0,0),(@HealthyPollId,'Somewhat',0,0),(@HealthyPollId,'Not today',0,0);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Polls WHERE PollMode='Wellness' AND Question='Did you have breakfast today?')
BEGIN
    DECLARE @BreakfastPollId BIGINT;
    INSERT INTO dbo.Polls (Question,Description,Category,ExpiresAt,IsActive,IsTrending,CreatedByUserId,CreatedAt,TotalVotes,SourceType,SourceUrl,ThumbnailUrl,IsAIGenerated,IsPrivate,IsWellness,PollMode,ModerationStatus,ReportCount)
    VALUES ('Did you have breakfast today?','A private daily nutrition check-in.','Health',DATEADD(year,10,GETUTCDATE()),1,0,NULL,GETUTCDATE(),0,'manual',NULL,NULL,0,1,1,'Wellness','Published',0);
    SET @BreakfastPollId=CAST(SCOPE_IDENTITY() AS BIGINT);
    INSERT INTO dbo.PollOptions(PollId,Text,VoteCount,VotePercentage) VALUES (@BreakfastPollId,'Yes',0,0),(@BreakfastPollId,'Something small',0,0),(@BreakfastPollId,'Not yet',0,0);
END;
