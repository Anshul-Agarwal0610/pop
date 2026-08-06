-- Idempotent achievement collection upgrade (issue #120).
IF COL_LENGTH('dbo.AchievementBadges','Category') IS NULL ALTER TABLE dbo.AchievementBadges ADD Category NVARCHAR(30) NOT NULL CONSTRAINT DF_AchievementBadges_Category DEFAULT 'Voting';
IF COL_LENGTH('dbo.AchievementBadges','RequirementText') IS NULL ALTER TABLE dbo.AchievementBadges ADD RequirementText NVARCHAR(300) NOT NULL CONSTRAINT DF_AchievementBadges_Requirement DEFAULT '';
IF COL_LENGTH('dbo.AchievementBadges','IsSecret') IS NULL ALTER TABLE dbo.AchievementBadges ADD IsSecret BIT NOT NULL CONSTRAINT DF_AchievementBadges_Secret DEFAULT 0;
IF COL_LENGTH('dbo.AchievementBadges','IsPublic') IS NULL ALTER TABLE dbo.AchievementBadges ADD IsPublic BIT NOT NULL CONSTRAINT DF_AchievementBadges_Public DEFAULT 1;
IF COL_LENGTH('dbo.AchievementBadges','ProgressVisible') IS NULL ALTER TABLE dbo.AchievementBadges ADD ProgressVisible BIT NOT NULL CONSTRAINT DF_AchievementBadges_Progress DEFAULT 1;
IF COL_LENGTH('dbo.AchievementBadges','RewardTitle') IS NULL ALTER TABLE dbo.AchievementBadges ADD RewardTitle NVARCHAR(80) NULL;
IF COL_LENGTH('dbo.AchievementBadges','SortOrder') IS NULL ALTER TABLE dbo.AchievementBadges ADD SortOrder INT NOT NULL CONSTRAINT DF_AchievementBadges_Sort DEFAULT 0;
IF COL_LENGTH('dbo.AchievementBadges','IsActive') IS NULL ALTER TABLE dbo.AchievementBadges ADD IsActive BIT NOT NULL CONSTRAINT DF_AchievementBadges_Active DEFAULT 1;
IF COL_LENGTH('dbo.UserBadges','CelebrationClaimedAt') IS NULL ALTER TABLE dbo.UserBadges ADD CelebrationClaimedAt DATETIME2 NULL;
IF COL_LENGTH('dbo.Users','SelectedTitleBadgeId') IS NULL ALTER TABLE dbo.Users ADD SelectedTitleBadgeId BIGINT NULL;
GO

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Users_SelectedTitleBadge') ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_SelectedTitleBadge FOREIGN KEY(SelectedTitleBadgeId) REFERENCES dbo.AchievementBadges(Id);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_AchievementBadges_Category') ALTER TABLE dbo.AchievementBadges ADD CONSTRAINT CK_AchievementBadges_Category CHECK(Category IN ('Voting','Streak','Challenge','Exploration'));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_AchievementBadges_Catalog' AND object_id=OBJECT_ID('dbo.AchievementBadges')) CREATE INDEX IX_AchievementBadges_Catalog ON dbo.AchievementBadges(IsActive,Category,SortOrder);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_UserBadges_Celebrations' AND object_id=OBJECT_ID('dbo.UserBadges')) CREATE INDEX IX_UserBadges_Celebrations ON dbo.UserBadges(UserId,CelebrationClaimedAt) INCLUDE(BadgeId,AwardedAt);

UPDATE dbo.AchievementBadges SET
 Category=CASE WHEN RuleType='Streak' THEN 'Streak' WHEN RuleType='ChallengeCompletion' THEN 'Challenge' ELSE 'Voting' END,
 RequirementText=Description, ProgressVisible=1, IsPublic=1, IsActive=1;
UPDATE dbo.AchievementBadges SET RewardTitle='Pulse Pioneer' WHERE Code='first_vote';
UPDATE dbo.AchievementBadges SET RewardTitle='Conversation Starter' WHERE Code='creator_1';

MERGE dbo.AchievementBadges WITH (HOLDLOCK) AS target
USING (VALUES ('explorer_3','Curious Explorer','Explore three different poll categories.','Compass','DistinctCategoriesVoted',3,75,'Exploration','Vote in 3 different poll categories.',0,1,1,'Trailblazer',10,1),
              ('secret_explorer','Hidden Horizon','You found a hidden corner of the community.','Sparkles','DistinctCategoriesVoted',8,200,'Exploration','',1,1,0,'Pathfinder',20,1))
AS source(Code,Name,Description,Icon,RuleType,Threshold,RewardXp,Category,RequirementText,IsSecret,IsPublic,ProgressVisible,RewardTitle,SortOrder,IsActive)
ON target.Code=source.Code
WHEN MATCHED THEN UPDATE SET Name=source.Name,Description=source.Description,Icon=source.Icon,RuleType=source.RuleType,Threshold=source.Threshold,RewardXp=source.RewardXp,Category=source.Category,RequirementText=source.RequirementText,IsSecret=source.IsSecret,IsPublic=source.IsPublic,ProgressVisible=source.ProgressVisible,RewardTitle=source.RewardTitle,SortOrder=source.SortOrder,IsActive=source.IsActive
WHEN NOT MATCHED THEN INSERT(Code,Name,Description,Icon,RuleType,Threshold,RewardXp,Category,RequirementText,IsSecret,IsPublic,ProgressVisible,RewardTitle,SortOrder,IsActive) VALUES(source.Code,source.Name,source.Description,source.Icon,source.RuleType,source.Threshold,source.RewardXp,source.Category,source.RequirementText,source.IsSecret,source.IsPublic,source.ProgressVisible,source.RewardTitle,source.SortOrder,source.IsActive);
