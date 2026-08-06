"use client"

import { motion, AnimatePresence } from "framer-motion"
import { Check, X, Zap, TrendingUp, Trophy } from "lucide-react"
import { useEffect, useState } from "react"
import { cn } from "@/lib/utils"

interface VoteFeedbackProps {
  vote: "yes" | "no" | "option" | null
  xpEarned: number
  streakCount: number
  milestoneReached?: number | null
  onComplete: () => void
}

export function VoteFeedback({
  vote,
  xpEarned,
  streakCount,
  milestoneReached,
  onComplete,
}: VoteFeedbackProps) {
  const [showStreak, setShowStreak] = useState(false)

  useEffect(() => {
    if (vote) {
      const streakTimer = setTimeout(() => setShowStreak(true), 400)
      const completeTimer = setTimeout(onComplete, 1500)
      return () => {
        clearTimeout(streakTimer)
        clearTimeout(completeTimer)
      }
    }
  }, [vote, onComplete])

  return (
    <AnimatePresence>
      {vote && (
        <motion.div
          className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
        >
          {/* Main Vote Animation */}
          <motion.div
            className={cn(
              "relative flex h-32 w-32 items-center justify-center rounded-full",
              vote === "yes" && "bg-emerald-500",
              vote === "no" && "bg-red-500",
              vote === "option" && "bg-primary"
            )}
            initial={{ scale: 0, rotate: -180 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: "spring", stiffness: 260, damping: 20 }}
          >
            {vote === "no" ? (
              <X className="h-16 w-16 text-white" strokeWidth={3} />
            ) : (
              <Check className="h-16 w-16 text-white" strokeWidth={3} />
            )}

            {/* Ripple Effect */}
            <motion.div
              className={cn(
                "absolute inset-0 rounded-full",
                vote === "yes" && "bg-emerald-500",
                vote === "no" && "bg-red-500",
                vote === "option" && "bg-primary"
              )}
              initial={{ scale: 1, opacity: 0.5 }}
              animate={{ scale: 2.5, opacity: 0 }}
              transition={{ duration: 0.6 }}
            />
            <motion.div
              className={cn(
                "absolute inset-0 rounded-full",
                vote === "yes" && "bg-emerald-500",
                vote === "no" && "bg-red-500",
                vote === "option" && "bg-primary"
              )}
              initial={{ scale: 1, opacity: 0.3 }}
              animate={{ scale: 3, opacity: 0 }}
              transition={{ duration: 0.8, delay: 0.1 }}
            />
          </motion.div>

          {/* XP Earned */}
          <motion.div
            className="absolute mt-48 flex items-center gap-2 rounded-full bg-amber-500 px-4 py-2 text-amber-950 shadow-lg"
            initial={{ scale: 0, y: 20 }}
            animate={{ scale: 1, y: 0 }}
            transition={{ delay: 0.2, type: "spring" }}
          >
            <Zap className="h-5 w-5 fill-current" />
            <span className="text-lg font-bold">+{xpEarned} XP</span>
          </motion.div>

          {/* Streak Counter */}
          <AnimatePresence>
            {showStreak && milestoneReached && (
              <motion.div
                className="absolute mt-80 flex items-center gap-2 rounded-full bg-primary px-4 py-2 text-primary-foreground shadow-lg"
                initial={{ scale: 0, y: 20 }}
                animate={{ scale: 1, y: 0 }}
                exit={{ scale: 0 }}
                transition={{ type: "spring" }}
              >
                {streakCount >= 5 ? (
                  <Trophy className="h-5 w-5" />
                ) : (
                  <TrendingUp className="h-5 w-5" />
                )}
                <span className="text-lg font-bold">{milestoneReached}-day milestone!</span>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Floating Particles */}
          {[...Array(8)].map((_, i) => (
            <motion.div
              key={i}
              className={cn(
                "absolute h-3 w-3 rounded-full",
                vote === "yes" && "bg-emerald-400",
                vote === "no" && "bg-red-400",
                vote === "option" && "bg-primary"
              )}
              initial={{
                scale: 0,
                x: 0,
                y: 0,
              }}
              animate={{
                scale: [0, 1, 0],
                x: Math.cos((i * Math.PI * 2) / 8) * 150,
                y: Math.sin((i * Math.PI * 2) / 8) * 150,
              }}
              transition={{
                duration: 0.8,
                delay: 0.1 + i * 0.03,
                ease: "easeOut",
              }}
            />
          ))}
        </motion.div>
      )}
    </AnimatePresence>
  )
}
