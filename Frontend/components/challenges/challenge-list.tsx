import type { ApiChallenge } from "@/lib/api"
import { ChallengeCard } from "./challenge-card"

export function ChallengeList({ challenges, empty = "No challenges here yet." }: { challenges: ApiChallenge[]; empty?: string }) {
  if (!challenges.length) return <p className="rounded-2xl border border-dashed p-6 text-center text-sm text-muted-foreground">{empty}</p>
  return <div className="grid gap-4 md:grid-cols-2">{challenges.map(challenge => <ChallengeCard challenge={challenge} key={challenge.challengeId} />)}</div>
}
