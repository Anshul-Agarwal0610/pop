"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { BarChart3, Check, ChevronRight, Clock, Loader2, Plus, RefreshCw, SlidersHorizontal, TrendingUp, Trophy, Users, Zap } from "lucide-react"
import { motion } from "framer-motion"
import { AppShell } from "@/components/app-shell"
import { CategoryBadge } from "@/components/category-badge"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { cn } from "@/lib/utils"
import { POLL_CATEGORIES } from "@/lib/categories"
import { pollsApi, usersApi, type ApiPoll } from "@/lib/api"

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
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [preferredCategories, setPreferredCategories] = useState<string[]>([])
  const [savingPreferences, setSavingPreferences] = useState(false)

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
      setPreferredCategories([])
      return
    }

    usersApi.getCategoryPreferences()
      .then((preferences) => {
        setPreferredCategories(
          preferences.filter((preference) => preference.isExplicit).map((preference) => preference.category)
        )
      })
      .catch(() => setPreferredCategories([]))
  }, [isAuthenticated])

  async function togglePreference(category: string) {
    const next = preferredCategories.includes(category)
      ? preferredCategories.filter((item) => item !== category)
      : [...preferredCategories, category]

    setPreferredCategories(next)
    setSavingPreferences(true)
    try {
      const preferences = await usersApi.updateCategoryPreferences(next)
      setPreferredCategories(
        preferences.filter((preference) => preference.isExplicit).map((preference) => preference.category)
      )
    } finally {
      setSavingPreferences(false)
    }
  }

  async function resetPreferences() {
    setSavingPreferences(true)
    try {
      await usersApi.resetCategoryPreferences()
      setPreferredCategories([])
    } finally {
      setSavingPreferences(false)
    }
  }

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

        {isAuthenticated && (
          <motion.div
            animate={{ opacity: 1, y: 0 }}
            className="mb-6 rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
            initial={{ opacity: 0, y: 20 }}
            transition={{ delay: 0.08 }}
          >
            <div className="mb-3 flex items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <SlidersHorizontal className="h-5 w-5 text-primary" />
                <h2 className="text-lg font-semibold text-foreground">Feed Preferences</h2>
              </div>
              <Button
                className="gap-2 text-muted-foreground"
                disabled={savingPreferences || preferredCategories.length === 0}
                onClick={resetPreferences}
                size="sm"
                type="button"
                variant="ghost"
              >
                <RefreshCw className="h-4 w-4" />
                Reset
              </Button>
            </div>

            <div className="flex flex-wrap gap-2">
              {POLL_CATEGORIES.filter((category) => category.name !== "Health").map((category) => {
                const selected = preferredCategories.includes(category.name)

                return (
                  <button
                    className={cn(
                      "inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-semibold ring-1 transition-colors",
                      selected
                        ? "bg-primary text-primary-foreground ring-primary"
                        : "bg-secondary text-secondary-foreground ring-border hover:bg-secondary/80"
                    )}
                    disabled={savingPreferences}
                    key={category.name}
                    onClick={() => togglePreference(category.name)}
                    type="button"
                  >
                    {selected && <Check className="h-3.5 w-3.5" />}
                    {category.name}
                  </button>
                )
              })}
            </div>
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
