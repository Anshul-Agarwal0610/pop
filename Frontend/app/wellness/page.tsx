"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { Download, HeartPulse, Loader2, RefreshCw, ShieldCheck, Trash2 } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { useAuth } from "@/contexts/auth-context"
import { wellnessApi, type ApiPoll, type ApiWellnessOverview } from "@/lib/api"

export default function WellnessPage() {
  const { isAuthenticated } = useAuth()
  const [overview, setOverview] = useState<ApiWellnessOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [savingOptionId, setSavingOptionId] = useState<number | null>(null)
  const [exporting, setExporting] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [noteByPoll, setNoteByPoll] = useState<Record<number, string>>({})
  const [error, setError] = useState<string | null>(null)

  const loadOverview = useCallback(async () => {
    if (!isAuthenticated) return

    setLoading(true)
    setError(null)
    try {
      setOverview(await wellnessApi.getOverview())
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load wellness mode")
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated])

  useEffect(() => {
    if (isAuthenticated) loadOverview()
  }, [isAuthenticated, loadOverview])

  const latestResponse = overview?.history[0]
  const insight = overview?.insight
  const checkInCount = insight?.totalCheckIns ?? 0

  const consentPoints = useMemo(() => [
    "Your wellness responses stay out of public feeds, leaderboards, result cards, and share previews.",
    "Wellness check-ins are stored separately from public poll votes.",
    "You can export or delete your wellness response history at any time.",
  ], [])

  async function answer(poll: ApiPoll, optionId: number) {
    setSavingOptionId(optionId)
    setError(null)
    try {
      await wellnessApi.createResponse(poll.id, optionId, noteByPoll[poll.id])
      setNoteByPoll((current) => ({ ...current, [poll.id]: "" }))
      await loadOverview()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save check-in")
    } finally {
      setSavingOptionId(null)
    }
  }

  async function exportCsv() {
    setExporting(true)
    setError(null)
    try {
      const blob = await wellnessApi.exportCsv()
      const url = URL.createObjectURL(blob)
      const link = document.createElement("a")
      link.href = url
      link.download = "wellness-responses.csv"
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not export wellness data")
    } finally {
      setExporting(false)
    }
  }

  async function deleteResponses() {
    setDeleting(true)
    setError(null)
    try {
      await wellnessApi.deleteResponses()
      await loadOverview()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not delete wellness responses")
    } finally {
      setDeleting(false)
    }
  }

  return (
    <AppShell>
      <main className="mx-auto w-full max-w-4xl px-4 py-6">
        <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-foreground">
              <HeartPulse className="h-6 w-6 text-rose-500" />
              Private Wellness
            </h1>
            <p className="text-sm text-muted-foreground">
              Personal check-ins for reflection, not public competition.
            </p>
          </div>
          <Button className="gap-2" onClick={loadOverview} variant="outline">
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
        </div>

        {error && (
          <div className="mb-4 rounded-lg border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <section className="mb-5 rounded-lg border border-rose-500/20 bg-rose-500/10 p-4">
          <div className="flex gap-3">
            <ShieldCheck className="mt-0.5 h-5 w-5 flex-shrink-0 text-rose-600" />
            <div>
              <h2 className="font-semibold text-foreground">Privacy and consent</h2>
              <div className="mt-2 space-y-1 text-sm text-muted-foreground">
                {consentPoints.map((point) => (
                  <p key={point}>{point}</p>
                ))}
              </div>
            </div>
          </div>
        </section>

        {loading ? (
          <div className="flex items-center justify-center gap-3 py-20 text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading wellness mode...
          </div>
        ) : (
          <div className="grid gap-4 lg:grid-cols-[1fr_280px]">
            <section className="space-y-4">
              {overview?.polls.length ? (
                overview.polls.map((poll) => (
                  <div className="rounded-lg border border-border/60 bg-card p-4" key={poll.id}>
                    <div className="mb-3">
                      <p className="text-xs font-semibold uppercase text-rose-600">Private check-in</p>
                      <h2 className="mt-1 text-lg font-bold text-foreground">{poll.question}</h2>
                      {poll.description && (
                        <p className="mt-1 text-sm text-muted-foreground">{poll.description}</p>
                      )}
                    </div>

                    <div className="grid gap-2 sm:grid-cols-2">
                      {poll.options.map((option) => (
                        <Button
                          className="min-h-12 justify-start rounded-lg"
                          disabled={savingOptionId != null}
                          key={option.id}
                          onClick={() => answer(poll, option.id)}
                          variant="outline"
                        >
                          {savingOptionId === option.id ? (
                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          ) : null}
                          {option.text}
                        </Button>
                      ))}
                    </div>

                    <Textarea
                      className="mt-3"
                      onChange={(event) =>
                        setNoteByPoll((current) => ({ ...current, [poll.id]: event.target.value }))
                      }
                      placeholder="Optional private note"
                      value={noteByPoll[poll.id] ?? ""}
                    />
                  </div>
                ))
              ) : (
                <div className="rounded-lg border border-dashed border-border/70 px-4 py-12 text-center text-sm text-muted-foreground">
                  No wellness check-ins are active yet.
                </div>
              )}
            </section>

            <aside className="space-y-4">
              <div className="rounded-lg border border-border/60 bg-card p-4">
                <h2 className="font-semibold text-foreground">Personal insight</h2>
                <div className="mt-3 grid grid-cols-2 gap-2 text-sm">
                  <Metric label="Check-ins" value={String(checkInCount)} />
                  <Metric
                    label="Common"
                    value={insight?.mostCommonResponse ?? "None"}
                  />
                </div>
                <p className="mt-3 text-xs text-muted-foreground">
                  {latestResponse
                    ? `Last check-in: ${new Date(latestResponse.createdAt).toLocaleString()}`
                    : "Your first check-in will appear here."}
                </p>
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-4">
                <h2 className="font-semibold text-foreground">Your data</h2>
                <div className="mt-3 flex flex-col gap-2">
                  <Button className="gap-2" disabled={exporting || checkInCount === 0} onClick={exportCsv} variant="outline">
                    {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
                    Export CSV
                  </Button>
                  <Button className="gap-2" disabled={deleting || checkInCount === 0} onClick={deleteResponses} variant="destructive">
                    {deleting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
                    Delete history
                  </Button>
                </div>
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-4">
                <h2 className="font-semibold text-foreground">Recent history</h2>
                <div className="mt-3 space-y-3">
                  {overview?.history.length ? (
                    overview.history.map((item) => (
                      <div className="border-b border-border/50 pb-3 last:border-0 last:pb-0" key={item.id}>
                        <p className="text-sm font-medium text-foreground">{item.optionText}</p>
                        <p className="text-xs text-muted-foreground">{item.question}</p>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {new Date(item.createdAt).toLocaleString()}
                        </p>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-muted-foreground">No private check-ins yet.</p>
                  )}
                </div>
              </div>
            </aside>
          </div>
        )}
      </main>
    </AppShell>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md bg-secondary/60 p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 truncate font-semibold text-foreground">{value}</p>
    </div>
  )
}
