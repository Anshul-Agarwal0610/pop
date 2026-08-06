"use client"

import { useState, useCallback, useEffect } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { RefreshCw, Sparkles, AlertCircle, Loader2 } from "lucide-react"
import { FEED_CATEGORIES, normalizeCategoryName } from "@/lib/categories"
import { CategoryChips } from "./category-chips"
import { PollCard } from "./poll-card"
import { VoteFeedback } from "./vote-feedback"
import { StreakCounter } from "./streak-counter"
import { ProgressIndicator } from "./progress-indicator"
import { Button } from "@/components/ui/button"
import { usePolls } from "@/hooks/use-polls"
import type { Poll } from "@/lib/poll-data"

export function PollFeed() {
  const [selectedCategory, setSelectedCategory] = useState("All")
  const feedCategory = selectedCategory === "All" ? undefined : selectedCategory
  const { polls, loading, error, castVote, loadMore, hasMore } = usePolls(feedCategory)
  const [currentIndex, setCurrentIndex]   = useState(0)
  const [streak, setStreak]               = useState(0)
  const [totalXp, setTotalXp]             = useState(1250)
  const [currentVote, setCurrentVote]     = useState<"yes" | "no" | "option" | null>(null)
  const [sessionXp, setSessionXp]         = useState(0)

  const currentPoll: Poll | undefined = polls[currentIndex]
  const hasMorePolls = currentIndex < polls.length

  useEffect(() => {
    const saved = window.localStorage.getItem("poll-feed-category")
    if (!saved) return
    if (saved === "All") {
      setSelectedCategory(saved)
      return
    }

    const normalized = normalizeCategoryName(saved)
    if (FEED_CATEGORIES.includes(normalized as (typeof FEED_CATEGORIES)[number])) {
      setSelectedCategory(normalized)
    }
  }, [])

  const handleCategorySelect = useCallback((category: string) => {
    setSelectedCategory(category)
    setCurrentIndex(0)
    setCurrentVote(null)
    window.localStorage.setItem("poll-feed-category", category)
  }, [])

  const handleVote = useCallback(
    async (optionId: number, feedback: "yes" | "no" | "option") => {
      if (!currentPoll || currentVote) return

      setCurrentVote(feedback)
      setStreak((s) => s + 1)
      setTotalXp((xp) => xp + currentPoll.xpReward)
      setSessionXp((xp) => xp + currentPoll.xpReward)

      await castVote(currentPoll.id, optionId)
    },
    [currentPoll, currentVote, castVote]
  )

  const handleFeedbackComplete = useCallback(() => {
    setCurrentVote(null)
    setCurrentIndex((i) => {
      const next = i + 1
      // Pre-fetch more when nearing the end
      if (next >= polls.length - 3 && hasMore) {
        loadMore()
      }
      return next
    })
  }, [polls.length, hasMore, loadMore])

  const handleReset = () => {
    setCurrentIndex(0)
    setSessionXp(0)
  }

  // ── Loading state ─────────────────────────────────────────────────────────
  if (loading) {
    return (
      <div className="relative flex h-full flex-col">
        <CategoryChips selected={selectedCategory} onSelect={handleCategorySelect} />
        <div className="flex flex-1 flex-col items-center justify-center gap-4">
          <Loader2 className="h-10 w-10 animate-spin text-primary" />
          <p className="text-sm text-muted-foreground">Loading polls...</p>
        </div>
      </div>
    )
  }

  // ── Error state ───────────────────────────────────────────────────────────
  if (error) {
    return (
      <div className="relative flex h-full flex-col">
        <CategoryChips selected={selectedCategory} onSelect={handleCategorySelect} />
        <div className="flex flex-1 flex-col items-center justify-center gap-4 px-8 text-center">
          <AlertCircle className="h-10 w-10 text-destructive" />
          <p className="font-semibold text-foreground">Could not load polls</p>
          <p className="text-sm text-muted-foreground">{error}</p>
          <Button variant="outline" onClick={() => window.location.reload()} className="gap-2">
            <RefreshCw className="h-4 w-4" />
            Retry
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="relative flex h-full flex-col">
      <CategoryChips selected={selectedCategory} onSelect={handleCategorySelect} />

      {/* Header Stats */}
      <motion.div
        className="flex items-center justify-between px-4 py-3"
        initial={{ opacity: 0, y: -10 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <StreakCounter streak={streak} totalXp={totalXp} />
        <ProgressIndicator current={currentIndex} total={polls.length} />
      </motion.div>

      {/* Card Stack */}
      <div className="relative mx-auto min-h-0 w-full max-w-md flex-1 px-0 sm:px-4">
        <AnimatePresence mode="popLayout">
          {hasMorePolls ? (
            polls.slice(currentIndex, currentIndex + 2)
              .reverse()
              .map((poll, idx) => (
                <PollCard
                  key={poll.id}
                  poll={poll}
                  onVote={handleVote}
                  isActive={idx === (currentIndex < polls.length - 1 ? 1 : 0)}
                />
              ))
          ) : (
            <motion.div
              className="absolute inset-x-4 top-1/2 -translate-y-1/2 text-center md:inset-x-0"
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ type: "spring" }}
            >
              <div className="rounded-3xl bg-card p-8 shadow-xl ring-1 ring-border/50">
                <motion.div
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  transition={{ delay: 0.2, type: "spring" }}
                >
                  <Sparkles className="mx-auto h-16 w-16 text-primary" />
                </motion.div>
                <h2 className="mt-4 text-2xl font-bold text-foreground">
                  All Caught Up!
                </h2>
                <p className="mt-2 text-muted-foreground">
                  You&apos;ve voted on all trending polls. Check back later for more!
                </p>
                <div className="mt-6 flex flex-col gap-3">
                  <motion.div
                    className="rounded-xl bg-primary/10 p-4"
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: 0.3 }}
                  >
                    <div className="text-3xl font-bold text-primary">
                      +{sessionXp} XP
                    </div>
                    <div className="text-sm text-muted-foreground">
                      earned this session
                    </div>
                  </motion.div>
                  <Button onClick={handleReset} variant="outline" className="gap-2">
                    <RefreshCw className="h-4 w-4" />
                    Start Over
                  </Button>
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Swipe Hint */}
      {hasMorePolls && currentIndex === 0 && (
        <motion.div
          className="absolute bottom-4 left-1/2 -translate-x-1/2 text-center"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 1 }}
        >
          <motion.p
            className="text-xs text-muted-foreground"
            animate={{ x: [-5, 5, -5] }}
            transition={{ repeat: Infinity, duration: 2 }}
          >
            Swipe right for YES, left for NO
          </motion.p>
        </motion.div>
      )}

      {/* Vote Feedback Overlay */}
      <VoteFeedback
        vote={currentVote}
        xpEarned={currentPoll?.xpReward || 0}
        streakCount={streak}
        onComplete={handleFeedbackComplete}
      />
    </div>
  )
}
