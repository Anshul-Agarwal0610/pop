/**
 * Centralized API client for the Pollify backend (US-12 + US-18).
 * Base URL is read from NEXT_PUBLIC_API_URL env variable.
 * JWT token is automatically attached from localStorage when present.
 */

import { getToken } from "@/lib/auth"
import { API_BASE_URL } from "@/lib/config"

// ── Raw backend shapes ────────────────────────────────────────────────────────

export interface ApiPollOption {
  id: number
  pollId: number
  text: string
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
}

export interface ApiVoteReward {
  xp: number
  streak: number
  totalVotes: number
  xpAwarded: number
  streakAdvanced: boolean
  lastVoteDate: string | null
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
}

export interface ApiNotification {
  id: number
  userId: number
  type: "VoteMilestone" | "LevelUp" | "PollTrending" | "DailyReminder" | "ChallengeAvailable" | "StreakReminder" | "PollExpiring"
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
    throw new Error(`API ${res.status}: ${text}`)
  }

  return res.json() as Promise<T>
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

// ── User endpoints ────────────────────────────────────────────────────────────

export const challengesApi = {
  getActive: () => request<ApiChallenge[]>("/api/challenges/active"),
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
  totalVotes: number
  pollsCreated: number
  lastVoteDate?: string | null
  createdAt: string
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

export const usersApi = {
  /** Leaderboard — top users by XP. */
  getLeaderboard: (count = 20) =>
    request<ApiUser[]>(`/api/users/leaderboard?count=${count}`),

  /** Vote history for the current authenticated user. */
  getMyVotes: (count = 10) =>
    request<ApiVoteHistoryItem[]>(`/api/users/me/votes?count=${count}`),

  getCategoryPreferences: () =>
    request<ApiCategoryPreference[]>("/api/users/me/preferences/categories"),

  updateCategoryPreferences: (categories: string[]) =>
    request<ApiCategoryPreference[]>("/api/users/me/preferences/categories", {
      method: "PUT",
      body: JSON.stringify({ categories }),
    }),

  resetCategoryPreferences: () =>
    request<void>("/api/users/me/preferences/categories", { method: "DELETE" }),
}

// ── Auth endpoints ────────────────────────────────────────────────────────────

export const authApi = {
  /** Fetch fresh profile data for the logged-in user. Requires auth token. */
  getMe: () => request<ApiUser>("/api/auth/me"),
}
