"use client"
import { useEffect, useState } from "react"
import { Loader2, Target } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { ChallengeList } from "@/components/challenges/challenge-list"
import { useAuth } from "@/contexts/auth-context"
import { challengesApi, type ApiChallenge } from "@/lib/api"

export default function ChallengesPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [items, setItems] = useState<ApiChallenge[]>([]), [loading, setLoading] = useState(true), [error, setError] = useState<string | null>(null)
  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) { setLoading(false); return }
    challengesApi.getAll("all").then(setItems).catch((e: Error) => setError(e.message)).finally(() => setLoading(false))
  }, [authLoading, isAuthenticated])
  const active = items.filter(x => x.state === "Available" || x.state === "InProgress")
  return <AppShell><main className="mx-auto max-w-5xl px-4 py-8"><div className="mb-8 flex items-center gap-3"><Target className="h-8 w-8 text-primary" /><div><h1 className="text-3xl font-bold">Challenges</h1><p className="text-muted-foreground">Daily and weekly goals, calculated in UTC.</p></div></div>
    {loading ? <Loader2 className="mx-auto h-8 w-8 animate-spin" /> : !isAuthenticated ? <p>Sign in to see your challenges.</p> : error ? <p className="text-destructive">{error}</p> : <div className="space-y-10"><section><h2 className="mb-4 text-xl font-semibold">Active</h2><ChallengeList challenges={active} /></section><section><h2 className="mb-4 text-xl font-semibold">Completed</h2><ChallengeList challenges={items.filter(x => x.state === "Completed")} /></section><section><h2 className="mb-4 text-xl font-semibold">Expired</h2><ChallengeList challenges={items.filter(x => x.state === "Expired")} /></section></div>}
  </main></AppShell>
}
