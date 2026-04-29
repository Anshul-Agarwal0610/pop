"use client"

import { motion } from "framer-motion"
import { Globe, Lock, Users, Eye, Shield } from "lucide-react"
import { cn } from "@/lib/utils"

interface PrivacyStepProps {
  value: "public" | "private"
  onChange: (value: "public" | "private") => void
}

const PRIVACY_OPTIONS = [
  {
    value: "public" as const,
    icon: Globe,
    title: "Public",
    description: "Anyone can see and vote on this poll",
    features: [
      { icon: Users, text: "Visible to everyone" },
      { icon: Eye, text: "Appears in feed & search" },
    ],
    color: "text-emerald-500 bg-emerald-500/10 ring-emerald-500/30",
    activeRing: "ring-emerald-500",
  },
  {
    value: "private" as const,
    icon: Lock,
    title: "Private",
    description: "Only people with the link can vote",
    features: [
      { icon: Shield, text: "Hidden from public feed" },
      { icon: Users, text: "Share via link only" },
    ],
    color: "text-violet-500 bg-violet-500/10 ring-violet-500/30",
    activeRing: "ring-violet-500",
  },
]

export function PrivacyStep({ value, onChange }: PrivacyStepProps) {
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
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-violet-500/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <Shield className="h-8 w-8 text-violet-500" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          Who can see this?
        </h2>
        <p className="mt-2 text-muted-foreground">
          Choose your poll&apos;s visibility
        </p>
      </div>

      {/* Privacy Options */}
      <div className="space-y-4">
        {PRIVACY_OPTIONS.map((option, index) => {
          const isSelected = value === option.value
          
          return (
            <motion.button
              key={option.value}
              onClick={() => onChange(option.value)}
              className={cn(
                "relative w-full overflow-hidden rounded-2xl bg-card p-5 text-left ring-2 transition-all",
                isSelected
                  ? option.activeRing
                  : "ring-border hover:ring-border/80"
              )}
              initial={{ opacity: 0, x: index === 0 ? -20 : 20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: index * 0.1 }}
              whileHover={{ scale: 1.01 }}
              whileTap={{ scale: 0.99 }}
            >
              {/* Selected indicator */}
              {isSelected && (
                <motion.div
                  layoutId="privacySelected"
                  className="absolute inset-0 bg-gradient-to-r from-primary/5 to-transparent"
                  transition={{ type: "spring", stiffness: 500, damping: 30 }}
                />
              )}

              <div className="relative flex items-start gap-4">
                {/* Icon */}
                <div className={cn("rounded-xl p-3 ring-1", option.color)}>
                  <option.icon className="h-6 w-6" />
                </div>

                {/* Content */}
                <div className="flex-1">
                  <div className="flex items-center gap-3">
                    <h3 className="text-lg font-semibold text-foreground">
                      {option.title}
                    </h3>
                    {isSelected && (
                      <motion.span
                        initial={{ scale: 0 }}
                        animate={{ scale: 1 }}
                        className="rounded-full bg-primary px-2.5 py-0.5 text-xs font-medium text-primary-foreground"
                      >
                        Selected
                      </motion.span>
                    )}
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">
                    {option.description}
                  </p>

                  {/* Features */}
                  <div className="mt-3 flex flex-wrap gap-3">
                    {option.features.map((feature, i) => (
                      <div
                        key={i}
                        className="flex items-center gap-1.5 text-sm text-muted-foreground"
                      >
                        <feature.icon className="h-3.5 w-3.5" />
                        <span>{feature.text}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Radio indicator */}
                <div
                  className={cn(
                    "flex h-6 w-6 shrink-0 items-center justify-center rounded-full ring-2 transition-colors",
                    isSelected
                      ? "bg-primary ring-primary"
                      : "bg-transparent ring-muted-foreground/30"
                  )}
                >
                  {isSelected && (
                    <motion.div
                      initial={{ scale: 0 }}
                      animate={{ scale: 1 }}
                      className="h-2.5 w-2.5 rounded-full bg-primary-foreground"
                    />
                  )}
                </div>
              </div>
            </motion.button>
          )
        })}
      </div>
    </motion.div>
  )
}
