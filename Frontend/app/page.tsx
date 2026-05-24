"use client"

import { useEffect, useMemo, useState } from "react"
import { motion } from "framer-motion"
import {
  TrendingUp,
  Users,
  Clock,
  ChevronRight,
  Zap,
  BarChart3,
  Flame,
  AlertCircle,
  Loader2,
} from "lucide-react"
import Link from "next/link"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { authApi, pollsApi, type ApiPoll, type ApiUser } from "@/lib/api"
import { useAuth } from "@/contexts/auth-context"

function timeLeft(expiresAt: string): string {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return "Expired"

  const minutes = Math.ceil(diff / 60_000)
  if (minutes < 60) return `${minutes}m left`

  const hours = Math.ceil(minutes / 60)
  if (hours < 24) return `${hours}h left`

  return `${Math.ceil(hours / 24)}d left`
}

export default function HomePage() {
  const { user: authUser, isAuthenticated, isLoading: authLoading } = useAuth()
  const [profile, setProfile] = useState<ApiUser | null>(null)
  const [polls, setPolls] = useState<ApiPoll[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadHome() {
      setLoading(true)
      setError(null)

      try {
        const [trending, me] = await Promise.all([
          pollsApi.getTrending(3),
          isAuthenticated ? authApi.getMe().catch(() => null) : Promise.resolve(null),
        ])

        if (cancelled) return
        setPolls(trending)
        setProfile(me)
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Could not load home data")
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    if (!authLoading) loadHome()

    return () => {
      cancelled = true
    }
  }, [authLoading, isAuthenticated])

  const displayUser = profile ?? authUser
  const displayName = displayUser?.displayName || displayUser?.username || "there"

  const quickStats = useMemo(
    () => [
      {
        icon: BarChart3,
        label: "Polls Voted",
        value: String(displayUser?.totalVotes ?? 0),
        color: "text-primary",
      },
      {
        icon: Flame,
        label: "Day Streak",
        value: String(displayUser?.streak ?? 0),
        color: "text-orange-500",
      },
      {
        icon: Zap,
        label: "Total XP",
        value: (displayUser?.xp ?? 0).toLocaleString(),
        color: "text-amber-500",
      },
    ],
    [displayUser]
  )

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        {/* Welcome Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
        >
          <h1 className="text-2xl font-bold text-foreground md:text-3xl">
            Welcome back, {displayName}!
          </h1>
          <p className="mt-1 text-muted-foreground">
            Ready to share your opinion today?
          </p>
        </motion.div>

        {/* Quick Stats */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 grid grid-cols-3 gap-3"
        >
          {quickStats.map((stat, index) => (
            <motion.div
              key={stat.label}
              whileHover={{ scale: 1.02, y: -2 }}
              whileTap={{ scale: 0.98 }}
              className="flex flex-col items-center rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 + index * 0.05 }}
            >
              <stat.icon className={cn("h-5 w-5", stat.color)} />
              <span className="mt-2 text-xl font-bold text-foreground">
                {stat.value}
              </span>
              <span className="text-[10px] text-muted-foreground">
                {stat.label}
              </span>
            </motion.div>
          ))}
        </motion.div>

        {/* Trending Polls Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-primary" />
              <h2 className="text-lg font-semibold text-foreground">
                Trending Polls
              </h2>
            </div>
            <Button asChild variant="ghost" size="sm" className="text-muted-foreground">
              <Link href="/polls">
                View all
                <ChevronRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          </div>

          {loading ? (
            <div className="flex items-center justify-center rounded-2xl bg-card p-8 text-muted-foreground ring-1 ring-border/50">
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Loading polls...
            </div>
          ) : error ? (
            <div className="rounded-2xl bg-card p-5 text-sm text-destructive ring-1 ring-border/50">
              <AlertCircle className="mb-2 h-5 w-5" />
              {error}
            </div>
          ) : polls.length === 0 ? (
            <div className="rounded-2xl bg-card p-5 text-sm text-muted-foreground ring-1 ring-border/50">
              No active polls yet. Create the first one and get the conversation moving.
            </div>
          ) : (
            <div className="space-y-3">
              {polls.map((poll, index) => (
                <motion.div
                  key={poll.id}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: 0.25 + index * 0.05 }}
                  whileHover={{ scale: 1.01, x: 4 }}
                  whileTap={{ scale: 0.99 }}
                  className="group cursor-pointer rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50 transition-shadow hover:shadow-md"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1">
                      <div className="mb-2 flex items-center gap-2">
                        <span className="rounded-full bg-secondary px-2.5 py-0.5 text-xs font-medium text-secondary-foreground">
                          {poll.category}
                        </span>
                        {poll.isTrending && (
                          <span className="flex items-center gap-1 text-xs font-medium text-primary">
                            <TrendingUp className="h-3 w-3" />
                            Trending
                          </span>
                        )}
                      </div>
                      <h3 className="font-semibold text-foreground transition-colors group-hover:text-primary">
                        {poll.question}
                      </h3>
                      <div className="mt-2 flex items-center gap-4 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <Users className="h-3.5 w-3.5" />
                          {poll.totalVotes.toLocaleString()} votes
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-3.5 w-3.5" />
                          {timeLeft(poll.expiresAt)}
                        </span>
                      </div>
                    </div>
                    <ChevronRight className="h-5 w-5 text-muted-foreground transition-transform group-hover:translate-x-1" />
                  </div>
                </motion.div>
              ))}
            </div>
          )}
        </motion.div>

        {/* Create Poll CTA */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="mt-6"
        >
          <motion.div
            whileHover={{ scale: 1.01 }}
            whileTap={{ scale: 0.99 }}
            className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary via-primary to-accent p-6 text-primary-foreground shadow-lg"
          >
            {/* Background decoration */}
            <div className="absolute -right-8 -top-8 h-32 w-32 rounded-full bg-white/10" />
            <div className="absolute -bottom-4 -left-4 h-24 w-24 rounded-full bg-white/5" />

            <div className="relative">
              <h3 className="text-xl font-bold">Create Your First Poll</h3>
              <p className="mt-1 text-sm text-primary-foreground/80">
                Ask the world a question and see what they think!
              </p>
              <Button
                asChild
                variant="secondary"
                className="mt-4 bg-white text-primary hover:bg-white/90"
              >
                <Link href="/create">
                  <Zap className="mr-2 h-4 w-4" />
                  Create Poll
                </Link>
              </Button>
            </div>
          </motion.div>
        </motion.div>
      </div>
    </AppShell>
  )
}
