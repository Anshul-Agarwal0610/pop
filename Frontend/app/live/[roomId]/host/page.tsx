"use client"
import { useCallback } from "react"
import { useParams } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { liveRoomsApi } from "@/lib/api"
import { useLiveRoom } from "@/hooks/use-live-room"
import { JoinCodeCard } from "@/components/live-room/join-code-card"
import { HostControls } from "@/components/live-room/host-controls"
import { ParticipantRoster } from "@/components/live-room/participant-roster"
import { AggregateResults } from "@/components/live-room/aggregate-results"
import { ConnectionStatus } from "@/components/live-room/connection-status"

export default function HostPage() {
  const id = useParams<{roomId:string}>().roomId
  const load = useCallback(() => liveRoomsApi.host(id), [id])
  const { snapshot, connected, refresh } = useLiveRoom(id, "host", load)
  if (!snapshot) return <p>Loading room…</p>
  const origin = typeof window !== "undefined" ? window.location.origin : ""
  return <AppShell><div className="mx-auto max-w-4xl space-y-6 p-6">
    <div className="flex justify-between"><h1 className="text-3xl font-bold">Host room</h1><ConnectionStatus connected={connected}/></div>
    <JoinCodeCard code={snapshot.code} url={`${origin}/live/join?code=${snapshot.code}`}/>
    <a className="underline" target="_blank" href={`/live/${id}/display?capability=${encodeURIComponent(snapshot.displayToken)}`}>Open shared display</a>
    <HostControls status={snapshot.status} round={snapshot.round} onCommand={async command => { await liveRoomsApi.command(id, command); await refresh() }}/>
    {snapshot.round && <section><h2 className="text-xl font-bold">{snapshot.round.proposition}</h2><AggregateResults round={snapshot.round}/></section>}
    <ParticipantRoster participants={snapshot.participants} onRemove={async participantId => { await liveRoomsApi.remove(id, participantId); await refresh() }}/>
  </div></AppShell>
}
