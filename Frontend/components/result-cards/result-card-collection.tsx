"use client"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { resultCardsApi, type ApiResultCardPage } from "@/lib/api"
import { ResultCard } from "./result-card"
import { ResultCardActions } from "./result-card-actions"

export function ResultCardCollection() {
  const [data,setData]=useState<ApiResultCardPage|null>(null), [error,setError]=useState(false), [loading,setLoading]=useState(true), [reload,setReload]=useState(0)
  const load=(offset:number, append=false)=>{ setLoading(true); setError(false); resultCardsApi.getMine(offset).then(next=>setData(old=>append&&old?{...next,items:[...old.items,...next.items]}:next)).catch(()=>setError(true)).finally(()=>setLoading(false)) }
  useEffect(()=>load(0),[reload]) // eslint-disable-line react-hooks/exhaustive-deps
  if(error) return <div role="alert" className="rounded-xl border p-4">Could not load memories. <Button size="sm" onClick={()=>setReload(x=>x+1)}>Retry</Button></div>
  if(loading&&!data) return <p role="status">Loading memories…</p>
  if(!data?.items.length) return <p className="rounded-xl border p-6 text-center">Complete a Clash, Relay, or Room to collect your first memory.</p>
  return <><div className="grid gap-6 sm:grid-cols-2">{data.items.map(card=><div key={card.id}><ResultCard card={card}/><ResultCardActions card={card}/></div>)}</div>
    {data.hasMore&&<Button className="mt-5" disabled={loading} onClick={()=>load(data.offset+data.limit,true)}>{loading?"Loading…":"Load more"}</Button>}</>
}
