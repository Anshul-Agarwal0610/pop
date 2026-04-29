"use client"

import { motion } from "framer-motion"
import {
  Eye,
  TrendingUp,
  Zap,
  Clock,
  Users,
  Youtube,
  Instagram,
  Newspaper,
  ExternalLink,
  Globe,
  Lock,
  Play,
  FileText,
  Music,
} from "lucide-react"
import { cn } from "@/lib/utils"
import type { PollDraft } from "@/lib/poll-creation"

interface PreviewStepProps {
  draft: PollDraft
}

// X/Twitter icon
function XIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
    </svg>
  )
}

const PLATFORM_ICONS = {
  youtube: Youtube,
  instagram: Instagram,
  twitter: XIcon,
  news: Newspaper,
  other: ExternalLink,
}

const PLATFORM_STYLES = {
  youtube: "bg-red-500/90 text-white",
  instagram: "bg-gradient-to-r from-purple-500 to-pink-500 text-white",
  twitter: "bg-foreground text-background",
  news: "bg-blue-500 text-white",
  other: "bg-muted text-foreground",
}

export function PreviewStep({ draft }: PreviewStepProps) {
  const hasMedia = draft.media !== null
  const hasLink = draft.externalLink !== null
  const SourceIcon = draft.externalLink ? PLATFORM_ICONS[draft.externalLink.platform] : null
  const sourceStyle = draft.externalLink ? PLATFORM_STYLES[draft.externalLink.platform] : ""

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="flex flex-col gap-6"
    >
      {/* Header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <Eye className="h-8 w-8 text-primary" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          Preview your poll
        </h2>
        <p className="mt-2 text-muted-foreground">
          This is how your poll will look to voters
        </p>
      </div>

      {/* Poll Card Preview */}
      <motion.div
        className="relative overflow-hidden rounded-3xl bg-card shadow-xl ring-1 ring-border"
        initial={{ scale: 0.9, opacity: 0 }}
        animate={{ scale: 1, opacity: 1 }}
        transition={{ delay: 0.2, type: "spring", stiffness: 200, damping: 20 }}
      >
        {/* Media Section */}
        <div className="relative h-48 overflow-hidden bg-muted">
          {hasMedia && draft.media?.type === "image" ? (
            <img
              src={draft.media.url}
              alt=""
              className="h-full w-full object-cover"
            />
          ) : hasMedia && draft.media?.type === "video" ? (
            <div className="relative h-full w-full">
              <video
                src={draft.media.url}
                className="h-full w-full object-cover"
              />
              <div className="absolute inset-0 flex items-center justify-center bg-black/30">
                <div className="rounded-full bg-white/90 p-4">
                  <Play className="h-8 w-8 fill-current text-foreground" />
                </div>
              </div>
            </div>
          ) : hasMedia && draft.media?.type === "audio" ? (
            <div className="flex h-full items-center justify-center bg-gradient-to-br from-emerald-500/20 to-teal-500/20">
              <div className="rounded-2xl bg-emerald-500/10 p-6">
                <Music className="h-16 w-16 text-emerald-500" />
              </div>
            </div>
          ) : hasMedia && draft.media?.type === "document" ? (
            <div className="flex h-full items-center justify-center bg-gradient-to-br from-blue-500/20 to-indigo-500/20">
              <div className="text-center">
                <div className="mx-auto rounded-2xl bg-blue-500/10 p-6">
                  <FileText className="h-16 w-16 text-blue-500" />
                </div>
                <p className="mt-3 text-sm font-medium text-muted-foreground">
                  {draft.media.name}
                </p>
              </div>
            </div>
          ) : hasLink && draft.externalLink?.thumbnail ? (
            <img
              src={draft.externalLink.thumbnail}
              alt=""
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full items-center justify-center bg-gradient-to-br from-primary/10 to-accent/10">
              <p className="text-muted-foreground">No media attached</p>
            </div>
          )}

          {/* Gradient Overlay */}
          <div className="absolute inset-0 bg-gradient-to-t from-card via-transparent to-transparent" />

          {/* Top Badges */}
          <div className="absolute left-4 right-4 top-4 flex items-start justify-between">
            {/* New Badge */}
            <motion.div
              className="flex items-center gap-1.5 rounded-full bg-primary px-3 py-1.5 text-primary-foreground shadow-lg"
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              transition={{ delay: 0.4, type: "spring" }}
            >
              <TrendingUp className="h-3.5 w-3.5" />
              <span className="text-xs font-semibold">New</span>
            </motion.div>

            {/* XP Reward */}
            <motion.div
              className="flex items-center gap-1.5 rounded-full bg-amber-500 px-3 py-1.5 text-amber-950 shadow-lg"
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              transition={{ delay: 0.5, type: "spring" }}
            >
              <Zap className="h-3.5 w-3.5 fill-current" />
              <span className="text-xs font-bold">+10 XP</span>
            </motion.div>
          </div>

          {/* Source Label */}
          {hasLink && SourceIcon && (
            <motion.div
              className={cn(
                "absolute bottom-4 left-4 flex items-center gap-2 rounded-full px-3 py-1.5 shadow-lg",
                sourceStyle
              )}
              initial={{ scale: 0, x: -20 }}
              animate={{ scale: 1, x: 0 }}
              transition={{ delay: 0.6, type: "spring" }}
            >
              <SourceIcon className="h-4 w-4" />
              <span className="text-xs font-medium capitalize">
                {draft.externalLink?.platform === "twitter" ? "X" : draft.externalLink?.platform}
              </span>
            </motion.div>
          )}
        </div>

        {/* Content Section */}
        <div className="p-5">
          {/* Category & Privacy */}
          <div className="mb-3 flex items-center gap-3">
            <span className="rounded-full bg-secondary px-3 py-1 text-xs font-medium text-secondary-foreground">
              General
            </span>
            <span className="flex items-center gap-1 text-xs text-muted-foreground">
              {draft.privacy === "public" ? (
                <>
                  <Globe className="h-3 w-3" />
                  Public
                </>
              ) : (
                <>
                  <Lock className="h-3 w-3" />
                  Private
                </>
              )}
            </span>
            <span className="flex items-center gap-1 text-xs text-muted-foreground">
              <Clock className="h-3 w-3" />
              Just now
            </span>
          </div>

          {/* Question */}
          <h2 className="mb-2 text-xl font-bold leading-tight text-foreground text-balance">
            {draft.question || "Your question will appear here..."}
          </h2>

          {/* Description */}
          {draft.description && (
            <p className="mb-4 text-sm text-muted-foreground line-clamp-2">
              {draft.description}
            </p>
          )}

          {/* Stats */}
          <div className="mb-4 flex items-center gap-4 text-sm text-muted-foreground">
            <span className="flex items-center gap-1.5">
              <Users className="h-4 w-4" />
              0 votes
            </span>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center gap-3">
            <div className="flex flex-1 items-center justify-center gap-2 rounded-2xl bg-red-500/10 py-3 font-bold text-red-500 ring-2 ring-red-500/20">
              <span className="text-xl">NO</span>
            </div>
            <div className="flex flex-1 items-center justify-center gap-2 rounded-2xl bg-emerald-500/10 py-3 font-bold text-emerald-500 ring-2 ring-emerald-500/20">
              <span className="text-xl">YES</span>
            </div>
          </div>
        </div>
      </motion.div>

      {/* Info */}
      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5 }}
        className="text-center text-sm text-muted-foreground"
      >
        Looks good? Hit publish to share your poll!
      </motion.p>
    </motion.div>
  )
}
