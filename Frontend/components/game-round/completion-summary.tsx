import { Award, Target, Zap } from "lucide-react"
import type { ApiCompletionSummary } from "@/lib/api"

export function CompletionSummary({ summary }: { summary: ApiCompletionSummary }) {
  return <section className="rounded-3xl bg-card p-7 text-center ring-1 ring-border" aria-labelledby="complete-title"><Award className="mx-auto h-14 w-14 text-amber-500"/><h1 id="complete-title" className="mt-3 text-3xl font-black">Round complete!</h1>
    <div className="mt-6 grid grid-cols-2 gap-3"><div className="rounded-2xl bg-muted p-4"><p className="text-2xl font-black">{summary.votes}</p><p className="text-sm">votes</p></div><div className="rounded-2xl bg-muted p-4"><p className="flex items-center justify-center gap-1 text-2xl font-black"><Zap className="h-5 w-5"/>{summary.totalXpEarned}</p><p className="text-sm">XP earned</p></div></div>
    <div className="mt-5 text-left"><h2 className="flex items-center gap-2 font-bold"><Target className="h-4 w-4"/>Challenge progress</h2>{summary.challengeProgress.length?<ul className="mt-2 space-y-1">{summary.challengeProgress.map(c=><li key={c.challengeId}>{c.title}: {c.currentVotes}/{c.requiredVotes}</li>)}</ul>:<p className="mt-1 text-sm text-muted-foreground">No challenge progress this round.</p>}
    <h2 className="mt-5 font-bold">Achievements unlocked</h2>{summary.achievementsUnlocked.length?<ul className="mt-2">{summary.achievementsUnlocked.map(a=><li key={a.id}>{a.name}</li>)}</ul>:<p className="mt-1 text-sm text-muted-foreground">No new achievements this round.</p>}</div>
  </section>
}
