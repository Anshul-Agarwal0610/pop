type ShareablePoll = {
  id: number | string
  question: string
  category?: string | null
  totalVotes?: number
  isAIGenerated?: boolean
}

type ShareableOption = {
  text: string
  side?: "Up" | "Against" | null
  votePercentage?: number | null
}

export function appBaseUrl() {
  const configured = process.env.NEXT_PUBLIC_BASE_URL
  if (configured) return configured.replace(/\/+$/, "")

  if (typeof window !== "undefined") {
    return window.location.origin
  }

  return ""
}

export function isPollPubliclyShareable(category?: string | null) {
  return !category?.trim().toLowerCase().includes("health")
}

export function pollShareUrl(pollId: number | string) {
  return `${appBaseUrl()}/polls/${pollId}`
}

export function pollResultShareUrl(pollId: number | string) {
  return `${pollShareUrl(pollId)}?view=results`
}

export function pollShareText(poll: ShareablePoll) {
  return `${poll.question}${poll.totalVotes != null ? ` (${poll.totalVotes.toLocaleString()} votes)` : ""}`
}

export function resultShareText(poll: ShareablePoll, option?: ShareableOption | null) {
  if (!option) return pollShareText(poll)

  const percentage = Math.round(option.votePercentage ?? 0)
  const label = poll.isAIGenerated ? (option.side ?? "Invalid choice") : option.text
  return `${poll.question} Result: ${label} is at ${percentage}%`
}
