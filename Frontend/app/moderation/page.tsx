"use client"

import { useCallback, useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import {
  CheckCircle2,
  ExternalLink,
  Loader2,
  RefreshCw,
  ShieldCheck,
  Sparkles,
  XCircle,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { pollsApi, type ApiPoll } from "@/lib/api"

export default function ModerationPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [polls, setPolls] = useState<ApiPoll[]>([])
  const [loading, setLoading] = useState(true)
  const [actingPollId, setActingPollId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  const loadQueue = useCallback(async () => {
    if (!isAuthenticated) return

    setLoading(true)
    setError(null)
    try {
      setPolls(await pollsApi.getModerationQueue(undefined, 50))
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load moderation queue")
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated])

  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) {
      router.push("/login?message=Sign in to review polls&redirect=/moderation")
      return
    }

    loadQueue()
  }, [authLoading, isAuthenticated, loadQueue, router])

  async function moderatePoll(poll: ApiPoll, status: "Published" | "Rejected") {
    setActingPollId(poll.id)
    setError(null)
    try {
      await pollsApi.moderate(poll.id, {
        status,
        reason: status === "Rejected" ? "Rejected during moderation review." : poll.moderationReason ?? undefined,
      })
      setPolls((current) => current.filter((item) => item.id !== poll.id))
    } catch (err) {
      setError(err instanceof Error ? err.message : `Could not ${status.toLowerCase()} poll`)
    } finally {
      setActingPollId(null)
    }
  }

  return (
    <AppShell>
      <main className="mx-auto w-full max-w-5xl px-4 py-6">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-foreground">
              <ShieldCheck className="h-6 w-6 text-primary" />
              Poll Review
            </h1>
            <p className="text-sm text-muted-foreground">
              Review pending AI, sponsored, and reported polls before they reach the public feed.
            </p>
          </div>
          <Button className="gap-2" onClick={loadQueue} variant="outline">
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        </div>

        {error && (
          <div className="mb-4 rounded-lg border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center gap-3 py-20 text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading review queue...
          </div>
        ) : polls.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border/70 px-4 py-12 text-center text-sm text-muted-foreground">
            No polls are waiting for review.
          </div>
        ) : (
          <div className="space-y-4">
            {polls.map((poll) => (
              <article className="rounded-lg border border-border/60 bg-card p-4" key={poll.id}>
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div className="min-w-0 flex-1">
                    <div className="mb-2 flex flex-wrap items-center gap-2">
                      <span className="rounded-full bg-secondary px-2.5 py-1 text-xs font-semibold text-secondary-foreground">
                        {poll.moderationStatus}
                      </span>
                      {poll.isAIGenerated && (
                        <span className="inline-flex items-center gap-1 rounded-full bg-violet-500/15 px-2.5 py-1 text-xs font-medium text-violet-500">
                          <Sparkles className="h-3 w-3" />
                          AI generated
                        </span>
                      )}
                      <span className="rounded-full bg-primary/10 px-2.5 py-1 text-xs font-medium text-primary">
                        {poll.category}
                      </span>
                    </div>
                    <h2 className="text-lg font-bold leading-snug text-foreground">{poll.question}</h2>
                    {poll.description && (
                      <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted-foreground">
                        {poll.description}
                      </p>
                    )}
                    {poll.moderationReason && (
                      <p className="mt-3 rounded-md bg-secondary/70 px-3 py-2 text-sm text-muted-foreground">
                        {poll.moderationReason}
                      </p>
                    )}
                    {poll.sourceUrl && (
                      <a
                        className="mt-3 inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline"
                        href={poll.sourceUrl}
                        rel="noreferrer"
                        target="_blank"
                      >
                        <ExternalLink className="h-4 w-4" />
                        View source
                      </a>
                    )}
                  </div>
                  <div className="flex w-full gap-2 sm:w-auto">
                    <Button
                      className="flex-1 gap-2 sm:flex-none"
                      disabled={actingPollId === poll.id}
                      onClick={() => moderatePoll(poll, "Published")}
                    >
                      {actingPollId === poll.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <CheckCircle2 className="h-4 w-4" />
                      )}
                      Publish
                    </Button>
                    <Button
                      className="flex-1 gap-2 sm:flex-none"
                      disabled={actingPollId === poll.id}
                      onClick={() => moderatePoll(poll, "Rejected")}
                      variant="outline"
                    >
                      <XCircle className="h-4 w-4" />
                      Reject
                    </Button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </main>
    </AppShell>
  )
}
