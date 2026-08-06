"use client"

import { Award, Zap } from "lucide-react"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { cn } from "@/lib/utils"
import type { ApiLeaderboardRow } from "@/lib/api"

function rankTone(rank: number) {
  if (rank === 1) return "bg-amber-500 text-white"
  if (rank === 2) return "bg-slate-400 text-white"
  if (rank === 3) return "bg-orange-700 text-white"
  return "bg-secondary text-secondary-foreground"
}

export function LeaderboardRow({ row, isYou = false }: { row: ApiLeaderboardRow; isYou?: boolean }) {
  const name = row.displayName || row.username
  const seed = row.username || name || `user-${row.id}`
  return (
    <div className={cn("flex items-center gap-3 rounded-2xl bg-card p-4 ring-1 ring-border/50", isYou && "bg-primary/10 ring-primary/30")}>
      <div className={cn("flex h-9 w-9 shrink-0 items-center justify-center rounded-xl text-sm font-black", rankTone(row.rank))}>
        {row.rank <= 3 ? <Award className="h-4 w-4" /> : row.rank}
      </div>
      <Avatar className="h-12 w-12">
        <AvatarImage alt={name} src={row.avatarUrl ?? `https://api.dicebear.com/9.x/notionists/svg?seed=${seed}`} />
        <AvatarFallback className="bg-primary text-primary-foreground">{name[0]?.toUpperCase() ?? "?"}</AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate font-semibold">{name}</p>
          {isYou && <Badge className="text-[10px]">YOU</Badge>}
        </div>
        <p className="text-sm text-muted-foreground">@{row.username}</p>
        {!!row.badges?.length && <div className="mt-1 flex flex-wrap gap-1">{row.badges.slice(0, 2).map(b => <Badge key={b.id} variant="secondary" className="text-[10px]">{b.name}</Badge>)}</div>}
      </div>
      <div className="text-right">
        <div className="flex items-center justify-end gap-1 font-black text-amber-500"><Zap className="h-4 w-4 fill-current" />{row.periodXp.toLocaleString()}</div>
        <p className="text-xs text-muted-foreground">period XP · Level {row.level}</p>
      </div>
    </div>
  )
}
