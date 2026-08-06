"use client"

import { useEffect, useState } from "react"
import { Award, Loader2, Medal, Trophy, Zap } from "lucide-react"
import { motion } from "framer-motion"
import { AppShell } from "@/components/app-shell"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { socialApi, usersApi, type ApiUser } from "@/lib/api"
import { cn } from "@/lib/utils"

function levelFromXp(xp: number) {
  return Math.floor(xp / 1000) + 1
}

function rankTone(rank: number) {
  if (rank === 1) return "bg-amber-500 text-white"
  if (rank === 2) return "bg-slate-400 text-white"
  if (rank === 3) return "bg-orange-700 text-white"
  return "bg-secondary text-secondary-foreground"
}

export default function LeaderboardPage() {
  const { user: authUser } = useAuth()
  const [users, setUsers] = useState<ApiUser[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [scope, setScope] = useState<"global" | "friends">("global")
  const [weekLabel, setWeekLabel] = useState<string | null>(null)

  useEffect(() => {
    const load = Promise.resolve().then(() => {
      setLoading(true)
      setError(null)
      return scope === "global"
      ? usersApi.getLeaderboard(20).then(data => { setWeekLabel(null); return data })
      : socialApi.friendsLeaderboard().then(data => {
          setWeekLabel(`${new Date(data.weekStartUtc).toLocaleDateString()} – ${new Date(data.weekEndUtc).toLocaleDateString()} (resets Monday 00:00 UTC)`)
          return data.items.map(entry => ({ ...entry.user, xp: entry.xp, totalVotes: entry.activityCount, level: levelFromXp(entry.xp), streak: 0, pollsCreated: 0, createdAt: "", authProvider: "", badges: [] })) as ApiUser[]
        })
    })
    load
      .then(setUsers)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [scope])

  const currentRank = users.findIndex((user) => user.id === authUser?.id) + 1

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        <motion.div
          animate={{ opacity: 1, y: 0 }}
          className="mb-6 flex items-center justify-between gap-4"
          initial={{ opacity: 0, y: 20 }}
        >
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-500 text-white shadow-lg">
              <Trophy className="h-6 w-6" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Leaderboard</h1>
              <p className="text-sm text-muted-foreground">
                {weekLabel ?? "Top users by lifetime XP"}
              </p>
            </div>
          </div>
          {currentRank > 0 && (
            <div className="rounded-xl bg-primary/10 px-3 py-2 text-right">
              <p className="text-xs text-muted-foreground">Your Rank</p>
              <p className="text-lg font-black text-primary">#{currentRank}</p>
            </div>
          )}
        </motion.div>

        <div className="mb-5 flex gap-2" aria-label="Leaderboard scope">
          <Button onClick={() => setScope("global")} variant={scope === "global" ? "default" : "outline"}>Global</Button>
          <Button disabled={!authUser} onClick={() => setScope("friends")} variant={scope === "friends" ? "default" : "outline"}>Friends this week</Button>
        </div>

        {loading && (
          <div className="flex items-center justify-center gap-2 rounded-2xl bg-card py-20 text-muted-foreground ring-1 ring-border/50">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading leaderboard...
          </div>
        )}

        {!loading && error && (
          <div className="rounded-2xl bg-destructive/10 p-6 text-center">
            <p className="font-semibold text-destructive">Could not load leaderboard</p>
            <p className="mt-1 text-sm text-muted-foreground">{error}</p>
            <Button className="mt-4" onClick={() => window.location.reload()} variant="outline">
              Retry
            </Button>
          </div>
        )}

        {!loading && !error && users.length === 0 && (
          <div className="rounded-2xl bg-card p-10 text-center ring-1 ring-border/50">
            <Medal className="mx-auto h-12 w-12 text-muted-foreground" />
            <h2 className="mt-4 text-lg font-semibold text-foreground">No leaderboard yet</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Start voting to earn XP and appear here.
            </p>
          </div>
        )}

        {!loading && !error && users.length > 0 && (
          <div className="space-y-3">
            {users.map((user, index) => {
              const rank = index + 1
              const isYou = user.id === authUser?.id
              const avatarSeed = user.username || user.displayName || `user-${user.id}`

              return (
                <motion.div
                  animate={{ opacity: 1, x: 0 }}
                  className={cn(
                    "flex items-center gap-3 rounded-2xl bg-card p-4 ring-1 ring-border/50 transition-shadow hover:shadow-md",
                    isYou && "bg-primary/10 ring-primary/30"
                  )}
                  initial={{ opacity: 0, x: -20 }}
                  key={user.id}
                  transition={{ delay: index * 0.03 }}
                >
                  <div className={cn("flex h-9 w-9 items-center justify-center rounded-xl text-sm font-black", rankTone(rank))}>
                    {rank <= 3 ? <Award className="h-4 w-4" /> : rank}
                  </div>

                  <Avatar className="h-12 w-12">
                    <AvatarImage
                      alt={user.displayName}
                      src={user.avatarUrl ?? `https://api.dicebear.com/9.x/notionists/svg?seed=${avatarSeed}`}
                    />
                    <AvatarFallback>{(user.displayName || user.username)[0]}</AvatarFallback>
                  </Avatar>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <p className="truncate font-semibold text-foreground">
                        {user.displayName || user.username}
                      </p>
                      {isYou && (
                        <span className="rounded-full bg-primary px-2 py-0.5 text-[10px] font-bold text-primary-foreground">
                          YOU
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground">@{user.username}</p>
                    {user.badges?.length > 0 && (
                      <div className="mt-1 flex flex-wrap gap-1">
                        {user.badges.slice(0, 2).map((badge) => (
                          <span
                            className="rounded-full bg-amber-500/10 px-2 py-0.5 text-[10px] font-semibold text-amber-600 dark:text-amber-400"
                            key={badge.id}
                          >
                            {badge.name}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="text-right">
                    <div className="flex items-center justify-end gap-1 font-black text-amber-500">
                      <Zap className="h-4 w-4 fill-current" />
                      {user.xp.toLocaleString()}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Level {user.level ?? levelFromXp(user.xp)} - {user.totalVotes.toLocaleString()} votes
                    </p>
                  </div>
                </motion.div>
              )
            })}
          </div>
        )}
      </div>
    </AppShell>
  )
}
