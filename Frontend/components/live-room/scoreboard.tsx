import type { LiveParticipant } from "@/lib/api"
export function Scoreboard({participants}:{participants:LiveParticipant[]}) { return <ol aria-label="Scoreboard">{participants.map(p=><li key={p.id} className="flex justify-between border-b py-2"><span>{p.displayName}</span><b>{p.score}</b></li>)}</ol> }
