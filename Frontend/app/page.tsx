"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { BarChart3, ChevronRight, Clock, Loader2, Plus, Target, TrendingUp, Trophy, Users, Zap } from "lucide-react"
import { motion } from "framer-motion"
import { AppShell } from "@/components/app-shell"
import { CategoryBadge } from "@/components/category-badge"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { challengesApi, pollsApi, type ApiChallenge, type ApiPoll } from "@/lib/api"

function timeLeft(iso: string) {
  const diff = new Date(iso).getTime() - Date.now()
  if (diff <= 0) return "Ended"
  const mins = Math.floor(diff / 60_000)
  if (mins < 60) return `${mins}m left`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h left`
  return `${Math.floor(hrs / 24)}d left`
}

export default function HomePage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const [polls, setPolls] = useState<ApiPoll[]>([])
  const [challenges, setChallenges] = useState<ApiChallenge[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    pollsApi.getTrending(5)
      .then(setPolls)
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!isAuthenticated) {
      setChallenges([])
      return
    }

    challengesApi.getActive()
      .then(setChallenges)
      .catch(() => setChallenges([]))
  }, [isAuthenticated])

  const displayName = isAuthenticated ? user?.displayName ?? user?.username : "there"
  const stats = [
    { icon: BarChart3, label: "Polls Voted", value: user?.totalVotes ?? 0, color: "text-primary" },
    { icon: Plus, label: "Polls Created", value: user?.pollsCreated ?? 0, color: "text-emerald-500" },
    { icon: Zap, label: "Total XP", value: user?.xp ?? 0, color: "text-amber-500" },
  ]

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        <motion.div
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
          initial={{ opacity: 0, y: 20 }}
        >
          <h1 className="text-2xl font-bold text-foreground md:text-3xl">
            Welcome back, {authLoading ? "..." : displayName}!
          </h1>
          <p className="mt-1 text-muted-foreground">
            {isAuthenticated ? "Your live activity is ready." : "Sign in to track XP, streaks, and poll history."}
          </p>
        </motion.div>

        {isAuthenticated && challenges.length > 0 && (
          <motion.div
            animate={{ opacity: 1, y: 0 }}
            className="mb-6 space-y-3"
            initial={{ opacity: 0, y: 20 }}
            transition={{ delay: 0.15 }}
          >
            <div className="flex items-center gap-2">
              <Target className="h-5 w-5 text-primary" />
              <h2 className="text-lg font-semibold text-foreground">Daily Challenges</h2>
            </div>

            {challenges.map((challenge) => {
              const progress = Math.min(100, Math.round((challenge.currentVotes / challenge.requiredVotes) * 100))
              const remaining = Math.max(0, challenge.requiredVotes - challenge.currentVotes)

              return (
                <div
                  className="rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
                  key={challenge.challengeId}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="font-semibold text-foreground">{challenge.title}</p>
                      <p className="mt-1 text-sm text-muted-foreground">
                        {challenge.isCompleted
                          ? `Completed. +${challenge.rewardXp} XP earned.`
                          : `${remaining} more vote${remaining === 1 ? "" : "s"} for +${challenge.rewardXp} XP`}
                      </p>
                    </div>
                    {challenge.rewardBadge && (
                      <span className="rounded-full bg-amber-500/10 px-2.5 py-1 text-xs font-semibold text-amber-600 dark:text-amber-400">
                        {challenge.rewardBadge}
                      </span>
                    )}
                  </div>
                  <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-secondary">
                    <div
                      className="h-full rounded-full bg-primary transition-all"
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                  <div className="mt-2 text-xs text-muted-foreground">
                    {challenge.currentVotes}/{challenge.requiredVotes} votes today
                  </div>
                </div>
              )
            })}
          </motion.div>
        )}

        <motion.div
          animate={{ opacity: 1, y: 0 }}
          className="mb-6 grid grid-cols-3 gap-3"
          initial={{ opacity: 0, y: 20 }}
          transition={{ delay: 0.1 }}
        >
          {stats.map((stat, index) => (
            <motion.div
              animate={{ opacity: 1, y: 0 }}
              className="flex flex-col items-center rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
              initial={{ opacity: 0, y: 20 }}
              key={stat.label}
              transition={{ delay: 0.1 + index * 0.05 }}
              whileHover={{ scale: 1.02, y: -2 }}
            >
              <stat.icon className={`h-5 w-5 ${stat.color}`} />
              <span className="mt-2 text-xl font-bold text-foreground">
                {Number(stat.value).toLocaleString()}
              </span>
              <span className="text-center text-[10px] text-muted-foreground">
                {stat.label}
              </span>
            </motion.div>
          ))}
        </motion.div>

        <motion.div
          animate={{ opacity: 1, y: 0 }}
          initial={{ opacity: 0, y: 20 }}
          transition={{ delay: 0.2 }}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-primary" />
              <h2 className="text-lg font-semibold text-foreground">
                Trending Polls
              </h2>
            </div>
            <Button asChild className="text-muted-foreground" size="sm" variant="ghost">
              <Link href="/polls">
                View all
                <ChevronRight className="ml-1 h-4 w-4" />
              </Link>
            </Button>
          </div>

          {loading && (
            <div className="flex items-center justify-center gap-2 rounded-2xl bg-card py-12 text-sm text-muted-foreground ring-1 ring-border/50">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading trending polls...
            </div>
          )}

          {!loading && error && (
            <div className="rounded-2xl bg-destructive/10 p-5 text-center text-sm text-destructive">
              Could not load trending polls.
            </div>
          )}

          {!loading && !error && polls.length === 0 && (
            <div className="rounded-2xl bg-card p-8 text-center ring-1 ring-border/50">
              <Trophy className="mx-auto h-10 w-10 text-muted-foreground" />
              <p className="mt-3 font-semibold text-foreground">No trending polls yet</p>
              <p className="mt-1 text-sm text-muted-foreground">Create or vote on polls to get the feed moving.</p>
            </div>
          )}

          <div className="space-y-3">
            {polls.map((poll, index) => (
              <motion.div
                animate={{ opacity: 1, x: 0 }}
                initial={{ opacity: 0, x: -20 }}
                key={poll.id}
                transition={{ delay: 0.25 + index * 0.05 }}
                whileHover={{ scale: 1.01, x: 4 }}
              >
                <Link
                  className="group block rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50 transition-shadow hover:shadow-md"
                  href={`/polls/${poll.id}`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="mb-2 flex items-center gap-2">
                        <CategoryBadge category={poll.category} className="px-2.5 py-0.5" />
                        {poll.isTrending && (
                          <span className="flex items-center gap-1 text-xs font-medium text-primary">
                            <TrendingUp className="h-3 w-3" />
                            Trending
                          </span>
                        )}
                      </div>
                      <h3 className="line-clamp-2 font-semibold text-foreground transition-colors group-hover:text-primary">
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
                </Link>
              </motion.div>
            ))}
          </div>
        </motion.div>

        <motion.div
          animate={{ opacity: 1, y: 0 }}
          className="mt-6"
          initial={{ opacity: 0, y: 20 }}
          transition={{ delay: 0.4 }}
        >
          <div className="relative overflow-hidden rounded-2xl bg-primary p-6 text-primary-foreground shadow-lg">
            <h3 className="text-xl font-bold">Create a poll</h3>
            <p className="mt-1 text-sm text-primary-foreground/80">
              Ask the community a question and watch the votes come in.
            </p>
            <Button asChild className="mt-4 bg-white text-primary hover:bg-white/90" variant="secondary">
              <Link href="/create">
                <Zap className="mr-2 h-4 w-4" />
                Create Poll
              </Link>
            </Button>
          </div>
        </motion.div>
      </div>
    </AppShell>
  )
}
