-- Existing authenticated-user polls followed the old moderation policy.
-- Publish only ordinary custom polls; business campaigns and generated polls
-- have separate moderation/quality workflows.
UPDATE Polls
SET ModerationStatus = 'Published',
    ModerationReason = NULL
WHERE CreatedByUserId IS NOT NULL
  AND COALESCE(IsAIGenerated, 0) = 0
  AND COALESCE(IsWellness, 0) = 0
  AND ModerationStatus = 'PendingReview';
