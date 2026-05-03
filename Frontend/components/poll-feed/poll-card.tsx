"use client"

import { motion, useMotionValue, useTransform, PanInfo } from "framer-motion"
import {
  TrendingUp,
  Play,
  Youtube,
  Instagram,
  Newspaper,
  Rss,
  Users,
  Zap,
  Clock,
  Sparkles,
  ImageOff,
} from "lucide-react"
import { cn } from "@/lib/utils"
import { Poll, SOURCE_COLORS, SOURCE_LABELS } from "@/lib/poll-data"
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip"

// ── Custom icons ──────────────────────────────────────────────────────────────
function XIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
    </svg>
  )
}

function TikTokIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor">
      <path d="M19.59 6.69a4.83 4.83 0 01-3.77-4.25V2h-3.45v13.67a2.89 2.89 0 01-5.2 1.74 2.89 2.89 0 012.31-4.64 2.93 2.93 0 01.88.13V9.4a6.84 6.84 0 00-1-.05A6.33 6.33 0 005 20.1a6.34 6.34 0 0010.86-4.43v-7a8.16 8.16 0 004.77 1.52v-3.4a4.85 4.85 0 01-1-.1z" />
    </svg>
  )
}

function GoogleNewsIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor">
      <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 14H9V8h2v8zm4 0h-2V8h2v8z" />
    </svg>
  )
}

// ── Source icon map ───────────────────────────────────────────────────────────
// Maps both legacy UI sources and ingestion pipeline sources to icons
const SourceIcon: Record<string, React.ComponentType<{ className?: string }>> = {
  // Legacy
  youtube:   Youtube,
  instagram: Instagram,
  twitter:   XIcon,
  news:      Newspaper,
  tiktok:    TikTokIcon,
  // Ingestion pipeline (US-10)
  rss:       Rss,
  gnews:     GoogleNewsIcon,
  manual:    Newspaper,
}

// ── Component ─────────────────────────────────────────────────────────────────
interface PollCardProps {
  poll: Poll
  onVote: (vote: "yes" | "no") => void
  isActive: boolean
}

export function PollCard({ poll, onVote, isActive }: PollCardProps) {
  const x         = useMotionValue(0)
  const rotate    = useTransform(x, [-200, 200], [-15, 15])
  const yesOpacity = useTransform(x, [0, 100], [0, 1])
  const noOpacity  = useTransform(x, [-100, 0], [1, 0])

  // Prefer backend thumbnailUrl → fallback to mediaUrl
  const imageUrl = poll.thumbnailUrl || poll.mediaUrl

  // Source resolution: prefer ingestion sourceType, fall back to legacy source field
  const effectiveSource = poll.sourceType ?? poll.source
  const sourceStyle     = SOURCE_COLORS[effectiveSource] ?? SOURCE_COLORS.manual
  const Icon            = SourceIcon[effectiveSource] ?? Newspaper

  // Display label: prefer sourceLabel, fall back to SOURCE_LABELS map for ingestion types
  const sourceDisplayLabel =
    poll.sourceType
      ? SOURCE_LABELS[poll.sourceType]
      : poll.sourceLabel

  function handleDragEnd(_: unknown, info: PanInfo) {
    const threshold = 100
    if (info.offset.x > threshold)      onVote("yes")
    else if (info.offset.x < -threshold) onVote("no")
  }

  return (
    <motion.div
      className={cn(
        "absolute inset-x-4 top-0 h-full cursor-grab active:cursor-grabbing md:inset-x-0",
        !isActive && "pointer-events-none"
      )}
      style={{ x, rotate, zIndex: isActive ? 10 : 0 }}
      drag={isActive ? "x" : false}
      dragConstraints={{ left: 0, right: 0 }}
      dragElastic={0.7}
      onDragEnd={handleDragEnd}
      initial={{ scale: 0.95, opacity: 0 }}
      animate={{ scale: isActive ? 1 : 0.95, opacity: isActive ? 1 : 0.5 }}
      exit={{ scale: 0.8, opacity: 0 }}
      transition={{ type: "spring", stiffness: 300, damping: 25 }}
    >
      {/* Card Container */}
      <div className="relative h-full overflow-hidden rounded-3xl bg-card shadow-2xl ring-1 ring-border/50">

        {/* YES Overlay */}
        <motion.div
          className="pointer-events-none absolute inset-0 z-20 flex items-center justify-center bg-emerald-500/20"
          style={{ opacity: yesOpacity }}
        >
          <motion.div
            className="rounded-2xl border-4 border-emerald-500 bg-emerald-500/30 px-8 py-4"
            style={{ opacity: yesOpacity, rotate: -15 }}
          >
            <span className="text-4xl font-black text-emerald-500">YES</span>
          </motion.div>
        </motion.div>

        {/* NO Overlay */}
        <motion.div
          className="pointer-events-none absolute inset-0 z-20 flex items-center justify-center bg-red-500/20"
          style={{ opacity: noOpacity }}
        >
          <motion.div
            className="rounded-2xl border-4 border-red-500 bg-red-500/30 px-8 py-4"
            style={{ opacity: noOpacity, rotate: 15 }}
          >
            <span className="text-4xl font-black text-red-500">NO</span>
          </motion.div>
        </motion.div>

        {/* ── Media Section ─────────────────────────────────────────────────── */}
        <div className="relative h-[55%] overflow-hidden bg-muted">
          {imageUrl ? (
            <img
              src={imageUrl}
              alt=""
              className="h-full w-full object-cover"
              crossOrigin="anonymous"
              onError={(e) => {
                // Hide broken image; placeholder gradient underneath will show
                ;(e.currentTarget as HTMLImageElement).style.display = "none"
              }}
            />
          ) : (
            /* No thumbnail — show branded placeholder */
            <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-primary/20 via-accent/10 to-primary/5">
              <div className="flex flex-col items-center gap-2 text-muted-foreground">
                <ImageOff className="h-10 w-10 opacity-40" />
                <span className="text-xs opacity-50">No preview available</span>
              </div>
            </div>
          )}

          {/* Gradient Overlay — always on top of image */}
          <div className="absolute inset-0 bg-gradient-to-t from-card via-transparent to-transparent" />

          {/* Play Button for Videos */}
          {poll.mediaType === "video" && (
            <motion.button
              className="absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 rounded-full bg-foreground/80 p-5 text-background"
              whileHover={{ scale: 1.1 }}
              whileTap={{ scale: 0.95 }}
            >
              <Play className="h-8 w-8 fill-current" />
            </motion.button>
          )}

          {/* ── Top Badges ───────────────────────────────────────────────── */}
          <div className="absolute left-4 right-4 top-4 flex items-start justify-between">
            <div className="flex flex-col gap-1.5">
              {/* Trending Badge */}
              {poll.trending && (
                <motion.div
                  className="flex items-center gap-1.5 rounded-full bg-primary px-3 py-1.5 text-primary-foreground shadow-lg"
                  initial={{ scale: 0, y: -20 }}
                  animate={{ scale: 1, y: 0 }}
                  transition={{ delay: 0.2, type: "spring" }}
                >
                  <TrendingUp className="h-3.5 w-3.5" />
                  <span className="text-xs font-semibold">Trending</span>
                </motion.div>
              )}

              {/* AI Generated Badge (US-10) */}
              {poll.isAIGenerated && (
                <motion.div
                  className="flex items-center gap-1.5 rounded-full bg-violet-500/15 px-3 py-1.5 shadow-lg ring-1 ring-violet-500/30"
                  initial={{ scale: 0, y: -20 }}
                  animate={{ scale: 1, y: 0 }}
                  transition={{ delay: 0.25, type: "spring" }}
                >
                  <Sparkles className="h-3 w-3 text-violet-400" />
                  <span className="text-xs font-medium text-violet-400">AI Poll</span>
                </motion.div>
              )}
            </div>

            {/* XP Reward */}
            <motion.div
              className="flex items-center gap-1.5 rounded-full bg-amber-500 px-3 py-1.5 text-amber-950 shadow-lg"
              initial={{ scale: 0, y: -20 }}
              animate={{ scale: 1, y: 0 }}
              transition={{ delay: 0.3, type: "spring" }}
            >
              <Zap className="h-3.5 w-3.5 fill-current" />
              <span className="text-xs font-bold">+{poll.xpReward} XP</span>
            </motion.div>
          </div>

          {/* ── Source Badge ─────────────────────────────────────────────── */}
          <motion.div
            className={cn(
              "absolute bottom-4 left-4 flex items-center gap-2 rounded-full px-3 py-1.5 shadow-lg backdrop-blur-sm",
              sourceStyle.bg
            )}
            initial={{ scale: 0, x: -20 }}
            animate={{ scale: 1, x: 0 }}
            transition={{ delay: 0.4, type: "spring" }}
          >
            <Icon className={cn("h-4 w-4", sourceStyle.icon)} />
            <span className={cn("text-xs font-medium", sourceStyle.text)}>
              {sourceDisplayLabel}
            </span>
          </motion.div>
        </div>

        {/* ── Content Section ───────────────────────────────────────────────── */}
        <div className="flex h-[45%] flex-col p-5">
          {/* Category & Time — never shrink */}
          <div className="mb-3 flex flex-shrink-0 items-center gap-3">
            <span className="rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
              {poll.category}
            </span>
            <span className="flex items-center gap-1 text-xs text-muted-foreground">
              <Clock className="h-3 w-3" />
              {poll.timePosted}
            </span>
          </div>

          {/* Question — clamped to 3 lines; hover tooltip shows full text on desktop */}
          <div className="mb-3 min-h-0 flex-1 overflow-hidden">
            <Tooltip delayDuration={300}>
              <TooltipTrigger asChild>
                <h2 className="line-clamp-3 cursor-default text-xl font-bold leading-tight text-foreground md:text-2xl">
                  {poll.question}
                </h2>
              </TooltipTrigger>
              <TooltipContent
                side="top"
                className="max-w-xs text-sm font-medium leading-snug"
              >
                {poll.question}
              </TooltipContent>
            </Tooltip>
          </div>

          {/* Stats — never shrink */}
          <div className="mb-3 flex flex-shrink-0 items-center gap-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-1.5">
              <Users className="h-4 w-4" />
              {poll.totalVotes.toLocaleString()} votes
            </span>
          </div>

          {/* Action Buttons — never shrink, always visible */}
          <div className="flex flex-shrink-0 items-center gap-3">
            <motion.button
              onClick={() => onVote("no")}
              className="flex flex-1 items-center justify-center gap-2 rounded-2xl bg-red-500/10 py-4 font-bold text-red-500 ring-2 ring-red-500/20 transition-colors hover:bg-red-500/20"
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
            >
              <span className="text-2xl">NO</span>
            </motion.button>
            <motion.button
              onClick={() => onVote("yes")}
              className="flex flex-1 items-center justify-center gap-2 rounded-2xl bg-emerald-500/10 py-4 font-bold text-emerald-500 ring-2 ring-emerald-500/20 transition-colors hover:bg-emerald-500/20"
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
            >
              <span className="text-2xl">YES</span>
            </motion.button>
          </div>
        </div>
      </div>
    </motion.div>
  )
}
