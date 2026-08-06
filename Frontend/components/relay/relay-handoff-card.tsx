"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { Clock, Link2, Loader2, Users } from "lucide-react"
import { ApiError, type ApiRelayComplete, type ApiRelayHandoff, relaysApi } from "@/lib/api"
import { useAuth } from "@/contexts/auth-context"
import { appBaseUrl } from "@/lib/share"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Progress } from "@/components/ui/progress"
import { Switch } from "@/components/ui/switch"

export function RelayHandoffCard({ token }: { token: string }) {
  const router=useRouter(); const {isAuthenticated,isLoading}=useAuth()
  const [handoff,setHandoff]=useState<ApiRelayHandoff|null>(null)
  const [complete,setComplete]=useState<ApiRelayComplete|null>(null)
  const [consent,setConsent]=useState(false); const [busy,setBusy]=useState(false)
  const [error,setError]=useState<string|null>(null); const [now,setNow]=useState(Date.now())
  useEffect(()=>{relaysApi.handoff(token).then(setHandoff).catch(e=>setError(e instanceof Error?e.message:"Could not load handoff"))},[token])
  useEffect(()=>{const id=setInterval(()=>setNow(Date.now()),1000);return()=>clearInterval(id)},[])
  const expired=handoff ? now>=new Date(handoff.expiresAt).getTime():false
  const deadline=handoff ? new Date(handoff.expiresAt).toLocaleString(undefined,{dateStyle:"medium",timeStyle:"short"}):""
  const remaining=handoff?Math.max(0,new Date(handoff.expiresAt).getTime()-now):0
  const countdown=useMemo(()=>{const m=Math.floor(remaining/60000),s=Math.floor((remaining%60000)/1000);return `${Math.floor(m/60)}h ${m%60}m ${s}s`},[remaining])
  async function accept(){if(!isAuthenticated){router.push(`/login?message=${encodeURIComponent("Sign in to accept this relay")}&redirect=${encodeURIComponent(`/relay/${token}`)}`);return}setBusy(true);setError(null);try{await relaysApi.accept(token);setHandoff(await relaysApi.handoff(token))}catch(e){setError(message(e))}finally{setBusy(false)}}
  async function vote(optionId:number){if(!window.confirm("Lock this choice? Relay votes cannot be changed."))return;setBusy(true);setError(null);try{setComplete(await relaysApi.complete(token,optionId,consent))}catch(e){setError(message(e))}finally{setBusy(false)}}
  async function share(){if(!complete?.handoffToken)return;const url=`${appBaseUrl()}/relay/${complete.handoffToken}`;try{if(navigator.share)await navigator.share({title:"Continue this Pollify Relay",url});else await navigator.clipboard.writeText(url)}catch(e){if((e as DOMException).name!=="AbortError")setError("Could not share the handoff. You can copy the link instead.")}}
  if(!handoff&&!error)return <div className="flex justify-center p-12"><Loader2 aria-label="Loading relay" className="animate-spin"/></div>
  if(!handoff)return <p role="alert" className="p-6 text-destructive">{error}</p>
  const length=complete?.chainLength??handoff.chainLength;const milestone=complete?.nextMilestone??handoff.nextMilestone
  return <Card className="mx-auto w-full max-w-xl">
    <CardHeader><div className="mb-2 flex items-center gap-2 text-sm font-medium text-primary"><Link2 className="h-4 w-4"/>Poll Relay</div><CardTitle className="text-2xl">{handoff.question}</CardTitle></CardHeader>
    <CardContent className="space-y-6">
      <div className="grid grid-cols-2 gap-3 text-sm"><div className="rounded-xl bg-secondary p-3"><Users className="mb-1 h-4 w-4"/><strong>{length}</strong> completed</div><div className="rounded-xl bg-secondary p-3"><Clock className="mb-1 h-4 w-4"/><span aria-live="polite">{expired?"Expired":countdown}</span></div></div>
      <p className="text-sm text-muted-foreground">Deadline: <time dateTime={handoff.expiresAt}>{deadline}</time>. The server deadline is authoritative.</p>
      {milestone&&<div><div className="mb-2 flex justify-between text-sm"><span>Next milestone</span><span>{length} / {milestone}</span></div><Progress aria-label={`Progress to ${milestone} participant milestone`} value={Math.min(100,length/milestone*100)}/></div>}
      <p className="text-sm text-muted-foreground">Choices stay private. Chain members see progress only; aggregate results appear after the relay ends to members who opt in.</p>
      {error&&<p role="alert" className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</p>}
      {complete ? <div className="space-y-3"><p className="font-semibold">Your vote is locked.</p>{complete.status==="Completed"?<p>This relay has reached its final outcome.</p>:<Button className="w-full" onClick={share}>Pass relay to one person</Button>}{complete.rewardCapped&&<p className="text-sm text-muted-foreground">The transfer succeeded. Your relay XP cap has been reached.</p>}</div>
      : !handoff.isAcceptedByCurrentUser ? <Button className="w-full" disabled={busy||expired||isLoading} onClick={accept}>{busy?<Loader2 className="animate-spin"/>:"Accept handoff"}</Button>
      : <div className="space-y-4"><div className="flex items-center justify-between rounded-xl border p-3"><label htmlFor="relay-consent">Send me the final aggregate outcome</label><Switch id="relay-consent" checked={consent} onCheckedChange={setConsent}/></div><div className="grid grid-cols-2 gap-3">{handoff.options.slice(0,2).map((o,i)=><Button key={o.id} disabled={busy||expired} className={i===0?"bg-emerald-600 hover:bg-emerald-700":"bg-red-600 hover:bg-red-700"} onClick={()=>vote(o.id)}>{o.text}</Button>)}</div><p className="text-center text-xs text-muted-foreground">You will confirm before your choice is permanently locked.</p></div>}
    </CardContent></Card>
}
function message(error:unknown){if(error instanceof ApiError){const labels:Record<string,string>={handoff_expired:"This handoff expired before it could be completed.",handoff_replayed:"Someone already used this single-use handoff.",relay_branch_conflict:"This chain already has its next participant.",relay_cycle_detected:"You already participated in this chain.",relay_blocked:"This transfer is unavailable."};return error.code&&labels[error.code]||error.message}return error instanceof Error?error.message:"Relay action failed"}
