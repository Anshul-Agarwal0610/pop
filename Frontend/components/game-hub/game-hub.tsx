"use client"

import { useCallback, useEffect, useState } from "react"
import Link from "next/link"
import { Award, CheckCircle2, Flame, Gamepad2, RefreshCw, Target, Trophy, Zap } from "lucide-react"
import { useAuth } from "@/contexts/auth-context"
import { achievementsApi, ApiError, challengesApi, usersApi, type ApiAchievementOverview, type ApiChallenge, type ApiProgression, type ApiWeeklyLeaderboardResponse } from "@/lib/api"
import { normalizeCategoryName } from "@/lib/categories"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { Skeleton } from "@/components/ui/skeleton"

type Section<T> = { data: T | null; loading: boolean; error: string | null }
const initial = <T,>(): Section<T> => ({ data: null, loading: true, error: null })

function LoadingCard() {
  return <Card aria-busy="true"><CardHeader><Skeleton className="h-6 w-40" /><Skeleton className="h-4 w-56 max-w-full" /></CardHeader><CardContent><Skeleton className="h-20 w-full" /></CardContent></Card>
}

function ErrorCard({ title, error, retry }: { title: string; error: string; retry: () => void }) {
  return <Alert variant="destructive" role="alert" className="h-full"><AlertTitle>{title}</AlertTitle><AlertDescription className="mt-2 break-words">{error}</AlertDescription><Button size="sm" variant="outline" onClick={retry} className="mt-4 gap-2"><RefreshCw className="h-4 w-4" />Retry</Button></Alert>
}

export function GameHub() {
  const { isLoading: authLoading, isAuthenticated, logout } = useAuth()
  const [challenge, setChallenge] = useState<Section<ApiChallenge[]>>(initial)
  const [progression, setProgression] = useState<Section<ApiProgression>>(initial)
  const [badges, setBadges] = useState<Section<ApiAchievementOverview>>(initial)
  const [leaderboard, setLeaderboard] = useState<Section<ApiWeeklyLeaderboardResponse>>(initial)

  const handleError = useCallback((error: unknown) => {
    if (error instanceof ApiError && error.status === 401) logout()
    return error instanceof Error ? error.message : "Something went wrong"
  }, [logout])

  const loadChallenges = useCallback(() => {
    setChallenge(s => ({ ...s, loading: true, error: null }))
    return challengesApi.getActive().then(data => setChallenge({ data, loading: false, error: null })).catch(e => setChallenge({ data: null, loading: false, error: handleError(e) }))
  }, [handleError])
  const loadProgression = useCallback(() => {
    setProgression(s => ({ ...s, loading: true, error: null }))
    return usersApi.getMyProgression().then(data => setProgression({ data, loading: false, error: null })).catch(e => setProgression({ data: null, loading: false, error: handleError(e) }))
  }, [handleError])
  const loadBadges = useCallback(() => {
    setBadges(s => ({ ...s, loading: true, error: null }))
    return achievementsApi.getMyOverview().then(data => setBadges({ data, loading: false, error: null })).catch(e => setBadges({ data: null, loading: false, error: handleError(e) }))
  }, [handleError])
  const loadLeaderboard = useCallback(() => {
    setLeaderboard(s => ({ ...s, loading: true, error: null }))
    return usersApi.getWeeklyLeaderboard(5).then(data => setLeaderboard({ data, loading: false, error: null })).catch(e => setLeaderboard({ data: null, loading: false, error: handleError(e) }))
  }, [handleError])

  useEffect(() => {
    if (!authLoading && isAuthenticated) void Promise.allSettled([loadChallenges(), loadProgression(), loadBadges(), loadLeaderboard()])
  }, [authLoading, isAuthenticated, loadChallenges, loadProgression, loadBadges, loadLeaderboard])

  if (authLoading) return <div className="mx-auto grid max-w-6xl gap-4 px-4 py-6 sm:grid-cols-2"><LoadingCard /><LoadingCard /><LoadingCard /><LoadingCard /></div>
  if (!isAuthenticated) return <div className="mx-auto flex min-h-[65vh] max-w-lg items-center px-4 py-10"><Card className="w-full text-center"><CardHeader><Gamepad2 className="mx-auto h-12 w-12 text-primary" /><CardTitle>Your Game Hub awaits</CardTitle><CardDescription>Sign in to see today&apos;s challenge, streak, badges, and weekly rank.</CardDescription></CardHeader><CardContent><Button asChild><Link href="/login?redirect=%2Fplay&message=Sign%20in%20to%20view%20your%20Game%20Hub">Sign in to play</Link></Button></CardContent></Card></div>

  const daily = challenge.data?.slice().sort((a, b) => new Date(a.endAt).getTime() - new Date(b.endAt).getTime())[0]
  const pollHref = daily?.category ? `/polls?category=${encodeURIComponent(normalizeCategoryName(daily.category))}` : "/polls"
  const currentRank = leaderboard.data?.currentUser

  return <div className="mx-auto max-w-6xl min-w-0 px-3 py-5 sm:px-5 sm:py-8">
    <header className="mb-6 min-w-0"><p className="text-sm font-semibold text-primary">TODAY IN POLLIFY</p><h1 className="break-words text-3xl font-black tracking-tight sm:text-4xl">Game Hub</h1><p className="mt-1 text-muted-foreground">Play today, keep your momentum, and see what comes next.</p></header>
    <div className="grid min-w-0 gap-4 lg:grid-cols-2">
      <section className="min-w-0 lg:row-span-2" aria-label="Daily challenge">
        {challenge.loading ? <LoadingCard /> : challenge.error ? <ErrorCard title="Could not load today's challenge" error={challenge.error} retry={loadChallenges} /> : !daily ? <Card className="h-full"><CardHeader><CardTitle>No challenge available</CardTitle><CardDescription>There is no active daily challenge right now.</CardDescription></CardHeader><CardContent><Button asChild><Link href="/polls">Browse polls</Link></Button></CardContent></Card> : <Card className="h-full overflow-hidden border-primary/20 bg-gradient-to-br from-primary/10 to-card"><CardHeader><div className="flex flex-wrap items-center justify-between gap-2"><span className="rounded-full bg-primary px-3 py-1 text-xs font-bold text-primary-foreground">DAILY CHALLENGE</span><span className="text-sm font-bold text-amber-500">+{daily.rewardXp} XP</span></div><CardTitle className="mt-4 break-words text-2xl">{daily.title}</CardTitle><CardDescription>{daily.category ? `Vote in ${normalizeCategoryName(daily.category)}` : "Vote on any eligible poll"}</CardDescription></CardHeader><CardContent className="space-y-5"><div><div className="mb-2 flex justify-between text-sm"><span>Progress</span><span className="font-bold">{daily.currentVotes} / {daily.requiredVotes}</span></div><Progress value={Math.min(100, Math.max(0, daily.requiredVotes ? daily.currentVotes / daily.requiredVotes * 100 : 100))} /></div>{daily.isCompleted ? <div className="flex items-center gap-2 rounded-xl bg-emerald-500/10 p-3 text-sm font-semibold text-emerald-600"><CheckCircle2 className="h-5 w-5" />Completed today{daily.rewardBadge ? ` · ${daily.rewardBadge} earned` : ""}</div> : <p className="text-sm text-muted-foreground">{Math.max(0, daily.requiredVotes - daily.currentVotes)} eligible vote{daily.requiredVotes - daily.currentVotes === 1 ? "" : "s"} to go</p>}<Button className="w-full" asChild><Link href={pollHref}>{daily.isCompleted ? "Keep playing" : "Play now"}</Link></Button></CardContent></Card>}
      </section>

      <section className="grid min-w-0 gap-4 sm:grid-cols-2" aria-label="Your progression">
        {progression.loading ? <><LoadingCard /><LoadingCard /></> : progression.error ? <div className="sm:col-span-2"><ErrorCard title="Could not load progression" error={progression.error} retry={loadProgression} /></div> : progression.data && <><Card className="min-w-0"><CardHeader><CardTitle className="flex items-center gap-2"><Flame className="h-5 w-5 text-orange-500" />{progression.data.streak} day streak</CardTitle><CardDescription>{progression.data.todayActivityComplete ? "Today's activity is complete" : progression.data.streak ? "Vote today to continue your streak" : "Cast a vote to start your streak"}</CardDescription></CardHeader></Card><Card className="min-w-0"><CardHeader><CardTitle className="flex items-center gap-2"><Zap className="h-5 w-5 text-amber-500" />Level {progression.data.level}</CardTitle><CardDescription>{progression.data.xp.toLocaleString()} total XP</CardDescription></CardHeader><CardContent><Progress value={progression.data.progressPercent} /><p className="mt-2 text-xs text-muted-foreground">{progression.data.xpIntoLevel} / {progression.data.xpRequiredForLevel} XP to level {progression.data.level + 1}</p></CardContent></Card></>}
      </section>

      <section className="min-w-0" aria-label="Achievements">
        {badges.loading ? <LoadingCard /> : badges.error ? <ErrorCard title="Could not load badges" error={badges.error} retry={loadBadges} /> : <Card className="h-full min-w-0"><CardHeader><CardTitle className="flex items-center gap-2"><Award className="h-5 w-5 text-violet-500" />Badges</CardTitle><CardDescription>{badges.data?.allEarned ? "Collection complete" : "Recently earned and within reach"}</CardDescription></CardHeader><CardContent className="space-y-4">{badges.data?.recentlyEarned.length ? <div><p className="mb-2 text-xs font-bold uppercase text-muted-foreground">Recently earned</p><div className="flex flex-wrap gap-2">{badges.data.recentlyEarned.map(b => <span key={b.id} className="rounded-full bg-amber-500/10 px-3 py-1 text-sm font-semibold">🏅 {b.name}</span>)}</div></div> : <p className="text-sm text-muted-foreground">No badges earned yet. Your first is close.</p>}{badges.data?.nextAchievable.map(b => <div key={b.badgeId} className="min-w-0 rounded-xl border p-3"><div className="flex min-w-0 justify-between gap-2 text-sm"><span className="truncate font-semibold">{b.name}</span><span className="shrink-0">{b.currentValue}/{b.threshold}</span></div><Progress className="mt-2" value={b.progressPercent} /></div>)}{!badges.data?.recentlyEarned.length && !badges.data?.nextAchievable.length && <p className="text-sm text-muted-foreground">Badge details are unavailable.</p>}</CardContent></Card>}
      </section>

      <section className="min-w-0 lg:col-start-2" aria-label="Weekly leaderboard">
        {leaderboard.loading ? <LoadingCard /> : leaderboard.error ? <ErrorCard title="Could not load weekly leaderboard" error={leaderboard.error} retry={loadLeaderboard} /> : <Card className="min-w-0"><CardHeader><CardTitle className="flex items-center gap-2"><Trophy className="h-5 w-5 text-amber-500" />Weekly leaderboard</CardTitle><CardDescription>{currentRank ? `You are #${currentRank.rank} with ${currentRank.score} ${leaderboard.data?.scoreUnit}` : "Cast a public vote to join this week's board"}</CardDescription></CardHeader><CardContent className="space-y-2">{leaderboard.data?.entries.length ? leaderboard.data.entries.slice(0, 3).map(entry => <div key={entry.userId} className="flex min-w-0 items-center gap-3 rounded-lg bg-secondary/50 px-3 py-2 text-sm"><span className="w-6 shrink-0 font-black">#{entry.rank}</span><span className="min-w-0 flex-1 truncate">{entry.displayName || entry.username}</span><span className="shrink-0 font-semibold">{entry.score} votes</span></div>) : <p className="text-sm text-muted-foreground">No qualifying activity this week yet.</p>}<Button asChild variant="outline" className="mt-3 w-full"><Link href="/leaderboard?view=weekly">View leaderboard</Link></Button></CardContent></Card>}
      </section>
    </div>
  </div>
}
