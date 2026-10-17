import { cn } from "@/lib/utils"
import type { PollReaction, PollTemplate } from "@/lib/poll-creation"

export const CREATIVE_TEMPLATES: { id: PollTemplate; label: string; hint: string }[] = [
  { id: "classic", label: "Classic", hint: "Clear and calm" },
  { id: "pulse", label: "Pulse", hint: "Bright energy" },
  { id: "spotlight", label: "Spotlight", hint: "Bold contrast" },
]

export const POPMOJI: { id: PollReaction; label: string; face: string; color: string }[] = [
  { id: "big_yes", label: "Big yes", face: "⌣", color: "#FF6D7B" },
  { id: "thinking", label: "Thinking", face: "〰", color: "#AA89F8" },
  { id: "no_way", label: "No way", face: "—", color: "#6DBAD6" },
  { id: "wow", label: "Wow", face: "○", color: "#F3B45D" },
]

export function PoPmoji({ id, small = false }: { id: PollReaction | string; small?: boolean }) {
  const item = POPMOJI.find(value => value.id === id)
  if (!item) return null
  return <span role="img" aria-label={`PoPmoji: ${item.label}`} className={cn("relative inline-flex shrink-0 rotate-6 items-center justify-center rounded-full border-2 border-white/60 shadow-md", small ? "h-9 w-9" : "h-12 w-12")} style={{ backgroundColor: item.color }}>
    <span className="absolute left-[32%] top-[31%] h-[10%] w-[7%] rounded-full bg-[#251B32]" />
    <span className="absolute right-[32%] top-[31%] h-[10%] w-[7%] rounded-full bg-[#251B32]" />
    <span className={cn("mt-2 font-black leading-none text-[#251B32]", small ? "text-lg" : "text-2xl")}>{item.face}</span>
  </span>
}

export function CreativePollHero({ question, description, category, template, reaction, compact = false, className }: {
  question: string; description?: string | null; category?: string | null;
  template?: PollTemplate | string | null; reaction?: PollReaction | string | null;
  compact?: boolean; className?: string;
}) {
  const style = template === "pulse" || template === "spotlight" ? template : "classic"
  return <div className={cn("relative flex flex-col justify-between gap-3 overflow-hidden rounded-3xl border p-5", compact ? "min-h-36" : "min-h-48 md:p-7", style === "pulse" && "border-rose-300/60 bg-[#FFE1E4] text-[#321D2B] dark:border-rose-400/40 dark:bg-[#40212F] dark:text-white", style === "spotlight" && "border-violet-300/30 bg-[#252034] text-white", style === "classic" && "border-border bg-card text-foreground", className)}>
    {style !== "classic" && <span aria-hidden="true" className={cn("pointer-events-none absolute -right-10 -top-14 h-44 w-44 rounded-full", style === "pulse" ? "bg-rose-400/20" : "bg-violet-300/15")} />}
    <div className="relative flex items-start justify-between gap-3"><span className={cn("text-xs font-extrabold uppercase tracking-[.15em]", style === "pulse" ? "text-[#EE526F]" : style === "spotlight" ? "text-[#D4B3FF]" : "text-primary")}>{category || "Your perspective"}</span>{reaction && <PoPmoji id={reaction} small={compact} />}</div>
    <div className="relative space-y-1"><h2 className={cn("font-extrabold leading-tight tracking-tight", compact ? "text-xl" : "text-2xl md:text-3xl")}>{question || "Your question appears here"}</h2>{description && <p className="line-clamp-3 text-sm opacity-75">{description}</p>}</div>
    {style !== "classic" && <span aria-hidden="true" className={cn("relative h-1 w-11 rounded-full", style === "pulse" ? "bg-[#EE526F]" : "bg-[#D4B3FF]")} />}
  </div>
}
