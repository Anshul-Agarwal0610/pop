"use client"

import { Clock3, Gamepad2, Gift, ListChecks } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { ApiGameMode } from "@/lib/api"

export function GameModeCard({ mode, pending, onStart }: { mode: ApiGameMode; pending: boolean; onStart: () => void }) {
  return <article className="rounded-3xl bg-card p-6 shadow-sm ring-1 ring-border/60">
    <div className="flex items-start gap-4"><span className="grid h-12 w-12 place-items-center rounded-2xl bg-primary text-primary-foreground"><Gamepad2 /></span><div><h2 className="text-xl font-bold">{mode.name}</h2><p className="text-sm text-muted-foreground">{mode.category} · opinion round</p></div></div>
    <dl className="mt-6 grid gap-3 text-sm sm:grid-cols-3">
      <div className="rounded-xl bg-muted p-3"><dt className="flex items-center gap-2 font-semibold"><ListChecks className="h-4 w-4"/>Polls</dt><dd>{mode.pollCount}</dd></div>
      <div className="rounded-xl bg-muted p-3"><dt className="flex items-center gap-2 font-semibold"><Clock3 className="h-4 w-4"/>Time</dt><dd>{mode.timeLimitSeconds ? `${mode.timeLimitSeconds / 60} minutes` : "Untimed"}</dd></div>
      <div className="rounded-xl bg-muted p-3"><dt className="flex items-center gap-2 font-semibold"><Gift className="h-4 w-4"/>Reward</dt><dd>{mode.completionXp} completion XP</dd></div>
    </dl>
    <h3 className="mt-5 font-semibold">Rules</h3><p className="mt-1 text-sm text-muted-foreground">{mode.rules}</p>
    <Button className="mt-6 min-h-11 w-full touch-manipulation" disabled={!mode.available || pending} onClick={onStart}>{pending ? "Starting…" : mode.available ? "Start round" : "Unavailable"}</Button>
  </article>
}
