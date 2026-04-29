"use client"

import { motion } from "framer-motion"
import { cn } from "@/lib/utils"

interface ProgressIndicatorProps {
  current: number
  total: number
}

export function ProgressIndicator({ current, total }: ProgressIndicatorProps) {
  return (
    <div className="flex items-center gap-2">
      {/* Dots */}
      <div className="flex items-center gap-1.5">
        {Array.from({ length: Math.min(total, 6) }).map((_, i) => {
          const isCompleted = i < current
          const isCurrent = i === current
          return (
            <motion.div
              key={i}
              className={cn(
                "rounded-full transition-colors",
                isCompleted
                  ? "bg-primary"
                  : isCurrent
                    ? "bg-primary/50"
                    : "bg-muted"
              )}
              initial={false}
              animate={{
                width: isCurrent ? 24 : 8,
                height: 8,
              }}
              transition={{ type: "spring", stiffness: 300, damping: 25 }}
            />
          )
        })}
      </div>

      {/* Counter */}
      <span className="ml-2 text-xs font-medium text-muted-foreground">
        {current + 1} / {total}
      </span>
    </div>
  )
}
