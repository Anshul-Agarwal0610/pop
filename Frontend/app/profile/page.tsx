"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { motion } from "framer-motion"
import {
  Trophy,
  Target,
  Flame,
  BarChart3,
  Calendar,
  Zap,
  Loader2,
  Clock,
  LogOut,
  CheckCircle2,
  Award,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { CategoryBadge } from "@/components/category-badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { useAuth } from "@/contexts/auth-context"
import { authApi, usersApi, type ApiUser, type ApiVoteHistoryItem } from "@/lib/api"

// ── Helpers ───────────────────────────────────────────────────────────────────

function relativeTime(iso: string) {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 60)  return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24)   return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

function joinedDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-US", { month: "long", year: "numeric" })
}

// ── Skeleton ──────────────────────────────────────────────────────────────────

function ProfileSkeleton() {
  return (
    <div className="mx-auto max-w-3xl animate-pulse px-4 py-6 space-y-6">
      <div className="rounded-2xl bg-muted h-48" />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="rounded-2xl bg-muted h-24" />
        ))}
      </div>
      <div className="rounded-2xl bg-muted h-40" />
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function ProfilePage() {
  const router              = useRouter()
  const { user: authUser, logout } = useAuth()
  const [profile, setProfile]   = useState<ApiUser | null>(null)
  const [history, setHistory]   = useState<ApiVoteHistoryItem[]>([])
  const [loading, setLoading]   = useState(true)

  // Fetch fresh profile (for up-to-date XP) + vote history
  useEffect(() => {
    if (!authUser) return
    Promise.all([
      authApi.getMe(),
      usersApi.getMyVotes(5),
    ])
      .then(([freshUser, votes]) => {
        setProfile(freshUser)
        setHistory(votes)
      })
      .catch(console.error)
      .finally(() => setLoading(false))
  }, [authUser])

  if (authLoading || loading) return <AppShell><ProfileSkeleton /></AppShell>
  if (!authUser) return null

  // Use fresh API data when available, fall back to cached auth user
  const displayUser = profile ?? authUser
  const { level, progressPercent: progress, nextLevelXp: nextXp } = displayUser.progression
  const badges = displayUser.badges ?? []

  const stats = [
    { icon: BarChart3, label: "Polls Created", value: String(displayUser.pollsCreated ?? 0), color: "text-primary"     },
    { icon: Trophy,    label: "Total Votes",   value: String(displayUser.totalVotes ?? 0),    color: "text-amber-500"   },
    { icon: Target,    label: "Total XP",      value: `${displayUser.xp ?? 0}`,               color: "text-emerald-500" },
    { icon: Flame,     label: "Current streak", value: String(displayUser.streak ?? 0),       color: "text-orange-500"  },
    { icon: Award,     label: "Longest streak", value: String(displayUser.longestStreak ?? displayUser.streak ?? 0), color: "text-orange-500" },
  ]

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">

        {/* Profile Header */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
        >
          <div className="relative rounded-2xl bg-gradient-to-br from-primary/20 via-accent/10 to-primary/5 p-6">
            {/* Logout */}
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-4 top-4 rounded-xl text-muted-foreground hover:text-foreground"
              onClick={() => { logout(); router.replace("/") }}
            >
              <LogOut className="h-5 w-5" />
              <span className="sr-only">Sign out</span>
            </Button>

            <div className="flex flex-col items-center text-center sm:flex-row sm:items-start sm:text-left">
              {/* Avatar */}
              <motion.div whileHover={{ scale: 1.05 }} className="relative">
                <Avatar className="h-24 w-24 ring-4 ring-background shadow-xl">
                  <AvatarImage
                    src={displayUser.avatarUrl ?? `https://api.dicebear.com/9.x/notionists/svg?seed=${displayUser.username}`}
                    alt={displayUser.displayName}
                  />
                  <AvatarFallback className="bg-primary text-2xl text-primary-foreground">
                    {displayUser.displayName?.[0]?.toUpperCase() ?? "?"}
                  </AvatarFallback>
                </Avatar>
                {/* Level badge */}
                <motion.div
                  className="absolute -bottom-1 -right-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground shadow-lg"
                  animate={{ scale: [1, 1.1, 1] }}
                  transition={{ duration: 2, repeat: Infinity }}
                >
                  {level}
                </motion.div>
              </motion.div>

              {/* Info */}
              <div className="mt-4 sm:ml-6 sm:mt-0">
                <h1 className="text-2xl font-bold text-foreground">{displayUser.displayName}</h1>
                <p className="text-muted-foreground">@{displayUser.username}</p>

                <div className="mt-3 flex flex-wrap items-center justify-center gap-3 text-sm text-muted-foreground sm:justify-start">
                  <span className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    Joined {joinedDate(displayUser.createdAt ?? new Date().toISOString())}
                  </span>
                </div>

                {/* XP Progress bar */}
                <div className="mt-4 w-full max-w-xs">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Level {level}</span>
                    <span className="flex items-center gap-1 font-medium text-primary">
                      <Zap className="h-3.5 w-3.5" />
                      {displayUser.xp ?? 0} / {nextXp} XP
                    </span>
                  </div>
                  <div className="mt-1 h-2 overflow-hidden rounded-full bg-background">
                    <motion.div
                      className="h-full rounded-full bg-gradient-to-r from-primary to-accent"
                      initial={{ width: 0 }}
                      animate={{ width: `${progress}%` }}
                      transition={{ delay: 0.3, duration: 0.8 }}
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </motion.div>

        {/* Stats Grid */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-5"
        >
          {stats.map((stat, index) => (
            <motion.div
              key={stat.label}
              whileHover={{ scale: 1.03, y: -2 }}
              className="flex flex-col items-center rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.15 + index * 0.05 }}
            >
              <stat.icon className={cn("h-5 w-5", stat.color)} />
              <span className="mt-2 text-2xl font-bold text-foreground">{stat.value}</span>
              <span className="text-xs text-muted-foreground">{stat.label}</span>
            </motion.div>
          ))}
        </motion.div>

        {/* Vote History */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.18 }}
          className="mb-6"
        >
          <div className="mb-3 flex items-center justify-between"><h2 className="text-lg font-semibold text-foreground">Badges</h2><Button asChild size="sm" variant="ghost"><Link href="/achievements">View all achievements</Link></Button></div>
          {badges.length === 0 ? (
            <div className="rounded-2xl bg-card p-6 text-center ring-1 ring-border/50">
              <Award className="mx-auto h-8 w-8 text-muted-foreground" />
              <p className="mt-2 text-sm text-muted-foreground">
                Keep voting, creating, and completing challenges to earn badges.
              </p>
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2">
              {badges.slice(0, 6).map((badge) => (
                <div
                  className="rounded-2xl bg-card p-4 ring-1 ring-border/50"
                  key={badge.id}
                >
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
                      <Award className="h-5 w-5" />
                    </div>
                    <div className="min-w-0">
                      <p className="truncate font-semibold text-foreground">{badge.name}</p>
                      <p className="line-clamp-2 text-xs text-muted-foreground">{badge.description}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <h2 className="mb-3 text-lg font-semibold text-foreground">Recent Votes</h2>

          {history.length === 0 ? (
            <div className="rounded-2xl bg-card p-8 text-center ring-1 ring-border/50">
              <p className="text-muted-foreground text-sm">You haven&apos;t voted on anything yet.</p>
              <Button className="mt-4" onClick={() => router.push("/")}>Browse Polls</Button>
            </div>
          ) : (
            <div className="space-y-2">
              {history.map((item, index) => (
                <motion.div
                  key={item.pollId}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.25 + index * 0.05 }}
                  whileHover={{ scale: 1.01, x: 4 }}
                  className="flex items-start justify-between gap-3 rounded-2xl bg-card p-4 ring-1 ring-border/50"
                >
                  <div className="min-w-0 flex-1">
                    <h3 className="line-clamp-2 font-medium text-foreground">{item.question}</h3>
                    <div className="mt-1.5 flex flex-wrap items-center gap-2">
                      <CategoryBadge category={item.category} className="px-2 py-0.5" />
                      <span className="flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400">
                        <CheckCircle2 className="h-3.5 w-3.5" />
                        {item.votedOptionText}
                      </span>
                    </div>
                    <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
                      <Clock className="h-3 w-3" />
                      {relativeTime(item.votedAt)} · {item.totalVotes.toLocaleString()} votes
                    </p>
                  </div>
                </motion.div>
              ))}
            </div>
          )}
        </motion.div>
      </div>
    </AppShell>
  )
}
