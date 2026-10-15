"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import {
  Activity, AlertTriangle, BarChart3, Bot, BriefcaseBusiness, CheckCircle2,
  Database, Flag, Loader2, LogOut, Pause, Play, RefreshCw, ShieldCheck,
  Sparkles, Users, Vote, XCircle,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import {
  adminApi, pollsApi, type ApiPipelineHealth, type ApiPlatformDashboard, type ApiPoll,
} from "@/lib/api"
import { cn } from "@/lib/utils"

type Tab = "overview" | "moderation" | "pipeline"
const sources = ["rss", "youtube", "gnews", "x", "instagram", "events"]

export default function AdminPage() {
  const router = useRouter()
  const { user, isAuthenticated, isLoading: authLoading, logout } = useAuth()
  const [tab, setTab] = useState<Tab>("overview")
  const [dashboard, setDashboard] = useState<ApiPlatformDashboard | null>(null)
  const [pipeline, setPipeline] = useState<ApiPipelineHealth | null>(null)
  const [queue, setQueue] = useState<ApiPoll[]>([])
  const [loading, setLoading] = useState(true)
  const [action, setAction] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!user?.isAdmin) return
    setLoading(true); setError(null)
    const results = await Promise.allSettled([
      adminApi.dashboard(), adminApi.pipelineHealth(), pollsApi.getModerationQueue(undefined, 100),
    ])
    if (results[0].status === "fulfilled") setDashboard(results[0].value)
    if (results[1].status === "fulfilled") setPipeline(results[1].value)
    if (results[2].status === "fulfilled") setQueue(results[2].value)
    const failed = results.find((result) => result.status === "rejected")
    if (failed?.status === "rejected") setError(failed.reason instanceof Error ? failed.reason.message : "Some platform data could not be loaded")
    setLoading(false)
  }, [user?.isAdmin])

  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) router.replace("/login?message=Admin+sign+in+required&redirect=%2Fadmin")
    else if (!user?.isAdmin) router.replace("/")
  }, [authLoading, isAuthenticated, router, user?.isAdmin])
  useEffect(() => { if (user?.isAdmin) load() }, [load, user?.isAdmin])

  const providers = useMemo(() => [
    ...(pipeline?.ingestion.providers ?? []).map((provider) => ({ ...provider, kind: "Ingestion" })),
    ...(pipeline?.generation.providers ?? []).map((provider) => ({ ...provider, kind: "Generation" })),
  ], [pipeline])

  async function moderate(poll: ApiPoll, status: "Published" | "Rejected") {
    const key = `poll-${poll.id}`; setAction(key); setError(null)
    try {
      await pollsApi.moderate(poll.id, { status, reason: status === "Rejected" ? "Rejected by platform administrator." : undefined })
      setQueue((items) => items.filter((item) => item.id !== poll.id))
      setDashboard((current) => current ? { ...current, stats: { ...current.stats, pendingReview: Math.max(0, current.stats.pendingReview - (poll.moderationStatus === "PendingReview" ? 1 : 0)), flaggedPolls: Math.max(0, current.stats.flaggedPolls - (poll.moderationStatus === "Flagged" ? 1 : 0)) } } : current)
      setNotice(status === "Published" ? "Poll published." : "Poll rejected.")
    } catch (err) { setError(err instanceof Error ? err.message : "Moderation action failed") }
    finally { setAction(null) }
  }

  async function toggleGeneration() {
    if (!pipeline) return
    setAction("generation"); setError(null)
    try { await adminApi.setGenerationPaused(!pipeline.generation.paused); await load(); setNotice(`Generation ${pipeline.generation.paused ? "resumed" : "paused"}.`) }
    catch (err) { setError(err instanceof Error ? err.message : "Could not update generation") }
    finally { setAction(null) }
  }

  async function runSource(source: string) {
    setAction(source); setError(null)
    try { const result = await adminApi.runIngestion(source); setNotice(`${source.toUpperCase()} ingestion queued as job ${result.jobId}.`) }
    catch (err) { setError(err instanceof Error ? err.message : "Could not queue ingestion") }
    finally { setAction(null) }
  }

  async function retryGeneration() {
    setAction("retry"); setError(null)
    try { const result = await adminApi.retryGeneration(10); setNotice(`Generation job ${result.jobId} queued; ${result.requeued} topics requeued.`); await load() }
    catch (err) { setError(err instanceof Error ? err.message : "Could not retry generation") }
    finally { setAction(null) }
  }

  if (authLoading || !user?.isAdmin) return <div className="flex min-h-screen items-center justify-center bg-background"><Loader2 className="h-7 w-7 animate-spin text-primary" /></div>

  const stats = dashboard?.stats
  const cards = [
    ["Users", stats?.totalUsers ?? 0, Users], ["Active polls", stats?.activePolls ?? 0, Vote],
    ["Votes", stats?.totalVotes ?? 0, BarChart3], ["Businesses", stats?.businessAccounts ?? 0, BriefcaseBusiness],
    ["Sponsored", stats?.sponsoredPolls ?? 0, Sparkles], ["Needs review", (stats?.pendingReview ?? 0) + (stats?.flaggedPolls ?? 0), ShieldCheck],
  ] as const

  return <div className="min-h-screen bg-background">
    <header className="sticky top-0 z-30 border-b border-border bg-card/95 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
        <div><p className="flex items-center gap-2 text-lg font-extrabold"><ShieldCheck className="h-5 w-5 text-primary" />Pop Platform</p><p className="text-xs text-muted-foreground">Administrator control center</p></div>
        <div className="flex items-center gap-3"><span className="hidden text-sm text-muted-foreground sm:inline">{user.displayName}</span><Button variant="outline" onClick={() => { logout(); router.replace("/login") }}><LogOut className="mr-2 h-4 w-4" />Sign out</Button></div>
      </div>
    </header>
    <main className="mx-auto max-w-7xl px-4 py-6">
      <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
        <div><h1 className="text-3xl font-bold">Platform administration</h1><p className="mt-1 text-muted-foreground">Monitor activity, review content, and operate the poll pipeline.</p></div>
        <Button variant="outline" onClick={load} disabled={loading}><RefreshCw className={cn("mr-2 h-4 w-4", loading && "animate-spin")} />Refresh</Button>
      </div>
      {(error || notice) && <div className={cn("mb-5 rounded-xl border px-4 py-3 text-sm", error ? "border-destructive/30 bg-destructive/10 text-destructive" : "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300")}>{error ?? notice}</div>}
      <nav className="mb-6 flex gap-2 overflow-x-auto rounded-xl border bg-card p-1.5">{(["overview", "moderation", "pipeline"] as Tab[]).map((item) => <button className={cn("rounded-lg px-4 py-2 text-sm font-semibold capitalize transition", tab === item ? "bg-primary text-primary-foreground shadow-sm" : "text-muted-foreground hover:bg-secondary")} key={item} onClick={() => setTab(item)}>{item}{item === "moderation" && queue.length > 0 ? ` (${queue.length})` : ""}</button>)}</nav>

      {tab === "overview" && <div className="space-y-6">
        <section className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{cards.map(([label, value, Icon]) => <article className="rounded-2xl border bg-card p-5 shadow-sm" key={label}><div className="mb-4 flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary"><Icon className="h-5 w-5" /></div><p className="text-3xl font-bold">{Number(value).toLocaleString()}</p><p className="text-sm text-muted-foreground">{label}</p></article>)}</section>
        <section className="grid gap-5 lg:grid-cols-2">
          <Panel title="Recent polls" icon={Activity}>{dashboard?.recentPolls?.slice(0, 5).map((poll) => <PollRow key={poll.id} poll={poll} />)}</Panel>
          <Panel title="Trending polls" icon={BarChart3}>{dashboard?.trendingPolls?.slice(0, 5).map((poll) => <PollRow key={poll.id} poll={poll} />)}</Panel>
        </section>
      </div>}

      {tab === "moderation" && <section className="space-y-4">{loading ? <Loading /> : queue.length === 0 ? <Empty text="No polls are waiting for review." /> : queue.map((poll) => <article className="rounded-2xl border bg-card p-5" key={poll.id}><div className="flex flex-col justify-between gap-4 md:flex-row"><div className="min-w-0"><div className="mb-2 flex flex-wrap gap-2"><Badge text={poll.moderationStatus} warning={poll.moderationStatus === "Flagged"} /><Badge text={poll.category} />{poll.reportCount > 0 && <Badge text={`${poll.reportCount} reports`} warning />}</div><h2 className="text-lg font-bold">{poll.question}</h2><p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{poll.description}</p><p className="mt-3 text-xs text-muted-foreground">#{poll.id} · {poll.isAIGenerated ? "AI generated" : poll.isSponsored ? "Sponsored" : "User created"} · {new Date(poll.createdAt).toLocaleString()}</p></div><div className="flex shrink-0 gap-2"><Button disabled={action === `poll-${poll.id}`} onClick={() => moderate(poll, "Published")}><CheckCircle2 className="mr-2 h-4 w-4" />Publish</Button><Button disabled={action === `poll-${poll.id}`} variant="outline" onClick={() => moderate(poll, "Rejected")}><XCircle className="mr-2 h-4 w-4" />Reject</Button></div></div></article>)}</section>}

      {tab === "pipeline" && <div className="space-y-6">
        <section className="grid gap-4 md:grid-cols-3"><article className="rounded-2xl border bg-card p-5"><p className="text-sm text-muted-foreground">Generation</p><p className="mt-2 flex items-center gap-2 text-xl font-bold">{pipeline?.generation.paused ? <><Pause className="text-amber-500" />Paused</> : <><Play className="text-emerald-500" />Running</>}</p><Button className="mt-5 w-full" variant="outline" disabled={action === "generation"} onClick={toggleGeneration}>{pipeline?.generation.paused ? "Resume generation" : "Pause generation"}</Button></article><article className="rounded-2xl border bg-card p-5"><p className="text-sm text-muted-foreground">Queued topics</p><p className="mt-2 text-3xl font-bold">{pipeline?.generation.backlog.queued ?? 0}</p><p className="mt-1 text-sm text-muted-foreground">{pipeline?.generation.backlog.retryPending ?? 0} waiting for retry</p><Button className="mt-5 w-full" disabled={action === "retry"} onClick={retryGeneration}><Bot className="mr-2 h-4 w-4" />Generate next 10</Button></article><article className="rounded-2xl border bg-card p-5"><p className="text-sm text-muted-foreground">Provider health</p><p className="mt-2 text-3xl font-bold">{providers.filter((p) => p.state === "Enabled").length}/{providers.length}</p><p className="mt-1 text-sm text-muted-foreground">providers operational</p></article></section>
        <Panel title="Ingestion sources" icon={Database}><div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">{sources.map((source) => { const health=pipeline?.ingestion.providers.find((item) => item.provider.toLowerCase() === source); return <div className="rounded-xl border p-4" key={source}><div className="flex items-center justify-between"><span className="font-bold uppercase">{source}</span><State value={health?.state ?? "Disabled"} /></div><p className="mt-2 min-h-5 text-xs text-muted-foreground">{health?.reason ?? (health?.lastSuccess ? `Last success ${new Date(health.lastSuccess).toLocaleString()}` : "Ready")}</p><Button className="mt-3 w-full" size="sm" variant="outline" disabled={action === source || health?.state === "Misconfigured" || health?.state === "Disabled"} onClick={() => runSource(source)}>{action === source ? <Loader2 className="h-4 w-4 animate-spin" /> : "Run now"}</Button></div> })}</div></Panel>
        <Panel title="AI generation providers" icon={Bot}><div className="divide-y">{pipeline?.generation.providers.map((provider) => <div className="flex items-center justify-between gap-4 py-3" key={provider.provider}><div><p className="font-semibold capitalize">{provider.provider}</p><p className="text-xs text-muted-foreground">{provider.reason ?? (provider.lastSuccess ? `Last success ${new Date(provider.lastSuccess).toLocaleString()}` : "Configured")}</p></div><State value={provider.state} /></div>)}</div></Panel>
      </div>}
    </main>
  </div>
}

function Panel({ title, icon: Icon, children }: { title: string; icon: typeof Activity; children: React.ReactNode }) { return <section className="rounded-2xl border bg-card p-5 shadow-sm"><h2 className="mb-4 flex items-center gap-2 text-lg font-bold"><Icon className="h-5 w-5 text-primary" />{title}</h2>{children}</section> }
function PollRow({ poll }: { poll: ApiPoll }) { return <div className="border-b py-3 last:border-0"><div className="flex items-start justify-between gap-3"><p className="line-clamp-2 text-sm font-semibold">{poll.question}</p><span className="shrink-0 text-xs text-muted-foreground">{poll.totalVotes.toLocaleString()} votes</span></div><p className="mt-1 text-xs text-muted-foreground">{poll.category} · {poll.moderationStatus}</p></div> }
function Badge({ text, warning=false }: { text: string; warning?: boolean }) { return <span className={cn("rounded-full px-2.5 py-1 text-xs font-semibold", warning ? "bg-amber-500/15 text-amber-700 dark:text-amber-300" : "bg-secondary text-secondary-foreground")}>{text}</span> }
function State({ value }: { value: string }) { const ok=value === "Enabled"; const bad=value === "Failed" || value === "Misconfigured"; return <span className={cn("inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold", ok ? "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300" : bad ? "bg-destructive/10 text-destructive" : "bg-amber-500/15 text-amber-700 dark:text-amber-300")}>{bad ? <AlertTriangle className="h-3 w-3" /> : ok ? <CheckCircle2 className="h-3 w-3" /> : <Flag className="h-3 w-3" />}{value}</span> }
function Loading() { return <div className="flex justify-center py-16"><Loader2 className="h-6 w-6 animate-spin text-primary" /></div> }
function Empty({ text }: { text: string }) { return <div className="rounded-2xl border border-dashed py-16 text-center text-muted-foreground">{text}</div> }
