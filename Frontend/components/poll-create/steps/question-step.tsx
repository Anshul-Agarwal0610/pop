"use client"

import { motion } from "framer-motion"
import { Sparkles, Lightbulb } from "lucide-react"
import { cn } from "@/lib/utils"

interface QuestionStepProps {
  value: string
  onChange: (value: string) => void
  error?: string
}

const SUGGESTIONS = [
  "Should AI be regulated by governments?",
  "Is remote work better than office work?",
  "Should social media have age restrictions?",
  "Is electric the future of transportation?",
]

export function QuestionStep({ value, onChange, error }: QuestionStepProps) {
  const charCount = value.length
  const maxChars = 280
  const isNearLimit = charCount > maxChars * 0.8

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
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <Sparkles className="h-8 w-8 text-primary" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          What&apos;s your question?
        </h2>
        <p className="mt-2 text-muted-foreground">
          Ask something that sparks conversation
        </p>
      </div>

      {/* Input Area */}
      <div className="relative">
        <textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Type your poll question here..."
          maxLength={maxChars}
          className={cn(
            "min-h-[160px] w-full resize-none rounded-2xl border-2 bg-card p-5 text-lg font-medium text-foreground placeholder:text-muted-foreground/60 focus:outline-none transition-colors",
            error
              ? "border-destructive focus:border-destructive"
              : "border-border focus:border-primary"
          )}
        />
        
        {/* Character Counter */}
        <div className="absolute bottom-4 right-4 flex items-center gap-2">
          <span
            className={cn(
              "text-sm font-medium",
              isNearLimit ? "text-amber-500" : "text-muted-foreground"
            )}
          >
            {charCount}/{maxChars}
          </span>
          {charCount >= maxChars && (
            <span className="text-xs text-destructive">Max reached</span>
          )}
        </div>

        {/* Error Message */}
        {error && (
          <motion.p
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            className="mt-2 text-sm text-destructive"
          >
            {error}
          </motion.p>
        )}
      </div>

      {/* Suggestions */}
      <div>
        <div className="mb-3 flex items-center gap-2 text-muted-foreground">
          <Lightbulb className="h-4 w-4" />
          <span className="text-sm font-medium">Need inspiration?</span>
        </div>
        <div className="flex flex-wrap gap-2">
          {SUGGESTIONS.map((suggestion, index) => (
            <motion.button
              key={index}
              onClick={() => onChange(suggestion)}
              className="rounded-full bg-secondary px-4 py-2 text-sm text-secondary-foreground transition-colors hover:bg-secondary/80"
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: 0.1 + index * 0.05 }}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
            >
              {suggestion}
            </motion.button>
          ))}
        </div>
      </div>
    </motion.div>
  )
}
