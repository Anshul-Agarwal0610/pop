"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import Link from "next/link"
import { Loader2, Medal, Trophy } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { LeaderboardRow } from "@/components/leaderboard/leaderboard-row"
import { Button } from "@/components/ui/button"
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useAuth } from "@/contexts/auth-context"
import { usersApi, type ApiLeaderboardResponse, type LeaderboardPeriod } from "@/lib/api"

export default function LeaderboardPage() {
  const { user, isLoading: authLoading } = useAuth()
  const [period, setPeriod] = useState<LeaderboardPeriod>("weekly")
  const [data, setData] = useState<ApiLeaderboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestId = useRef(0)

  const load = useCallback(() => {
    const id = ++requestId.current
    setLoading(true); setError(null); setData(null)
    usersApi.getRankings(period, 20, 0)
      .then(result => { if (requestId.current === id) setData(result) })
      .catch((err: Error) => { if (requestId.current === id) setError(err.message) })
      .finally(() => { if (requestId.current === id) setLoading(false) })
  }, [period])

  useEffect(load, [load])
  const currentInPage = data?.rows.some(row => row.id === data.currentUser?.id) ?? false

  return <AppShell><div className="mx-auto max-w-3xl px-4 py-6">
    <div className="mb-5 flex items-center gap-3"><div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-500 text-white"><Trophy /></div><div><h1 className="text-2xl font-bold">Leaderboard</h1><p className="text-sm text-muted-foreground">Fair competition based on eligible XP earned</p></div></div>
    <Tabs value={period} onValueChange={value => setPeriod(value as LeaderboardPeriod)} className="mb-5">
      <TabsList className="grid w-full grid-cols-2"><TabsTrigger value="weekly">Weekly</TabsTrigger><TabsTrigger value="allTime">All Time</TabsTrigger></TabsList>
    </Tabs>
    {period === "weekly" && data?.nextResetAtUtc && <p className="mb-4 text-sm text-muted-foreground">Week resets Monday at 00:00 UTC · next reset {new Date(data.nextResetAtUtc).toLocaleString(undefined, { timeZone: "UTC", timeZoneName: "short" })}</p>}
    {!authLoading && !user && <div className="mb-4 rounded-xl bg-primary/10 p-3 text-sm">Sign in to see your position. <Button asChild variant="link" className="h-auto p-0"><Link href="/login">Sign in</Link></Button></div>}
    {loading && <div className="flex justify-center gap-2 rounded-2xl bg-card py-20 text-muted-foreground"><Loader2 className="animate-spin" />Loading {period === "weekly" ? "weekly" : "all-time"} rankings…</div>}
    {!loading && error && <div className="rounded-2xl bg-destructive/10 p-6 text-center"><p className="font-semibold text-destructive">Could not load leaderboard</p><p className="mt-1 text-sm text-muted-foreground">{error}</p><Button className="mt-4" variant="outline" onClick={load}>Retry</Button></div>}
    {!loading && !error && data?.rows.length === 0 && <div className="rounded-2xl bg-card p-10 text-center ring-1 ring-border/50"><Medal className="mx-auto h-12 w-12 text-muted-foreground"/><h2 className="mt-4 font-semibold">No {period === "weekly" ? "weekly" : "all-time"} rankings yet</h2><p className="text-sm text-muted-foreground">Earn eligible XP to appear here.</p></div>}
    {!loading && !error && data && <div className="space-y-3">{data.rows.map(row => <LeaderboardRow key={row.id} row={row} isYou={row.id === user?.id} />)}</div>}
    {!loading && !error && data?.currentUser && !currentInPage && <section className="mt-6 border-t pt-4"><h2 className="mb-2 text-sm font-semibold text-muted-foreground">Your position</h2><LeaderboardRow row={data.currentUser} isYou /></section>}
  </div></AppShell>
}
