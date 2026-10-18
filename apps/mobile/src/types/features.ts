// Backend contracts mirrored from the web API client.
import type { ApiPoll } from './poll';

export interface ApiChallenge {
  challengeId: number
  title: string
  category: string | null
  requiredVotes: number
  rewardXp: number
  rewardBadge: string | null
  startAt: string
  endAt: string
  currentVotes: number
  isCompleted: boolean
  rewardGranted: boolean
  completedAt: string | null
  description: string
  challengeType: string
  recurrence: "Daily" | "Weekly" | "None"
  requirementType: string
  requirementText: string
  state: "Available" | "InProgress" | "Completed" | "Expired"
  eligiblePollsUrl: string
}

export interface ApiNotification {
  id: number
  userId: number
  type: "VoteMilestone" | "StreakMilestone" | "LevelUp" | "PollTrending" | "DailyReminder" | "ChallengeAvailable" | "StreakReminder" | "PollExpiring"
  title: string
  body: string
  pollId: number | null
  dedupKey: string | null
  isRead: boolean
  createdAt: string
}

export interface ApiNotificationPreference {
  type: ApiNotification["type"]
  isEnabled: boolean
}

export interface CreatePollPayload {
  question: string
  description: string
  category: string
  expiresAt: string          // ISO date string
  options: string[]
  sourceType?: string
  sourceUrl?: string
  thumbnailUrl?: string
  mediaType?: string
  creativeTemplate?: 'classic' | 'pulse' | 'spotlight'
  reactionId?: 'big_yes' | 'thinking' | 'no_way' | 'wow'
  reactionX?: number
  reactionY?: number
  reactionScale?: number
  resultVisibility?: 'Immediate' | 'AfterVote' | 'AfterClose'
  remixedFromPollId?: number
  isAIGenerated?: boolean
  isPrivate?: boolean
  audienceType?: 'Public' | 'Friend' | 'Group'
  audienceFriendUserId?: number
  audienceGroupId?: number
  isWellness?: boolean
}

export interface ApiWellnessResponse {
  id: number
  userId: number
  pollId: number
  optionId: number
  question: string
  optionText: string
  note: string | null
  createdAt: string
}

export interface ApiWellnessInsight {
  totalCheckIns: number
  lastCheckInAt: string | null
  mostCommonResponse: string | null
}

export interface ApiWellnessOverview {
  polls: ApiPoll[]
  history: ApiWellnessResponse[]
  insight: ApiWellnessInsight
}

export interface ApiGameMode {
  mode: "OpinionSprint"
  name: string
  category: string
  pollCount: number
  timeLimitSeconds: number | null
  completionXp: number
  rules: string
  available: boolean
}

export interface ApiCompletionSummary {
  votes: number
  voteXpEarned: number
  completionXpEarned: number
  totalXpEarned: number
  challengeProgress: ApiChallenge[]
  achievementsUnlocked: ApiUserBadge[]
}

export interface ApiGameSession {
  id: number
  mode: "OpinionSprint"
  category: string
  status: "Active" | "Completed" | "Expired" | "Abandoned"
  pollCount: number
  currentPosition: number
  votesCast: number
  remainingPolls: number
  timeLimitSeconds: number | null
  completionXp: number
  startedAt: string
  expiresAt: string | null
  completedAt: string | null
  serverNow: string
  currentPoll: ApiPoll | null
  summary: ApiCompletionSummary | null
}

export interface ApiGameVoteResult {
  session: ApiGameSession
  xpAwarded: number
  challenges: ApiChallenge[]
  achievementsUnlocked: ApiUserBadge[]
}

export interface ApiPredictionOpportunity { poll: ApiPoll; locksAt: string }
export interface ApiPredictionEntry {
  id: number; pollId: number; question: string; category: string
  predictedOptionId: number; predictedOptionText: string
    predictionType: 'Winner' | 'Split'; predictedPercentage: number | null
    actualPercentage: number | null; accuracyDifference: number | null
  winningOptionId: number | null; winningOptionText: string | null
  status: 'Pending' | 'Correct' | 'Incorrect' | 'NoContest'
  xpAwarded: number; createdAt: string; locksAt: string; settledAt: string | null; totalVotes: number
}

export interface ApiUserBadge {
  id: number
  userId: number
  badgeId: number
  code: string
  name: string
  description: string
  icon: string
  awardedAt: string
  rewardXp: number
  rewardTitle: string | null
}

export type AchievementStatus = "earned" | "in-progress" | "locked"

export interface ApiAchievement {
  badgeId: number; userBadgeId: number | null; code: string; name: string; description: string
  icon: string; category: "Voting" | "Streak" | "Challenge" | "Exploration"; status: AchievementStatus
  requirement: string | null; rewardXp: number; rewardTitle: string | null; awardedAt: string | null
  currentProgress: number | null; targetProgress: number | null; progressPercent: number | null; isSecret: boolean
}

export interface ApiAchievementCollection {
  achievements: ApiAchievement[]; selectedTitle: string | null; selectedTitleBadgeId: number | null
  earnedCount: number; totalCount: number
}

export interface ApiLeaderboardRow {
  rank: number
  id: number
  username: string
  displayName: string
  avatarUrl?: string | null
  periodXp: number
  lifetimeXp: number
  level: number
  badges: ApiUserBadge[]
}

export interface ApiLeaderboardResponse {
  rows: ApiLeaderboardRow[]
  currentUser: ApiLeaderboardRow | null
  period: "Weekly" | "AllTime"
  periodStartUtc: string | null
  periodEndUtc: string | null
  nextResetAtUtc: string | null
  limit: number
  offset: number
  hasMore: boolean
}

export interface ApiVoteHistoryItem {
  pollId: number
  question: string
  category: string
  votedOptionText: string
  totalVotes: number
  canViewResults?: boolean
  votedAt: string
}

export interface ApiStreakStatus {
  streak: number
  longestStreak: number
  todayComplete: boolean
  lastVoteDate: string | null
  recoveryEligible: boolean
  nextRecoveryAt: string | null
  timeZone: "UTC"
  dayBoundary: string
  milestones: number[]
}

export interface SocialUser { id: number; username: string; displayName: string; avatarUrl?: string | null }

export interface Paged<T> { items: T[]; nextCursor?: string | null }

export interface FriendConnection { id: number; user: SocialUser; state: "Pending" | "Accepted" | "Declined" | "Removed"; incoming: boolean; updatedAt: string }

export interface SocialGroup { id: number; name: string; ownerUserId: number; moderationStatus: string; memberCount: number; role: "Owner" | "Member"; createdAt: string }

export interface WeeklyEntry { rank: number; user: SocialUser; xp: number; activityCount: number }

export interface WeeklyLeaderboard { weekStartUtc: string; weekEndUtc: string; items: WeeklyEntry[]; nextCursor?: string | null }
