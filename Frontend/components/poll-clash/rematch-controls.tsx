"use client"
import type { ApiPollClash } from "@/lib/api"
import { Button } from "@/components/ui/button"
export function RematchControls({clash,pending,onRequest,onAccept}:{clash:ApiPollClash;pending:boolean;onRequest:()=>void;onAccept:(id:number)=>void}){const me=clash.players.find(p=>p.isViewer);if(!clash.rematch)return <Button disabled={pending} onClick={onRequest}>Request rematch</Button>;if(clash.rematch.status!=="Pending")return <p>Rematch {clash.rematch.status.toLowerCase()}.</p>;if(clash.rematch.requestedByUserId===me?.userId)return <p className="text-sm text-muted-foreground">Rematch requested — waiting for your opponent.</p>;return <Button disabled={pending} onClick={()=>onAccept(clash.rematch!.id)}>Accept rematch</Button>}
