import { render, screen } from "@testing-library/react"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { GameHub } from "@/components/game-hub/game-hub"
import { achievementsApi, challengesApi, usersApi } from "@/lib/api"

const auth = { isLoading: false, isAuthenticated: true, logout: vi.fn() }
vi.mock("@/contexts/auth-context", () => ({ useAuth: () => auth }))
vi.mock("@/lib/api", async importOriginal => {
  const actual = await importOriginal<typeof import("@/lib/api")>()
  return {
    ...actual,
    challengesApi: { getActive: vi.fn() },
    usersApi: { getMyProgression: vi.fn(), getWeeklyLeaderboard: vi.fn() },
    achievementsApi: { getMyOverview: vi.fn() },
  }
})

const challenge = { challengeId: 1, title: "Daily Pulse", category: "tech", requiredVotes: 3, rewardXp: 75, rewardBadge: "Daily Voter", startAt: "2026-08-06", endAt: "2026-08-07", currentVotes: 1, isCompleted: false, rewardGranted: false, completedAt: null }
const progression = { xp: 1420, level: 2, currentLevelStartXp: 1000, nextLevelXp: 2000, xpIntoLevel: 420, xpRequiredForLevel: 1000, progressPercent: 42, streak: 4, todayActivityComplete: true, lastVoteDate: "2026-08-06" }

beforeEach(() => {
  vi.clearAllMocks()
  auth.isAuthenticated = true
  vi.mocked(challengesApi.getActive).mockResolvedValue([challenge])
  vi.mocked(usersApi.getMyProgression).mockResolvedValue(progression)
  vi.mocked(achievementsApi.getMyOverview).mockResolvedValue({ recentlyEarned: [{ id: 1, userId: 1, badgeId: 1, code: "first", name: "First Vote", description: "", icon: "Vote", awardedAt: "2026-08-06" }], nextAchievable: [{ badgeId: 2, code: "ten", name: "Pulse 10", description: "", icon: "Zap", ruleType: "VoteCount", currentValue: 4, threshold: 10, progressPercent: 40, rewardXp: 50 }], allEarned: false })
  vi.mocked(usersApi.getWeeklyLeaderboard).mockResolvedValue({ weekStart: "2026-08-03", weekEnd: "2026-08-10", scoreUnit: "votes", entries: [{ userId: 1, username: "ada", displayName: "Ada", rank: 2, score: 8, scoreUnit: "votes" }], currentUser: { userId: 1, username: "ada", displayName: "Ada", rank: 2, score: 8, scoreUnit: "votes" } })
})

describe("GameHub", () => {
  it("shows populated progression and starts independent requests", async () => {
    render(<GameHub />)
    expect(await screen.findByText("Daily Pulse")).toBeInTheDocument()
    expect(screen.getByText("4 day streak")).toBeInTheDocument()
    expect(screen.getByText("Today's activity is complete")).toBeInTheDocument()
    expect(screen.getByText("Level 2")).toBeInTheDocument()
    expect(await screen.findByText(/First Vote/)).toBeInTheDocument()
    expect(await screen.findByText(/You are #2/)).toBeInTheDocument()
    expect(screen.getByRole("link", { name: "Play now" })).toHaveAttribute("href", "/polls?category=Technology")
    expect(challengesApi.getActive).toHaveBeenCalledOnce()
    expect(usersApi.getMyProgression).toHaveBeenCalledOnce()
    expect(achievementsApi.getMyOverview).toHaveBeenCalledOnce()
    expect(usersApi.getWeeklyLeaderboard).toHaveBeenCalledOnce()
  })

  it("prompts signed-out users without calling protected APIs", () => {
    auth.isAuthenticated = false
    render(<GameHub />)
    expect(screen.getByRole("link", { name: "Sign in to play" })).toHaveAttribute("href", expect.stringContaining("redirect=%2Fplay"))
    expect(challengesApi.getActive).not.toHaveBeenCalled()
  })

  it("keeps successful sections visible after a partial failure", async () => {
    vi.mocked(achievementsApi.getMyOverview).mockRejectedValue(new Error("badge service unavailable"))
    render(<GameHub />)
    expect(await screen.findByText("Daily Pulse")).toBeInTheDocument()
    expect(await screen.findByText("Could not load badges")).toBeInTheDocument()
    expect(await screen.findByText("Level 2")).toBeInTheDocument()
  })

  it("renders the completed and empty leaderboard states", async () => {
    vi.mocked(challengesApi.getActive).mockResolvedValue([{ ...challenge, currentVotes: 3, isCompleted: true }])
    vi.mocked(usersApi.getWeeklyLeaderboard).mockResolvedValue({ weekStart: "2026-08-03", weekEnd: "2026-08-10", scoreUnit: "votes", entries: [], currentUser: null })
    render(<GameHub />)
    expect(await screen.findByText(/Completed today/)).toBeInTheDocument()
    expect(screen.getByRole("link", { name: "Keep playing" })).toBeInTheDocument()
    expect(screen.getByText("No qualifying activity this week yet.")).toBeInTheDocument()
  })
})
