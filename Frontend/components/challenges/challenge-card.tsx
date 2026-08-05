import Link from "next/link"
import { Award, CalendarClock, CheckCircle2, LockKeyhole, Target, Trophy } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { ApiChallenge } from "@/lib/api"
import { cn } from "@/lib/utils"

export const challengePercent = (challenge: ApiChallenge) =>
  Math.min(100, Math.max(0, challenge.currentVotes / Math.max(1, challenge.requiredVotes) * 100))

export function ChallengeCard({ challenge }: { challenge: ApiChallenge }) {
  const actionable = challenge.state === "Available" || challenge.state === "InProgress"
  return <article className={cn("rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/60", challenge.state === "Expired" && "opacity-65")}>
    <div className="flex items-start justify-between gap-3">
      <div><div className="mb-2 flex gap-2 text-xs font-semibold uppercase text-primary"><span>{challenge.recurrence}</span><span>•</span><span>{challenge.category ?? "Any category"}</span></div>
        <h3 className="font-semibold">{challenge.title}</h3><p className="mt-1 text-sm text-muted-foreground">{challenge.description}</p></div>
      {challenge.state === "Completed" ? <CheckCircle2 className="h-6 w-6 text-emerald-500" /> : challenge.state === "Expired" ? <LockKeyhole className="h-6 w-6" /> : <Target className="h-6 w-6 text-primary" />}
    </div>
    <p className="mt-3 text-sm font-medium">{challenge.requirementText}</p>
    <div className="mt-2 h-2.5 overflow-hidden rounded-full bg-secondary" role="progressbar" aria-valuemax={challenge.requiredVotes} aria-valuenow={Math.min(challenge.currentVotes, challenge.requiredVotes)}>
      <div className="h-full rounded-full bg-primary" style={{ width: `${challengePercent(challenge)}%` }} />
    </div>
    <div className="mt-2 flex justify-between text-xs text-muted-foreground"><span>{Math.min(challenge.currentVotes, challenge.requiredVotes)}/{challenge.requiredVotes}</span><span className="flex items-center gap-1"><CalendarClock className="h-3.5 w-3.5" />{challenge.state === "Expired" ? "Expired" : `Ends ${new Date(challenge.endAt).toLocaleString([], { timeZone: "UTC", timeZoneName: "short" })}`}</span></div>
    <div className="mt-4 flex flex-wrap items-center gap-3 text-sm"><span className="flex items-center gap-1"><Trophy className="h-4 w-4 text-amber-500" />{challenge.rewardXp} XP</span>{challenge.rewardBadge && <span className="flex items-center gap-1"><Award className="h-4 w-4 text-violet-500" />{challenge.rewardBadge}</span>}<span className="ml-auto">{challenge.state.replace(/([A-Z])/g, " $1").trim()}</span></div>
    {actionable && <Button asChild className="mt-4 w-full"><Link href={challenge.eligiblePollsUrl}>{challenge.state === "Available" ? "Start" : "Continue"} challenge</Link></Button>}
  </article>
}
