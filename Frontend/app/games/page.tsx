"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { GameModeCard } from "@/components/game-hub/game-mode-card"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import { useAuth } from "@/contexts/auth-context"
import { ApiError, gameSessionsApi, type ApiGameMode, type ApiGameSession } from "@/lib/api"

export default function GamesPage() {
  const router = useRouter(); const { isAuthenticated, isLoading } = useAuth()
  const [modes,setModes]=useState<ApiGameMode[]>([]); const [active,setActive]=useState<ApiGameSession|null>(null); const [pending,setPending]=useState(false); const [error,setError]=useState<string|null>(null)
  useEffect(()=>{ if (!isAuthenticated) return; Promise.all([gameSessionsApi.modes(),gameSessionsApi.active()]).then(([m,a])=>{setModes(m);setActive(a)}).catch((e:Error)=>setError(e.message)) },[isAuthenticated])
  const start=async()=>{setPending(true);setError(null);try{const s=await gameSessionsApi.start();router.push(`/games/${s.id}`)}catch(e){setError(e instanceof ApiError && e.code==="insufficient_content" ? "There are not enough fresh General polls for a round yet." : (e as Error).message)}finally{setPending(false)}}
  return <AppShell><div className="mx-auto max-w-3xl px-4 py-8"><h1 className="text-3xl font-black">Game Hub</h1><p className="mt-1 text-muted-foreground">Short poll rounds. Your opinions are never graded as right or wrong.</p>
    {!isLoading&&!isAuthenticated&&<div className="mt-8 rounded-2xl bg-card p-8 text-center ring-1 ring-border"><p>Sign in to start or resume a round.</p><Button className="mt-4" onClick={()=>router.push("/login")}>Sign in</Button></div>}
    {active&&<section className="mt-8 rounded-2xl bg-primary/10 p-5 ring-1 ring-primary/30"><h2 className="font-bold">Round in progress</h2><p className="text-sm text-muted-foreground">Poll {active.currentPosition+1} of {active.pollCount} · {active.remainingPolls} remaining</p><Button className="mt-3" onClick={()=>router.push(`/games/${active.id}`)}>Resume round</Button></section>}
    {error&&<p role="alert" className="mt-6 rounded-xl bg-destructive/10 p-4 text-destructive">{error}</p>}
    <div className="mt-8 space-y-5">{modes.map(m=><GameModeCard key={m.mode} mode={m} pending={pending} onStart={start}/>)}</div>
    <section className="mt-5 rounded-3xl bg-card p-6 ring-1 ring-border"><h2 className="text-xl font-bold">Poll Bomb</h2><p className="mt-1 text-sm text-muted-foreground">Invite a group and keep results locked until enough people vote.</p><Button asChild className="mt-5"><Link href="/live">Create Poll Bomb</Link></Button></section>
  </div></AppShell>
}
