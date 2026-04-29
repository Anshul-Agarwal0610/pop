"use client"

import { motion } from "framer-motion"
import { AlignLeft, SkipForward } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

interface DescriptionStepProps {
  value: string
  onChange: (value: string) => void
  onSkip: () => void
}

export function DescriptionStep({ value, onChange, onSkip }: DescriptionStepProps) {
  const charCount = value.length
  const maxChars = 500

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="flex flex-col gap-6"
    >
      {/* Header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-accent/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <AlignLeft className="h-8 w-8 text-accent" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          Add some context
        </h2>
        <p className="mt-2 text-muted-foreground">
          Help voters understand the topic better (optional)
        </p>
      </div>

      {/* Input Area */}
      <div className="relative">
        <textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Add background info, your perspective, or why this matters..."
          maxLength={maxChars}
          className={cn(
            "min-h-[200px] w-full resize-none rounded-2xl border-2 border-border bg-card p-5 text-base text-foreground placeholder:text-muted-foreground/60 focus:border-primary focus:outline-none transition-colors"
          )}
        />
        
        {/* Character Counter */}
        <div className="absolute bottom-4 right-4">
          <span className="text-sm font-medium text-muted-foreground">
            {charCount}/{maxChars}
          </span>
        </div>
      </div>

      {/* Skip Button */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.3 }}
        className="flex justify-center"
      >
        <Button
          variant="ghost"
          onClick={onSkip}
          className="text-muted-foreground hover:text-foreground"
        >
          <SkipForward className="mr-2 h-4 w-4" />
          Skip this step
        </Button>
      </motion.div>
    </motion.div>
  )
}
