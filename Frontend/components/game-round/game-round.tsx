"use client"
import { useCallback, useEffect, useRef, useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { ApiError, gameSessionsApi, type ApiGameSession } from "@/lib/api"
import { CompletionSummary } from "./completion-summary"
import { RoundProgress } from "./round-progress"
import { RoundTimer } from "./round-timer"

const stateText:Record<string,string>={expired:"This round expired. Start a new round from the Game Hub.",poll_unavailable:"A poll in this round is no longer available, so the round was closed.",not_found:"This round is unavailable."}
export function GameRound({ id }: { id: string }) {
  const router=useRouter();const [session,setSession]=useState<ApiGameSession|null>(null);const [error,setError]=useState<string|null>(null);const [pending,setPending]=useState(false);const heading=useRef<HTMLHeadingElement>(null)
  const load=useCallback(()=>gameSessionsApi.get(id).then(setSession).catch((e:ApiError)=>setError(stateText[e.code??""]??e.message)),[id])
  useEffect(()=>{load()},[load])
  useEffect(()=>{if(session?.currentPosition)heading.current?.focus()},[session?.currentPosition])
  const expire=useCallback(()=>setSession(s=>s?{...s,status:"Expired"}:s),[])
  const vote=async(optionId:number)=>{if(!session?.currentPoll||pending)return;setPending(true);setError(null);try{const result=await gameSessionsApi.vote(session.id,session.currentPosition,session.currentPoll.id,optionId);setSession(result.session)}catch(e){const err=e as ApiError;setError(stateText[err.code??""]??err.message);if(err.code==="expired"||err.code==="poll_unavailable")setSession({...session,status:"Expired"})}finally{setPending(false)}}
  if(error&&!session)return <State message={error} onBack={()=>router.push("/games")}/>
  if(!session)return <p className="p-8 text-center" role="status">Loading round…</p>
  if(session.status==="Expired")return <State message={error??stateText.expired} onBack={()=>router.push("/games")}/>
  if(session.status==="Completed"&&session.summary)return <div className="mx-auto max-w-xl px-4 py-8"><CompletionSummary summary={session.summary}/><Button className="mt-5 w-full" onClick={()=>router.push("/games")}>Back to Game Hub</Button></div>
  const poll=session.currentPoll
  if(!poll)return <State message="The current poll is unavailable." onBack={()=>router.push("/games")}/>
  return <div className="mx-auto max-w-2xl px-4 py-8"><div className="flex items-center justify-between gap-4"><p className="font-bold">{session.category} · Opinion Sprint</p><RoundTimer expiresAt={session.expiresAt} serverNow={session.serverNow} onExpire={expire}/></div><div className="mt-5"><RoundProgress current={session.currentPosition} total={session.pollCount}/></div>
    <section className="mt-8 rounded-3xl bg-card p-6 ring-1 ring-border"><h1 ref={heading} tabIndex={-1} className="text-2xl font-bold outline-none focus-visible:ring-2 focus-visible:ring-ring">{poll.question}</h1>{poll.description&&<p className="mt-2 text-muted-foreground">{poll.description}</p>}<p className="sr-only" aria-live="polite">Poll {session.currentPosition+1} of {session.pollCount}. {session.remainingPolls} remaining.</p>
    <fieldset disabled={pending} className="mt-6 space-y-3"><legend className="sr-only">Choose your opinion</legend>{poll.options.map(option=><button key={option.id} type="button" onClick={()=>vote(option.id)} className="min-h-12 w-full touch-manipulation rounded-xl border bg-background px-4 py-3 text-left font-medium hover:border-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-60">{option.text}</button>)}</fieldset>{pending&&<p className="mt-3 text-sm" role="status">Submitting your vote…</p>}{error&&<p role="alert" className="mt-3 text-sm text-destructive">{error}</p>}</section>
  </div>
}
function State({message,onBack}:{message:string;onBack:()=>void}){return <div className="mx-auto max-w-lg px-4 py-16 text-center"><h1 className="text-2xl font-bold">Round unavailable</h1><p className="mt-3 text-muted-foreground">{message}</p><Button className="mt-6" onClick={onBack}>Back to Game Hub</Button></div>}
