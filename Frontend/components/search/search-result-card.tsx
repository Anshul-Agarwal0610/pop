"use client"

import { ImageOff, Newspaper, Users } from "lucide-react"
import { useRouter } from "next/navigation"
import { CategoryBadge } from "@/components/category-badge"
import type { ApiPoll } from "@/lib/api"
import { SOURCE_COLORS, SOURCE_LABELS, type IngestionSource } from "@/lib/poll-data"
import { cn } from "@/lib/utils"

interface SearchResultCardProps {
  poll: ApiPoll
  onSelect: () => void
}

function sourceStyle(sourceType: string | null) {
  const source = (sourceType ?? "manual") as IngestionSource
  return SOURCE_COLORS[source] ?? SOURCE_COLORS.manual
}

function sourceLabel(sourceType: string | null) {
  const source = (sourceType ?? "manual") as IngestionSource
  return SOURCE_LABELS[source] ?? "Pollify"
}

export function SearchResultCard({ poll, onSelect }: SearchResultCardProps) {
  const router = useRouter()
  const style = sourceStyle(poll.sourceType)

  function openPoll() {
    onSelect()
    router.push(`/polls/${poll.id}`)
  }

  return (
    <button
      className="flex w-full gap-3 rounded-xl p-2 text-left transition-colors hover:bg-secondary/70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      onClick={openPoll}
      type="button"
    >
      <div className="hidden h-20 w-20 flex-shrink-0 overflow-hidden rounded-lg bg-secondary min-[360px]:block">
        {poll.thumbnailUrl ? (
          <img
            alt=""
            className="h-full w-full object-cover"
            src={poll.thumbnailUrl}
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-muted-foreground">
            <ImageOff className="h-6 w-6 opacity-50" />
          </div>
        )}
      </div>

      <div className="min-w-0 flex-1 space-y-2">
        <h3 className="break-words text-sm font-bold leading-snug text-foreground min-[360px]:line-clamp-2">
          {poll.question}
        </h3>
        <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
          <CategoryBadge category={poll.category} className="px-2 py-0.5" />
          <span className="flex items-center gap-1">
            <Users className="h-3.5 w-3.5" />
            {poll.totalVotes.toLocaleString()}
          </span>
          <span
            className={cn(
              "flex items-center gap-1 rounded-full px-2 py-0.5 font-medium",
              style.bg,
              style.text
            )}
          >
            <Newspaper className={cn("h-3.5 w-3.5", style.icon)} />
            {sourceLabel(poll.sourceType)}
          </span>
        </div>
      </div>
    </button>
  )
}
