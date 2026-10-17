"use client"

import Link from "next/link"
import { useEffect, useMemo, useState } from "react"
import { BarChart3, Clock3, Loader2, PlusCircle } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { CategoryBadge } from "@/components/category-badge"
import { ShareButton } from "@/components/share-button"
import { Button } from "@/components/ui/button"
import { pollsApi, type ApiPoll } from "@/lib/api"

const statusTone: Record<ApiPoll["moderationStatus"], string> = {
  Draft: "bg-slate-500/10 text-slate-600 dark:text-slate-300",
  PendingReview: "bg-amber-500/10 text-amber-700 dark:text-amber-300",
  Published: "bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
  Rejected: "bg-red-500/10 text-red-700 dark:text-red-300",
  Flagged: "bg-orange-500/10 text-orange-700 dark:text-orange-300",
}

function timeLabel(poll: ApiPoll) {
  const remaining = new Date(poll.expiresAt).getTime() - Date.now()
  if (remaining <= 0) return "Closed"
  const hours = Math.ceil(remaining / 3_600_000)
  return hours < 24 ? `${hours}h left` : `${Math.ceil(hours / 24)}d left`
}

export default function MyPollsPage() {
  const [polls, setPolls] = useState<ApiPoll[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    pollsApi.getMine(200)
      .then(setPolls)
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : "Could not load your polls."))
      .finally(() => setLoading(false))
  }, [])

  const totals = useMemo(() => ({
    votes: polls.reduce((sum, poll) => sum + poll.totalVotes, 0),
    active: polls.filter((poll) => poll.isActive && new Date(poll.expiresAt) > new Date()).length,
  }), [polls])

  return (
    <AppShell>
      <main className="mx-auto max-w-4xl px-4 py-6">
        <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm font-semibold uppercase tracking-wider text-primary">Creator dashboard</p>
            <h1 className="mt-1 text-3xl font-bold text-foreground">My polls</h1>
            <p className="mt-1 text-muted-foreground">Track votes, status, remaining time, and share your published polls.</p>
          </div>
          <Button asChild><Link href="/create"><PlusCircle className="mr-2 h-4 w-4" />Create poll</Link></Button>
        </div>

        <div className="mb-6 grid grid-cols-3 gap-3">
          <div className="rounded-2xl bg-card p-4 ring-1 ring-border"><p className="text-sm text-muted-foreground">Created</p><p className="mt-1 text-2xl font-bold">{polls.length}</p></div>
          <div className="rounded-2xl bg-card p-4 ring-1 ring-border"><p className="text-sm text-muted-foreground">Active</p><p className="mt-1 text-2xl font-bold">{totals.active}</p></div>
          <div className="rounded-2xl bg-card p-4 ring-1 ring-border"><p className="text-sm text-muted-foreground">Votes</p><p className="mt-1 text-2xl font-bold">{totals.votes}</p></div>
        </div>

        {loading ? <div className="flex justify-center py-20"><Loader2 className="h-7 w-7 animate-spin text-primary" /></div> : null}
        {error ? <div className="rounded-2xl border border-destructive/30 bg-destructive/10 p-5 text-destructive">{error}</div> : null}
        {!loading && !error && polls.length === 0 ? (
          <div className="rounded-2xl bg-card p-10 text-center ring-1 ring-border">
            <BarChart3 className="mx-auto h-10 w-10 text-muted-foreground" />
            <h2 className="mt-3 text-lg font-semibold">No polls created yet</h2>
            <p className="mt-1 text-sm text-muted-foreground">Create your first poll to see its performance here.</p>
          </div>
        ) : null}

        <div className="space-y-4">
          {polls.map((poll) => (
            <article className="rounded-2xl bg-card p-5 shadow-sm ring-1 ring-border" key={poll.id}>
              <div className="flex flex-wrap items-center gap-2">
                <CategoryBadge category={poll.category} />
                <span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${statusTone[poll.moderationStatus]}`}>{poll.moderationStatus.replace(/([a-z])([A-Z])/g, "$1 $2")}</span>
                <span className="ml-auto flex items-center gap-1 text-xs text-muted-foreground"><Clock3 className="h-3.5 w-3.5" />{timeLabel(poll)}</span>
              </div>
              <h2 className="mt-3 text-lg font-semibold leading-snug">{poll.question}</h2>
              <div className="mt-4 grid gap-2 sm:grid-cols-2">
                {poll.options.map((option) => (
                  <div className="rounded-xl bg-muted/50 p-3" key={option.id}>
                    <div className="flex justify-between gap-3 text-sm"><span>{option.text}</span><strong>{option.votePercentage.toFixed(0)}%</strong></div>
                    <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-background"><div className="h-full rounded-full bg-primary" style={{ width: `${option.votePercentage}%` }} /></div>
                  </div>
                ))}
              </div>
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <span className="mr-auto text-sm font-semibold">{poll.totalVotes.toLocaleString()} votes</span>
                {poll.moderationStatus === "Published" && !poll.isPrivate ? <ShareButton pollId={poll.id} title={poll.question} variant="outline" /> : null}
                <Button asChild size="sm" variant="secondary"><Link href={`/polls/${poll.id}`}>View details</Link></Button>
              </div>
              {poll.moderationReason ? <p className="mt-3 text-xs text-muted-foreground">Review note: {poll.moderationReason}</p> : null}
            </article>
          ))}
        </div>
      </main>
    </AppShell>
  )
}
