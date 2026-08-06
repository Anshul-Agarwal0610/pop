"use client"
import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { AchievementCard } from "@/components/achievements/achievement-card"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { achievementsApi, type ApiAchievementCollection } from "@/lib/api"
import { ResultCardCollection } from "@/components/result-cards/result-card-collection"

const categories = ["All","Voting","Streak","Challenge","Exploration"] as const
export default function AchievementsPage() {
  const { isAuthenticated,isLoading:authLoading }=useAuth(); const router=useRouter()
  const [data,setData]=useState<ApiAchievementCollection|null>(null); const [category,setCategory]=useState<(typeof categories)[number]>("All")
  const [loading,setLoading]=useState(true); const [error,setError]=useState<string|null>(null); const [reload,setReload]=useState(0)
  useEffect(()=>{ if(!authLoading&&!isAuthenticated) router.replace("/login?returnUrl=%2Fachievements") },[authLoading,isAuthenticated,router])
  useEffect(()=>{ if(!isAuthenticated)return; achievementsApi.getMine().then(setData).catch((e:Error)=>setError(e.message)).finally(()=>setLoading(false)) },[isAuthenticated,reload])
  const visible=useMemo(()=>data?.achievements.filter(x=>category==="All"||x.category===category)??[],[data,category])
  async function selectTitle(value:string) { if(!value) await achievementsApi.clearTitle(); else await achievementsApi.selectTitle(Number(value)); setReload(x=>x+1) }
  return <AppShell><main className="mx-auto max-w-4xl px-4 py-6"><h1 className="text-3xl font-bold">Achievements</h1><p className="mt-1 text-muted-foreground">Collect badges, track goals, and choose a title.</p>
    {loading&&<p className="mt-8" role="status">Loading achievements…</p>}
    {error&&<div className="mt-8 rounded-xl bg-destructive/10 p-4" role="alert">Could not load achievements. <Button className="ml-2" onClick={()=>setReload(x=>x+1)} size="sm">Retry</Button></div>}
    {!loading&&!error&&data&&<><div className="mt-6 flex flex-wrap items-center gap-3"><strong>{data.earnedCount} of {data.totalCount} earned</strong>
      <label className="ml-auto text-sm">Profile title <select className="ml-2 rounded-md border bg-background p-2" value={data.selectedTitleBadgeId??""} onChange={e=>void selectTitle(e.target.value)}><option value="">No title</option>{data.achievements.filter(x=>x.status==="earned"&&x.rewardTitle).map(x=><option key={x.badgeId} value={x.badgeId}>{x.rewardTitle}</option>)}</select></label></div>
      <div className="mt-6 flex flex-wrap gap-2" role="tablist" aria-label="Achievement categories">{categories.map(x=><Button key={x} role="tab" aria-selected={category===x} variant={category===x?"default":"outline"} onClick={()=>setCategory(x)}>{x}</Button>)}</div>
      {visible.length===0?<p className="mt-8 rounded-xl bg-card p-8 text-center">No achievements in this category.</p>:<div className="mt-6 grid gap-4 sm:grid-cols-2">{visible.map(x=><AchievementCard key={x.badgeId} achievement={x}/>)}</div>}</>}
    <section className="mt-12" aria-labelledby="memories-heading"><h2 id="memories-heading" className="text-2xl font-bold">Multiplayer memories</h2><p className="mb-5 mt-1 text-muted-foreground">Your shareable Clash, Relay, and Room moments.</p><ResultCardCollection /></section>
  </main></AppShell>
}
