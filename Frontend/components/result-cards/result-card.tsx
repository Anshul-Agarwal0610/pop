import type { ApiResultCard } from "@/lib/api"

export function ResultCard({ card }: { card: ApiResultCard }) {
  const { payload } = card
  return <article aria-label={payload.accessibleSummary} className="min-w-0 overflow-hidden rounded-2xl border bg-gradient-to-br from-violet-950 to-slate-950 p-5 text-white shadow-lg">
    <p className="text-sm font-semibold uppercase tracking-widest text-violet-300">PoP Live · {payload.mode}</p>
    <h3 className="mt-3 break-words text-2xl font-bold">{payload.aggregateResult}</h3>
    {payload.milestone && <p className="mt-2 text-violet-100">{payload.milestone}</p>}
    {payload.badge && <p className="mt-4 rounded-full bg-white/10 px-3 py-2 text-sm"><span aria-hidden="true">{payload.badge.icon} </span>Badge earned: {payload.badge.name}</p>}
    <p className="mt-4 text-sm text-violet-200">{payload.participantCount} participants</p>
    <ul className="mt-2 flex flex-wrap gap-2" aria-label="Participants">{payload.participants.map((p, i) => <li className="rounded-full bg-white/10 px-3 py-1 text-sm" key={`${p.label}-${i}`}>{p.label}</li>)}</ul>
  </article>
}
