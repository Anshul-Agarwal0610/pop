"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { liveSessionsApi, type ApiLiveSessionMode } from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

export function PollBombCreate() {
  const router=useRouter(); const [mode,setMode]=useState<ApiLiveSessionMode|null>(null)
  const [pollId,setPollId]=useState(""); const [target,setTarget]=useState(0); const [duration,setDuration]=useState(0)
  const [notify,setNotify]=useState(false); const [error,setError]=useState<string|null>(null); const [pending,setPending]=useState(false)
  useEffect(()=>{liveSessionsApi.modes().then(([m])=>{setMode(m);setTarget(m.allowedThresholds[0]);setDuration(m.allowedDurationsSeconds[0])}).catch(e=>setError(e.message))},[])
  const create=async()=>{setPending(true);setError(null);try{const s=await liveSessionsApi.create({pollId:Number(pollId),targetVotes:target,durationSeconds:duration,notificationsEnabled:notify});router.push(`/live/${s.publicId}`)}catch(e){setError(e instanceof Error?e.message:"Could not create Poll Bomb")}finally{setPending(false)}}
  return <section className="space-y-5 rounded-3xl bg-card p-6 ring-1 ring-border"><div><h1 className="text-3xl font-black">Create a Poll Bomb</h1><p className="text-muted-foreground">Results stay sealed until the target is reached.</p></div>
    <label className="block text-sm font-semibold">Poll ID<Input className="mt-2" inputMode="numeric" value={pollId} onChange={e=>setPollId(e.target.value)} /></label>
    {mode&&<><label className="block text-sm font-semibold">Votes needed<select aria-label="Votes needed" className="mt-2 w-full rounded-md border bg-background p-2" value={target} onChange={e=>setTarget(Number(e.target.value))}>{mode.allowedThresholds.map(v=><option key={v} value={v}>{v}</option>)}</select></label>
    <label className="block text-sm font-semibold">Duration<select aria-label="Duration" className="mt-2 w-full rounded-md border bg-background p-2" value={duration} onChange={e=>setDuration(Number(e.target.value))}>{mode.allowedDurationsSeconds.map(v=><option key={v} value={v}>{v<3600?`${v/60} minutes`:`${v/3600} hours`}</option>)}</select></label></>}
    <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={notify} onChange={e=>setNotify(e.target.checked)} />Send me reminders (off by default)</label>
    {error&&<p role="alert" className="text-destructive">{error}</p>}<Button disabled={!mode||!pollId||pending} onClick={create}>{pending?"Creating…":"Arm Poll Bomb"}</Button></section>
}
