"use client"

import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Check, Trophy, TrendingUp, Zap } from "lucide-react"
import { useEffect } from "react"
import type { ApiVoteReward } from "@/lib/api"

interface VoteFeedbackProps {
  vote: "yes" | "no" | "option" | null
  reward: ApiVoteReward | null
  streakCount: number
  onComplete: () => void
}

export function VoteFeedback({ vote, reward, streakCount, onComplete }: VoteFeedbackProps) {
  const reducedMotion = useReducedMotion()

  useEffect(() => {
    if (!vote || !reward) return
    const timer = setTimeout(onComplete, reducedMotion ? 2500 : 1800)
    return () => clearTimeout(timer)
  }, [vote, reward, onComplete, reducedMotion])

  return (
    <AnimatePresence>
      {vote && reward && (
        <motion.div
          animate={{ opacity: 1 }}
          aria-label={reward.leveledUp ? `Level up! You reached level ${reward.progression.level}` : `You earned ${reward.awardedXp} XP`}
          aria-live="polite"
          className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 p-4 backdrop-blur-sm"
          exit={{ opacity: 0 }}
          initial={reducedMotion ? false : { opacity: 0 }}
          role="dialog"
        >
          <motion.div
            animate={reducedMotion ? undefined : { scale: 1, y: 0 }}
            className="w-full max-w-sm rounded-3xl bg-card p-6 text-center shadow-2xl ring-1 ring-border"
            initial={reducedMotion ? false : { scale: 0.8, y: 20 }}
          >
            {reward.leveledUp ? <Trophy className="mx-auto h-14 w-14 text-amber-500" /> : <Check className="mx-auto h-14 w-14 text-emerald-500" />}
            <h2 className="mt-3 text-2xl font-bold">
              {reward.leveledUp ? `Level ${reward.progression.level}!` : "Vote counted"}
            </h2>
            <p className="mt-2 inline-flex items-center gap-1 font-bold text-amber-600">
              <Zap className="h-5 w-5" /> +{reward.awardedXp} XP
            </p>
            <div className="mt-3 space-y-1 text-sm text-muted-foreground">
              {reward.events.filter((event) => event.awardedXp > 0).map((event) => (
                <p key={`${event.type}-${event.sourceId}`}>{event.label ?? event.type}: +{event.awardedXp} XP</p>
              ))}
            </div>
            <div className="mt-4 text-left">
              <div className="flex justify-between text-xs"><span>Level {reward.progression.level}</span><span>{reward.progression.totalXp} / {reward.progression.nextLevelXp} XP</span></div>
              <div aria-label="Progress to next level" aria-valuemax={100} aria-valuemin={0} aria-valuenow={reward.progression.progressPercent} className="mt-2 h-2 overflow-hidden rounded-full bg-secondary" role="progressbar">
                <div className="h-full rounded-full bg-primary" style={{ width: `${reward.progression.progressPercent}%` }} />
              </div>
            </div>
            {streakCount > 1 && <p className="mt-3 inline-flex items-center gap-1 text-sm font-semibold"><TrendingUp className="h-4 w-4" />{streakCount} day streak</p>}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
