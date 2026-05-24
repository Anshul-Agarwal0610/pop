"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { motion } from "framer-motion"
import {
  AlertCircle,
  BarChart3,
  Flame,
  Loader2,
  Medal,
  Star,
  Trophy,
  Users,
  Zap,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { usersApi, type ApiUser } from "@/lib/api"
import { cn } from "@/lib/utils"

interface LeaderboardUser {
  rank: number
  name: string
  username: string
  avatar: string
  xp: number
  streak: number
  totalVotes: number
  pollsCreated: number
  level: number
  isYou: boolean
}

function formatNumber(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}K`
  return value.toLocaleString()
}

function mapUsers(users: ApiUser[], currentUserId?: number): LeaderboardUser[] {
  return users.map((user, index) => ({
    rank: index + 1,
    name: user.displayName || user.username,
    username: `@${user.username}`,
    avatar: user.avatarUrl || user.username,
    xp: user.xp,
    streak: user.streak,
    totalVotes: user.totalVotes,
    pollsCreated: user.pollsCreated,
    level: Math.floor(user.xp / 1000) + 1,
    isYou: user.id === currentUserId,
  }))
}

function RankBadge({ rank }: { rank: number }) {
  const colors = [
    "bg-amber-500 text-white",
    "bg-slate-400 text-white",
    "bg-orange-600 text-white",
  ]

  if (rank <= 3) {
    return (
      <div className={cn("flex h-9 w-9 items-center justify-center rounded-xl shadow-sm", colors[rank - 1])}>
        <Medal className="h-4 w-4" />
      </div>
    )
  }

  return (
    <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-secondary text-sm font-bold text-secondary-foreground">
      {rank}
    </div>
  )
}

function LeaderboardRow({ user, index }: { user: LeaderboardUser; index: number }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.04 }}
      className={cn(
        "flex items-center gap-3 rounded-2xl p-3 ring-1 transition-colors md:p-4",
        user.isYou
          ? "bg-primary/10 ring-primary/40"
          : "bg-card ring-border/50 hover:ring-border"
      )}
    >
      <RankBadge rank={user.rank} />

      <Avatar className="h-11 w-11">
        <AvatarImage src={user.avatar.startsWith("http") ? user.avatar : `https://api.dicebear.com/9.x/notionists/svg?seed=${user.avatar}`} alt={user.name} />
        <AvatarFallback>{user.name[0]}</AvatarFallback>
      </Avatar>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="truncate font-semibold text-foreground">{user.name}</span>
          {user.isYou && (
            <span className="rounded-full bg-primary px-2 py-0.5 text-[10px] font-bold text-primary-foreground">
              YOU
            </span>
          )}
        </div>
        <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
          <span>{user.username}</span>
          <span>Lv.{user.level}</span>
          <span>{formatNumber(user.totalVotes)} votes</span>
          <span>{formatNumber(user.pollsCreated)} polls</span>
        </div>
      </div>

      <div className="text-right">
        <div className="flex items-center justify-end gap-1 text-sm font-bold text-amber-500">
          <Star className="h-4 w-4" />
          {formatNumber(user.xp)}
        </div>
        <div className="mt-1 flex items-center justify-end gap-1 text-xs text-orange-500">
          <Flame className="h-3.5 w-3.5" />
          {user.streak} day streak
        </div>
      </div>
    </motion.div>
  )
}

export default function LeaderboardPage() {
  const { user: authUser } = useAuth()
  const [users, setUsers] = useState<LeaderboardUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadLeaderboard = useCallback(async () => {
    setLoading(true)
    setError(null)

    try {
      const data = await usersApi.getLeaderboard(20)
      setUsers(mapUsers(data, authUser?.id))
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load leaderboard")
    } finally {
      setLoading(false)
    }
  }, [authUser?.id])

  useEffect(() => {
    loadLeaderboard()
  }, [loadLeaderboard])

  const currentUser = users.find((user) => user.isYou)
  const totalXp = useMemo(
    () => users.reduce((sum, user) => sum + user.xp, 0),
    [users]
  )

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-4 md:py-6">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-5 flex items-center justify-between gap-4"
        >
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-amber-400 to-orange-500 shadow-lg">
              <Trophy className="h-5 w-5 text-white" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-foreground md:text-2xl">Leaderboard</h1>
              <p className="text-xs text-muted-foreground md:text-sm">
                Ranked by real reputation XP
              </p>
            </div>
          </div>

          {currentUser && (
            <div className="rounded-xl bg-primary/10 px-3 py-2 text-right">
              <div className="text-xs text-muted-foreground">Your Rank</div>
              <div className="text-lg font-black text-primary">#{currentUser.rank}</div>
            </div>
          )}
        </motion.div>

        <div className="mb-5 grid grid-cols-3 gap-3">
          <div className="rounded-2xl bg-card p-3 ring-1 ring-border/50">
            <Users className="h-4 w-4 text-primary" />
            <div className="mt-2 text-lg font-bold text-foreground">{users.length}</div>
            <div className="text-[10px] text-muted-foreground">Ranked Users</div>
          </div>
          <div className="rounded-2xl bg-card p-3 ring-1 ring-border/50">
            <Zap className="h-4 w-4 text-amber-500" />
            <div className="mt-2 text-lg font-bold text-foreground">{formatNumber(totalXp)}</div>
            <div className="text-[10px] text-muted-foreground">Total XP</div>
          </div>
          <div className="rounded-2xl bg-card p-3 ring-1 ring-border/50">
            <BarChart3 className="h-4 w-4 text-emerald-500" />
            <div className="mt-2 text-lg font-bold text-foreground">XP</div>
            <div className="text-[10px] text-muted-foreground">Live Metric</div>
          </div>
        </div>

        <div className="mb-4 rounded-2xl bg-card p-3 text-sm text-muted-foreground ring-1 ring-border/50">
          More leaderboard modes will appear here once the backend supports those rankings.
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-2xl bg-card p-8 text-muted-foreground ring-1 ring-border/50">
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Loading leaderboard...
          </div>
        ) : error ? (
          <div className="rounded-2xl bg-card p-5 ring-1 ring-border/50">
            <AlertCircle className="mb-2 h-5 w-5 text-destructive" />
            <p className="text-sm text-destructive">{error}</p>
            <Button variant="outline" size="sm" onClick={loadLeaderboard} className="mt-4">
              Retry
            </Button>
          </div>
        ) : users.length === 0 ? (
          <div className="rounded-2xl bg-card p-6 text-center text-sm text-muted-foreground ring-1 ring-border/50">
            No ranked users yet. Vote on a poll to earn the first XP.
          </div>
        ) : (
          <div className="space-y-2">
            {users.map((user, index) => (
              <LeaderboardRow key={user.username} user={user} index={index} />
            ))}
          </div>
        )}
      </div>
    </AppShell>
  )
}
