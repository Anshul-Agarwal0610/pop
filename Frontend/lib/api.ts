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

export const pollsApi = {
  /** Fetch trending polls for the feed. Includes hasVoted when authenticated. */
  getTrending: (count = 20, category?: string) => {
    const query = new URLSearchParams({ count: String(count) })
    if (category) query.set("category", category)
    return request<ApiPoll[]>(`/api/polls/trending?${query.toString()}`)
  },

  /** Fetch all polls. Includes hasVoted when authenticated. */
  getAll: (category?: string) => {
    const query = categoryQuery(category)
    return request<ApiPoll[]>(`/api/polls${query ? `?${query}` : ""}`)
  },

  /** Fetch one poll. Includes hasVoted when authenticated. */
  getById: (id: number | string) => request<ApiPoll>(`/api/polls/${id}`),

  /** Create a new poll. Returns the created poll. */
  create: (payload: CreatePollPayload) =>
    request<ApiPoll>("/api/polls", {
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

// ── User endpoints ────────────────────────────────────────────────────────────

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

export const usersApi = {
  /** Leaderboard — top users by XP. */
  getLeaderboard: (count = 20) =>
    request<ApiUser[]>(`/api/users/leaderboard?count=${count}`),

  /** Vote history for the current authenticated user. */
  getMyVotes: (count = 10) =>
    request<ApiVoteHistoryItem[]>(`/api/users/me/votes?count=${count}`),
}

// ── Auth endpoints ────────────────────────────────────────────────────────────

export const authApi = {
  /** Fetch fresh profile data for the logged-in user. Requires auth token. */
  getMe: () => request<ApiUser>("/api/auth/me"),
}
