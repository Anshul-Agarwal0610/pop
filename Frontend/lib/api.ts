/**
 * Centralized API client for the Pollify backend (US-12 + US-18).
 * Base URL is read from NEXT_PUBLIC_API_URL env variable.
 * JWT token is automatically attached from localStorage when present.
 */

import { getToken } from "@/lib/auth"
import { API_BASE_URL } from "@/lib/config"
import type { PollSide } from "@/lib/poll-sides"

// ── Raw backend shapes ────────────────────────────────────────────────────────

export interface ApiPollOption {
  id: number
  pollId: number
  text: string
  side?: PollSide | null
  voteCount: number
  votePercentage: number
}

export interface ApiPoll {
  id: number
  question: string
  description: string
  category: string
  isTrending: boolean
  isActive: boolean
  expiresAt: string          // ISO date string
  createdAt: string          // ISO date string
  totalVotes: number
  createdByUserId: number | null
  createdByUsername: string | null
  createdByDisplayName: string | null
  isSponsored: boolean
  businessId: number | null
  campaignId: number | null
  sponsorName: string | null
  campaignName: string | null
  isPrivate: boolean
  isWellness: boolean
  pollMode: "Public" | "Wellness"
  moderationStatus: "Draft" | "PendingReview" | "Published" | "Rejected" | "Flagged"
  moderationReason: string | null
  moderatedByUserId: number | null
  moderatedAt: string | null
  reportCount: number
  lastReportedAt: string | null
  sourceType: string | null
  sourceUrl: string | null
  thumbnailUrl: string | null
  isAIGenerated: boolean
  options: ApiPollOption[]
  // US-16: per-user vote state (present when authenticated)
  hasVoted: boolean
  userVotedOptionId: number | null
}

export interface CastVoteRequest {
  pollId: number
  optionId: number
  useStreakRecovery?: boolean
}

export interface ApiProgression {
  totalXp: number
  level: number
  currentLevelXp: number
  nextLevelXp: number
  xpIntoLevel: number
  xpRequiredForNextLevel: number
  progressPercent: number
}

export interface ApiRewardEvent {
  type: "Vote" | "Challenge" | "Achievement"
  sourceId: string
  awardedXp: number
  label: string | null
}

export interface ApiVoteReward {
  xp: number
  level: number
  streak: number
  longestStreak: number
  totalVotes: number
  xpAwarded: number
  streakAdvanced: boolean
  todayComplete: boolean
  recoveryEligible: boolean
  recoveryUsed: boolean
  nextRecoveryAt: string | null
  milestoneReached: number | null
  lastVoteDate: string | null
  awardedBadges: ApiUserBadge[]
  awardedXp: number
  progression: ApiProgression
  previousLevel: number
  leveledUp: boolean
  levelsGained: number
  events: ApiRewardEvent[]
}

export interface ApiCastVoteResponse {
  poll: ApiPoll
  reward: ApiVoteReward
  challenges: ApiChallenge[]
}

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

export interface ApiNotificationsResponse {
  notifications: ApiNotification[]
  unreadCount: number
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
  isAIGenerated?: boolean
  isPrivate?: boolean
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

export interface ApiBusinessAccount {
  id: number
  ownerUserId: number
  name: string
  websiteUrl: string | null
  status: string
  createdAt: string
}

export interface ApiBusinessCampaign {
  id: number
  businessId: number
  businessName: string
  name: string
  objective: string
  startsAt: string | null
  endsAt: string | null
  status: string
  createdAt: string
  impressions: number
  votes: number
  completions: number
  completionRate: number
}

export interface ApiCampaignPollMetric {
  campaignId: number
  pollId: number
  question: string
  moderationStatus: string
  createdAt: string
  impressions: number
  votes: number
  completions: number
  completionRate: number
  updatedAt: string
}

export interface ApiCampaignOptionBreakdown {
  pollId: number
  optionId: number
  optionText: string
  voteCount: number
  votePercentage: number
}

export interface ApiCampaignDailyMetric {
  date: string
  votes: number
}

export interface ApiCampaignAnalytics {
  campaign: ApiBusinessCampaign
  polls: ApiCampaignPollMetric[]
  optionBreakdown: ApiCampaignOptionBreakdown[]
  dailyVotes: ApiCampaignDailyMetric[]
}

export interface CreateBusinessAccountPayload {
  name: string
  websiteUrl?: string
}

export interface CreateBusinessCampaignPayload {
  name: string
  objective: string
  startsAt?: string
  endsAt?: string
  status?: string
}

export interface CreateSponsoredPollPayload extends CreatePollPayload {
  campaignId?: number
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function authHeaders(): Record<string, string> {
  const token = getToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

export class ApiError extends Error {
  constructor(public status: number, message: string, public code?: string) { super(message); this.name = "ApiError" }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...authHeaders(),          // attach JWT when logged in (US-18)
      ...init?.headers,
    },
    ...init,
  })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    let code: string | undefined
    let message = text
    try { const body = JSON.parse(text); code = body.code; message = body.message ?? text } catch {}
    throw new ApiError(res.status, message, code)
  }

  if (res.status === 204) return undefined as T
  return res.json() as Promise<T>
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

export const gameSessionsApi = {
  modes: () => request<ApiGameMode[]>("/api/game-modes"),
  active: async () => {
    const res = await fetch(`${API_BASE_URL}/api/game-sessions/active`, { headers: { ...authHeaders() } })
    if (res.status === 204) return null
    if (!res.ok) throw new ApiError(res.status, await res.text())
    return res.json() as Promise<ApiGameSession>
  },
  start: (category = "General", timed = true) => request<ApiGameSession>("/api/game-sessions", { method: "POST", body: JSON.stringify({ mode: "OpinionSprint", category, timed }) }),
  get: (id: number | string) => request<ApiGameSession>(`/api/game-sessions/${id}`),
  vote: (id: number, position: number, pollId: number, optionId: number) => request<ApiGameVoteResult>(`/api/game-sessions/${id}/votes`, { method: "POST", body: JSON.stringify({ position, pollId, optionId }) }),
  complete: (id: number) => request<ApiGameSession>(`/api/game-sessions/${id}/complete`, { method: "POST" }),
}

// ── Poll endpoints ────────────────────────────────────────────────────────────

function categoryQuery(category?: string) {
  return category ? `category=${encodeURIComponent(category)}` : ""
}

export interface ModeratePollPayload {
  status: ApiPoll["moderationStatus"]
  reason?: string
}

export const pollsApi = {
  /** Fetch trending polls for the feed. Includes hasVoted when authenticated. */
  getTrending: (count = 20, category?: string) => {
    const query = new URLSearchParams({ count: String(count) })
    if (category) query.set("category", category)
    return request<ApiPoll[]>(`/api/polls/trending?${query.toString()}`)
  },

  /** Fetch personalized polls. Anonymous users receive the default trending feed. */
  getPersonalized: (count = 20, category?: string) => {
    const query = new URLSearchParams({ count: String(count) })
    if (category) query.set("category", category)
    return request<ApiPoll[]>(`/api/polls/personalized?${query.toString()}`)
  },

  /** Fetch all polls. Includes hasVoted when authenticated. */
  getAll: (category?: string) => {
    const query = categoryQuery(category)
    return request<ApiPoll[]>(`/api/polls${query ? `?${query}` : ""}`)
  },

  /** Fetch one poll. Includes hasVoted when authenticated. */
  getById: (id: number | string) => request<ApiPoll>(`/api/polls/${id}`),

  /** Search active polls by question. Includes hasVoted when authenticated. */
  search: (q: string, category?: string) => {
    const query = new URLSearchParams({ q })
    if (category) query.set("category", category)
    return request<ApiPoll[]>(`/api/polls/search?${query.toString()}`)
  },

  /** Create a new poll. Returns the created poll. */
  create: (payload: CreatePollPayload) =>
    request<ApiPoll>("/api/polls", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  report: (id: number | string, reason: string) =>
    request<{ message: string }>(`/api/polls/${id}/report`, {
      method: "POST",
      body: JSON.stringify({ reason }),
    }),

  getModerationQueue: (status?: ApiPoll["moderationStatus"], count = 50) => {
    const query = new URLSearchParams({ count: String(count) })
    if (status) query.set("status", status)
    return request<ApiPoll[]>(`/api/polls/moderation?${query.toString()}`)
  },

  moderate: (id: number | string, payload: ModeratePollPayload) =>
    request<ApiPoll>(`/api/polls/${id}/moderation`, {
      method: "PATCH",
      body: JSON.stringify(payload),
    }),

  recordImpression: (id: number | string) =>
    request<void>(`/api/polls/${id}/impression`, { method: "POST" }),
}

export const businessApi = {
  getAccounts: () => request<ApiBusinessAccount[]>("/api/business/accounts"),

  createAccount: (payload: CreateBusinessAccountPayload) =>
    request<ApiBusinessAccount>("/api/business/accounts", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  getCampaigns: () => request<ApiBusinessCampaign[]>("/api/business/campaigns"),

  getCampaignAnalytics: (campaignId: number) =>
    request<ApiCampaignAnalytics>(`/api/business/campaigns/${campaignId}/analytics`),

  exportCampaignCsv: async (campaignId: number) => {
    const res = await fetch(`${API_BASE_URL}/api/business/campaigns/${campaignId}/export.csv`, {
      headers: authHeaders(),
    })

    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText)
      throw new Error(`API ${res.status}: ${text}`)
    }

    return res.blob()
  },

  createCampaign: (businessId: number, payload: CreateBusinessCampaignPayload) =>
    request<ApiBusinessCampaign>(`/api/business/accounts/${businessId}/campaigns`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  createSponsoredPoll: (campaignId: number, payload: CreateSponsoredPollPayload) =>
    request<ApiPoll>(`/api/business/campaigns/${campaignId}/polls`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),
}

// ── Vote endpoints ────────────────────────────────────────────────────────────

export const votesApi = {
  /** Cast a vote — requires authentication. Returns the updated poll. */
  cast: (req: CastVoteRequest) =>
    request<ApiCastVoteResponse>("/api/votes", {
      method: "POST",
      body: JSON.stringify(req),
    }),
}

export const notificationsApi = {
  getAll: async (): Promise<ApiNotificationsResponse> => {
    const res = await fetch(`${API_BASE_URL}/api/notifications`, {
      headers: {
        "Content-Type": "application/json",
        ...authHeaders(),
      },
    })

    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText)
      throw new Error(`API ${res.status}: ${text}`)
    }

    return {
      notifications: (await res.json()) as ApiNotification[],
      unreadCount: Number(res.headers.get("X-Unread-Count") ?? 0),
    }
  },

  markAllRead: () =>
    request<void>("/api/notifications/read-all", { method: "POST" }),

  markRead: (id: number) =>
    request<void>(`/api/notifications/${id}/read`, { method: "PATCH" }),

  getPreferences: () =>
    request<ApiNotificationPreference[]>("/api/notifications/preferences"),

  updatePreferences: (disabledTypes: ApiNotification["type"][]) =>
    request<ApiNotificationPreference[]>("/api/notifications/preferences", {
      method: "PUT",
      body: JSON.stringify({ disabledTypes }),
    }),
}

export const wellnessApi = {
  getOverview: () => request<ApiWellnessOverview>("/api/wellness/overview"),

  createResponse: (pollId: number, optionId: number, note?: string) =>
    request<ApiWellnessResponse>("/api/wellness/responses", {
      method: "POST",
      body: JSON.stringify({ pollId, optionId, note }),
    }),

  deleteResponses: () =>
    request<void>("/api/wellness/responses", { method: "DELETE" }),

  exportCsv: async () => {
    const res = await fetch(`${API_BASE_URL}/api/wellness/export.csv`, {
      headers: authHeaders(),
    })

    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText)
      throw new Error(`API ${res.status}: ${text}`)
    }

    return res.blob()
  },
}

// ── User endpoints ────────────────────────────────────────────────────────────

export const challengesApi = {
  getActive: () => request<ApiChallenge[]>("/api/challenges/active"),
  getAll: (state: "active" | "completed" | "expired" | "all" = "all") =>
    request<ApiChallenge[]>(`/api/challenges?state=${state}`),
}

export interface ApiUser {
  id: number
  username: string
  displayName: string
  email?: string
  avatarUrl?: string
  authProvider: string
  xp: number
  streak: number
  longestStreak: number
  totalVotes: number
  pollsCreated: number
  lastVoteDate?: string | null
  createdAt: string
  level: number
  progression: ApiProgression
  badges: ApiUserBadge[]
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

export const achievementsApi = {
  getMine: () => request<ApiAchievementCollection>("/api/achievements/me"),
  getMyOverview: () => request<ApiAchievementOverview>("/api/achievements/me/overview"),
  claimCelebrations: () => request<ApiUserBadge[]>("/api/achievements/me/celebrations/claim", { method: "POST" }),
  selectTitle: (badgeId: number) => request<void>("/api/achievements/me/title", { method: "PUT", body: JSON.stringify({ badgeId }) }),
  clearTitle: () => request<void>("/api/achievements/me/title", { method: "DELETE" }),
}

export interface ApiProgression {
  xp: number; level: number; currentLevelStartXp: number; nextLevelXp: number
  xpIntoLevel: number; xpRequiredForLevel: number; progressPercent: number
  streak: number; todayActivityComplete: boolean; lastVoteDate: string | null
}

export interface ApiAchievementProgress {
  badgeId: number; code: string; name: string; description: string; icon: string
  ruleType: string; currentValue: number; threshold: number; progressPercent: number; rewardXp: number
}

export interface ApiAchievementOverview {
  recentlyEarned: ApiUserBadge[]; nextAchievable: ApiAchievementProgress[]; allEarned: boolean
}

export interface ApiWeeklyLeaderboardEntry {
  userId: number; username: string; displayName: string; rank: number; score: number; scoreUnit: string
}

export interface ApiWeeklyLeaderboardResponse {
  weekStart: string; weekEnd: string; entries: ApiWeeklyLeaderboardEntry[]
  currentUser: ApiWeeklyLeaderboardEntry | null; scoreUnit: string
}

export type LeaderboardPeriod = "weekly" | "allTime"

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
  votedAt: string
}

export interface ApiCategoryPreference {
  category: string
  isExplicit: boolean
  voteCount: number
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

export const usersApi = {
  getAnalyticsPrivacy: () => request<{ consent: "unknown" | "granted" | "denied"; updatedAt: string | null }>("/api/users/me/privacy"),
  updateAnalyticsPrivacy: (consent: "unknown" | "granted" | "denied") => request<{ consent: string; updatedAt: string | null }>("/api/users/me/privacy", { method: "PUT", body: JSON.stringify({ consent }) }),
  /** Leaderboard — top users by XP. */
  getLeaderboard: (count = 20) =>
    request<ApiUser[]>(`/api/users/leaderboard?count=${count}`),

  getRankings: (period: LeaderboardPeriod, limit = 20, offset = 0) =>
    request<ApiLeaderboardResponse>(
      `/api/users/leaderboard/rankings?period=${period}&limit=${limit}&offset=${offset}`
    ),

  /** Vote history for the current authenticated user. */
  getMyVotes: (count = 10) =>
    request<ApiVoteHistoryItem[]>(`/api/users/me/votes?count=${count}`),

  getMyStreak: () => request<ApiStreakStatus>("/api/users/me/streak"),

  getCategoryPreferences: () =>
    request<ApiCategoryPreference[]>("/api/users/me/preferences/categories"),

  updateCategoryPreferences: (categories: string[]) =>
    request<ApiCategoryPreference[]>("/api/users/me/preferences/categories", {
      method: "PUT",
      body: JSON.stringify({ categories }),
    }),

  resetCategoryPreferences: () =>
    request<void>("/api/users/me/preferences/categories", { method: "DELETE" }),

  getMyProgression: () => request<ApiProgression>("/api/users/me/progression"),
  getWeeklyLeaderboard: (count = 5) =>
    request<ApiWeeklyLeaderboardResponse>(`/api/users/leaderboard/weekly?count=${count}`),
}

export interface SocialUser { id: number; username: string; displayName: string; avatarUrl?: string | null }
export interface Paged<T> { items: T[]; nextCursor?: string | null }
export interface FriendConnection { id: number; user: SocialUser; state: "Pending" | "Accepted" | "Declined" | "Removed"; incoming: boolean; updatedAt: string }
export interface SocialGroup { id: number; name: string; ownerUserId: number; moderationStatus: string; memberCount: number; role: "Owner" | "Member"; createdAt: string }
export interface WeeklyEntry { rank: number; user: SocialUser; xp: number; activityCount: number }
export interface WeeklyLeaderboard { weekStartUtc: string; weekEndUtc: string; items: WeeklyEntry[]; nextCursor?: string | null }

const qs = (values: Record<string, string | number | undefined>) => {
  const q = new URLSearchParams()
  Object.entries(values).forEach(([key, value]) => value !== undefined && q.set(key, String(value)))
  return q.toString()
}

export const socialApi = {
  searchUsers: (query: string, cursor?: string) => request<Paged<SocialUser>>(`/api/social/users?${qs({ query, cursor })}`),
  friends: (state?: FriendConnection["state"], cursor?: string) => request<Paged<FriendConnection>>(`/api/social/friends?${qs({ state, cursor })}`),
  sendFriendRequest: (targetUserId: number) => request<{ id: number }>("/api/social/friends/requests", { method: "POST", body: JSON.stringify({ targetUserId }) }),
  acceptFriendRequest: (id: number) => request<void>(`/api/social/friends/requests/${id}/accept`, { method: "POST" }),
  declineFriendRequest: (id: number) => request<void>(`/api/social/friends/requests/${id}/decline`, { method: "POST" }),
  removeFriend: (id: number) => request<void>(`/api/social/friends/${id}`, { method: "DELETE" }),
  block: (targetUserId: number) => request<void>("/api/social/blocks", { method: "POST", body: JSON.stringify({ targetUserId }) }),
  unblock: (id: number) => request<void>(`/api/social/blocks/${id}`, { method: "DELETE" }),
  friendsLeaderboard: (cursor?: string) => request<WeeklyLeaderboard>(`/api/social/leaderboards/friends?${qs({ cursor })}`),
  groups: (cursor?: string) => request<Paged<SocialGroup>>(`/api/social/groups?${qs({ cursor })}`),
  createGroup: (name: string) => request<SocialGroup>("/api/social/groups", { method: "POST", body: JSON.stringify({ name }) }),
  inviteToGroup: (groupId: number, targetUserId: number) => request<{ token: string }>(`/api/social/groups/${groupId}/invites`, { method: "POST", body: JSON.stringify({ targetUserId }) }),
  acceptInvite: (token: string) => request<void>(`/api/social/group-invites/${encodeURIComponent(token)}/accept`, { method: "POST" }),
  declineInvite: (token: string) => request<void>(`/api/social/group-invites/${encodeURIComponent(token)}/decline`, { method: "POST" }),
  leaveGroup: (id: number) => request<void>(`/api/social/groups/${id}/membership`, { method: "DELETE" }),
  groupLeaderboard: (id: number, cursor?: string) => request<WeeklyLeaderboard>(`/api/social/groups/${id}/leaderboard?${qs({ cursor })}`),
}

// ── Auth endpoints ────────────────────────────────────────────────────────────

export const authApi = {
  /** Fetch fresh profile data for the logged-in user. Requires auth token. */
  getMe: () => request<ApiUser>("/api/auth/me"),
}

export type ClashStatus = "Lobby" | "Active" | "Completed" | "Expired"
export interface ApiPollClashOption { id: number; text: string; publicVotes: number | null }
export interface ApiPollClashPlayer { userId: number; displayName: string; isViewer: boolean; hasSubmitted: boolean; opinionOptionId: number | null; predictedMajorityOptionId: number | null; predictionScore: number }
export interface ApiPollClashRound { id: number; position: number; pollId: number; question: string; status: "Pending" | "Active" | "Revealed"; options: ApiPollClashOption[]; resolvedMajorityOptionId: number | null; agreed: boolean | null; predictionPointsAwarded: number; revealedOpinions: { userId: number; displayName: string; opinionOptionId: number; predictedMajorityOptionId: number | null; predictionPoint: number }[] }
export interface ApiPollClash { id: number; inviteCode: string; status: ClashStatus; source: "Poll" | "GeneratedPack"; roundCount: number; completedRounds: number; expiresAt: string; players: ApiPollClashPlayer[]; rounds: ApiPollClashRound[]; agreementCount: number; winnerUserId: number | null; reward: { awardedXp: number; isDuplicate: boolean; capReached: boolean }; rematch: { id: number; requestedByUserId: number; status: "Pending" | "Accepted" | "Declined"; resultingClashId: number | null } | null }
export const pollClashesApi = {
  create: (input: { seedPollId?: number; source: "Poll" | "GeneratedPack"; roundCount: 1 | 3 | 5 }) => request<ApiPollClash>("/api/poll-clashes", { method: "POST", body: JSON.stringify(input) }),
  get: (id: number) => request<ApiPollClash>(`/api/poll-clashes/${id}`),
  invite: (code: string) => request<ApiPollClash>(`/api/poll-clashes/invite/${encodeURIComponent(code)}`),
  join: (id: number) => request<ApiPollClash>(`/api/poll-clashes/${id}/join`, { method: "POST" }),
  respond: (id: number, input: { roundId: number; opinionOptionId: number; predictedMajorityOptionId?: number }) => request<ApiPollClash>(`/api/poll-clashes/${id}/responses`, { method: "POST", body: JSON.stringify(input) }),
  requestRematch: (id: number) => request<ApiPollClash>(`/api/poll-clashes/${id}/rematch-requests`, { method: "POST" }),
  acceptRematch: (id: number, requestId: number) => request<ApiPollClash>(`/api/poll-clashes/${id}/rematch-requests/${requestId}/accept`, { method: "POST" }),
  declineRematch: (id: number, requestId: number) => request<ApiPollClash>(`/api/poll-clashes/${id}/rematch-requests/${requestId}/decline`, { method: "POST" }),
}
