"use client"

import { useState } from "react"
import { useLiveSession } from "@/hooks/use-live-session"
import { Button } from "@/components/ui/button"

export function PollBombRoom({ publicId }: { publicId: string }) {
  const {state,error,vote,setNotifications}=useLiveSession(publicId); const [pending,setPending]=useState(false)
  if(error&&!state)return <p role="alert" className="text-destructive">{error}</p>
  if(!state)return <p>Loading Poll Bomb…</p>
  const revealed=state.status==="Revealed"
  return <section className="space-y-6 rounded-3xl bg-card p-6 ring-1 ring-border"><header><p className="text-sm font-bold text-primary">POLL BOMB · {state.status.toUpperCase()}</p><h1 className="mt-2 text-3xl font-black">{state.poll.question}</h1></header>
    <div className="grid grid-cols-3 gap-3 text-center"><div className="rounded-xl bg-muted p-3"><b>{state.joinedCount}</b><small className="block">joined</small></div><div className="rounded-xl bg-muted p-3"><b>{state.lockedCount}</b><small className="block">locked</small></div><div className="rounded-xl bg-muted p-3"><b>{state.targetVotes}</b><small className="block">target</small></div></div>
    {state.status==="Expired"?<div className="rounded-xl bg-muted p-5"><h2 className="font-bold">Expired without reveal</h2><p className="text-sm text-muted-foreground">The target was not reached, so all choices remain private.</p></div>:<div className="space-y-3">{state.poll.options.map(o=><Button key={o.id} variant="outline" className="h-auto w-full justify-between p-4" disabled={state.hasLockedVote||revealed||pending} onClick={async()=>{setPending(true);try{await vote(o.id)}finally{setPending(false)}}}><span>{o.text}</span>{revealed&&<b>{o.voteCount} votes</b>}</Button>)}</div>}
    {state.hasLockedVote&&!revealed&&<p className="rounded-xl bg-primary/10 p-4 font-semibold">Vote locked. {state.remainingVotes} more needed for the shared reveal.</p>}
    {state.status==="Voting"&&<label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={state.notificationsEnabled} onChange={e=>void setNotifications(e.target.checked)} />Poll Bomb reminders</label>}
  </section>
}
