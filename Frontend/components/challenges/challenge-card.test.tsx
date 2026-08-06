import { render, screen } from "@testing-library/react"
import { describe, expect, it } from "vitest"
import { ChallengeCard, challengePercent } from "./challenge-card"
import type { ApiChallenge } from "@/lib/api"

const challenge: ApiChallenge = { challengeId: 1, title: "Weekly Tech Voice", description: "Vote this week", challengeType: "Category", recurrence: "Weekly", requirementType: "VoteCount", requirementText: "Cast 7 Technology votes", category: "Technology", requiredVotes: 7, rewardXp: 200, rewardBadge: "Tech Voice", startAt: "2026-08-03T00:00:00Z", endAt: "2099-08-10T00:00:00Z", currentVotes: 2, isCompleted: false, rewardGranted: false, completedAt: null, state: "InProgress", eligiblePollsUrl: "/polls?category=Technology" }

describe("ChallengeCard", () => {
  it("renders weekly metadata, progress, rewards, and a category-aware continue action", () => {
    render(<ChallengeCard challenge={challenge} />)
    expect(screen.getByText("Weekly")).toBeInTheDocument()
    expect(screen.getByText("2/7")).toBeInTheDocument()
    expect(screen.getByText("200 XP")).toBeInTheDocument()
    expect(screen.getByRole("link", { name: "Continue challenge" })).toHaveAttribute("href", "/polls?category=Technology")
  })
  it("caps visual progress", () => expect(challengePercent({ ...challenge, currentVotes: 99 })).toBe(100))
  it("removes actions for completed and expired challenges", () => {
    const { rerender } = render(<ChallengeCard challenge={{ ...challenge, state: "Completed", isCompleted: true }} />)
    expect(screen.queryByRole("link")).not.toBeInTheDocument()
    rerender(<ChallengeCard challenge={{ ...challenge, state: "Expired" }} />)
    expect(screen.getByText("Expired")).toBeInTheDocument()
  })
})
