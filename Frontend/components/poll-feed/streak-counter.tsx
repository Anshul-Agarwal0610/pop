"use client"

import { motion } from "framer-motion"
import { Flame, Zap } from "lucide-react"
import { cn } from "@/lib/utils"

interface StreakCounterProps {
  streak: number
  totalXp: number
}

export function StreakCounter({ streak, totalXp }: StreakCounterProps) {
  return (
    <div className="flex items-center gap-3">
      {/* Streak */}
      <motion.div
        className={cn(
          "flex items-center gap-1.5 rounded-full px-3 py-1.5 shadow-sm",
          streak > 0
            ? "bg-orange-500/10 text-orange-500"
            : "bg-muted text-muted-foreground"
        )}
        whileHover={{ scale: 1.05 }}
        layout
      >
        <motion.div
          animate={
            streak > 0
              ? {
                  scale: [1, 1.2, 1],
                }
              : {}
          }
          transition={{ repeat: Infinity, duration: 1.5 }}
        >
          <Flame
            className={cn("h-4 w-4", streak > 0 && "fill-orange-500")}
          />
        </motion.div>
        <span className="text-sm font-bold">{streak}</span>
      </motion.div>

      {/* Total XP */}
      <motion.div
        className="flex items-center gap-1.5 rounded-full bg-amber-500/10 px-3 py-1.5 text-amber-600 shadow-sm dark:text-amber-400"
        whileHover={{ scale: 1.05 }}
        layout
      >
        <Zap className="h-4 w-4 fill-current" />
        <motion.span
          className="text-sm font-bold"
          key={totalXp}
          initial={{ scale: 1.3 }}
          animate={{ scale: 1 }}
        >
          {totalXp.toLocaleString()}
        </motion.span>
      </motion.div>
    </div>
  )
}
