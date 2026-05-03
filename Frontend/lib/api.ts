/**
 * Centralized API client for the Pollify backend (US-12).
 * Base URL is read from NEXT_PUBLIC_API_URL env variable.
 */

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5177"

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
  expiresAt: string        // ISO date string
  createdAt: string        // ISO date string
  totalVotes: number
  sourceType: string | null
  sourceUrl: string | null
  thumbnailUrl: string | null
  isAIGenerated: boolean
  options: ApiPollOption[]
}

export interface CastVoteRequest {
  pollId: number
  optionId: number
}

export interface CreatePollPayload {
  question: string
  description: string
  category: string
  expiresAt: string        // ISO date string
  options: string[]
  sourceType?: string
  sourceUrl?: string
  thumbnailUrl?: string
  isAIGenerated?: boolean
}

// ── Helpers ───────────────────────────────────────────────────────────────────

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(`API ${res.status}: ${text}`)
  }

  return res.json() as Promise<T>
}

// ── Poll endpoints ────────────────────────────────────────────────────────────

export const pollsApi = {
  /** Fetch trending polls for the feed. */
  getTrending: (count = 20) =>
    request<ApiPoll[]>(`/api/polls/trending?count=${count}`),

  /** Fetch all polls. */
  getAll: () => request<ApiPoll[]>("/api/polls"),

  /** Create a new poll. Returns the created poll. */
  create: (payload: CreatePollPayload) =>
    request<ApiPoll>("/api/polls", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
}

// ── Vote endpoints ────────────────────────────────────────────────────────────

export const votesApi = {
  /** Cast a vote — returns the updated poll with fresh percentages. */
  cast: (req: CastVoteRequest) =>
    request<ApiPoll>("/api/votes", {
      method: "POST",
      body: JSON.stringify(req),
    }),
}
