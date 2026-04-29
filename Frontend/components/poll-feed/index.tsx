"use client"

import { useState, useCallback } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { RefreshCw, Sparkles } from "lucide-react"
import { PollCard } from "./poll-card"
import { VoteFeedback } from "./vote-feedback"
import { StreakCounter } from "./streak-counter"
import { ProgressIndicator } from "./progress-indicator"
import { MOCK_POLLS } from "@/lib/poll-data"
import { Button } from "@/components/ui/button"

export function PollFeed() {
  const [currentIndex, setCurrentIndex] = useState(0)
  const [streak, setStreak] = useState(0)
  const [totalXp, setTotalXp] = useState(1250)
  const [currentVote, setCurrentVote] = useState<"yes" | "no" | null>(null)
  const [votedPolls, setVotedPolls] = useState<Set<string>>(new Set())

  const currentPoll = MOCK_POLLS[currentIndex]
  const hasMorePolls = currentIndex < MOCK_POLLS.length

  const handleVote = useCallback(
    (vote: "yes" | "no") => {
      if (!currentPoll || currentVote) return

      setCurrentVote(vote)
      setStreak((s) => s + 1)
      setTotalXp((xp) => xp + currentPoll.xpReward)
      setVotedPolls((prev) => new Set(prev).add(currentPoll.id))
    },
    [currentPoll, currentVote]
  )

  const handleFeedbackComplete = useCallback(() => {
    setCurrentVote(null)
    setCurrentIndex((i) => i + 1)
  }, [])

  const handleReset = () => {
    setCurrentIndex(0)
    setVotedPolls(new Set())
  }

  return (
    <div className="relative flex h-full flex-col">
      {/* Header Stats */}
      <motion.div
        className="flex items-center justify-between px-4 py-3"
        initial={{ opacity: 0, y: -10 }}
        animate={{ opacity: 1, y: 0 }}
      >
        <StreakCounter streak={streak} totalXp={totalXp} />
        <ProgressIndicator current={currentIndex} total={MOCK_POLLS.length} />
      </motion.div>

      {/* Card Stack */}
      <div className="relative mx-auto flex-1 w-full max-w-md px-0 md:px-4">
        <AnimatePresence mode="popLayout">
          {hasMorePolls ? (
            MOCK_POLLS.slice(currentIndex, currentIndex + 2)
              .reverse()
              .map((poll, idx) => (
                <PollCard
                  key={poll.id}
                  poll={poll}
                  onVote={handleVote}
                  isActive={idx === (currentIndex < MOCK_POLLS.length - 1 ? 1 : 0)}
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
                      +{MOCK_POLLS.reduce((acc, p) => acc + p.xpReward, 0)} XP
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
