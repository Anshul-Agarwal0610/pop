"use client"

import { motion } from "framer-motion"
import { Check } from "lucide-react"
import { cn } from "@/lib/utils"
import { STEPS } from "@/lib/poll-creation"

interface StepIndicatorProps {
  currentStep: number
  onStepClick: (step: number) => void
  completedSteps: Set<number>
}

export function StepIndicator({ currentStep, onStepClick, completedSteps }: StepIndicatorProps) {
  return (
    <div className="w-full">
      {/* Mobile: Dots */}
      <div className="flex items-center justify-center gap-2 md:hidden">
        {STEPS.map((step) => (
          <motion.button
            key={step.id}
            onClick={() => onStepClick(step.id)}
            className={cn(
              "relative h-2.5 w-2.5 rounded-full transition-colors",
              currentStep === step.id
                ? "bg-primary"
                : completedSteps.has(step.id)
                ? "bg-primary/50"
                : "bg-muted-foreground/30"
            )}
            whileTap={{ scale: 0.9 }}
          >
            {currentStep === step.id && (
              <motion.div
                layoutId="activeStepMobile"
                className="absolute inset-0 rounded-full ring-4 ring-primary/30"
                transition={{ type: "spring", stiffness: 500, damping: 30 }}
              />
            )}
          </motion.button>
        ))}
      </div>

      {/* Desktop: Full step bar */}
      <div className="hidden md:block">
        <div className="relative flex items-center justify-between">
          {/* Progress line */}
          <div className="absolute left-0 right-0 top-5 h-0.5 bg-border">
            <motion.div
              className="h-full bg-primary"
              initial={{ width: "0%" }}
              animate={{ width: `${((currentStep - 1) / (STEPS.length - 1)) * 100}%` }}
              transition={{ type: "spring", stiffness: 300, damping: 30 }}
            />
          </div>

          {STEPS.map((step) => {
            const isCompleted = completedSteps.has(step.id)
            const isActive = currentStep === step.id
            const isPast = step.id < currentStep

            return (
              <motion.button
                key={step.id}
                onClick={() => onStepClick(step.id)}
                className="relative z-10 flex flex-col items-center gap-2"
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
              >
                <motion.div
                  className={cn(
                    "flex h-10 w-10 items-center justify-center rounded-full border-2 text-sm font-semibold transition-colors",
                    isActive
                      ? "border-primary bg-primary text-primary-foreground"
                      : isCompleted || isPast
                      ? "border-primary bg-primary/20 text-primary"
                      : "border-border bg-card text-muted-foreground"
                  )}
                  animate={{
                    scale: isActive ? 1.1 : 1,
                  }}
                  transition={{ type: "spring", stiffness: 400, damping: 20 }}
                >
                  {isCompleted || isPast ? (
                    <Check className="h-5 w-5" />
                  ) : (
                    step.id
                  )}
                </motion.div>
                <span
                  className={cn(
                    "text-xs font-medium transition-colors",
                    isActive ? "text-primary" : "text-muted-foreground"
                  )}
                >
                  {step.title}
                </span>
              </motion.button>
            )
          })}
        </div>
      </div>
    </div>
  )
}
