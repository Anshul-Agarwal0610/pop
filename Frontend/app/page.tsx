"use client"

import { motion } from "framer-motion"
import {
  TrendingUp,
  Users,
  Clock,
  ChevronRight,
  Zap,
  BarChart3,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

const trendingPolls = [
  {
    id: 1,
    question: "Should remote work become the default?",
    votes: 12453,
    timeLeft: "2h left",
    category: "Work",
    trending: true,
  },
  {
    id: 2,
    question: "Best programming language in 2026?",
    votes: 8921,
    timeLeft: "5h left",
    category: "Tech",
    trending: true,
  },
  {
    id: 3,
    question: "Favorite streaming platform?",
    votes: 15234,
    timeLeft: "1d left",
    category: "Entertainment",
    trending: false,
  },
]

const quickStats = [
  { icon: BarChart3, label: "Polls Voted", value: "127", color: "text-primary" },
  { icon: Users, label: "Following", value: "89", color: "text-accent" },
  { icon: Zap, label: "XP Today", value: "+450", color: "text-amber-500" },
]

export default function HomePage() {
  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        {/* Welcome Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
        >
          <h1 className="text-2xl font-bold text-foreground md:text-3xl">
            Welcome back, Jane!
          </h1>
          <p className="mt-1 text-muted-foreground">
            Ready to share your opinion today?
          </p>
        </motion.div>

        {/* Quick Stats */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 grid grid-cols-3 gap-3"
        >
          {quickStats.map((stat, index) => (
            <motion.div
              key={stat.label}
              whileHover={{ scale: 1.02, y: -2 }}
              whileTap={{ scale: 0.98 }}
              className="flex flex-col items-center rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 + index * 0.05 }}
            >
              <stat.icon className={cn("h-5 w-5", stat.color)} />
              <span className="mt-2 text-xl font-bold text-foreground">
                {stat.value}
              </span>
              <span className="text-[10px] text-muted-foreground">
                {stat.label}
              </span>
            </motion.div>
          ))}
        </motion.div>

        {/* Trending Polls Section */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <div className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-primary" />
              <h2 className="text-lg font-semibold text-foreground">
                Trending Polls
              </h2>
            </div>
            <Button variant="ghost" size="sm" className="text-muted-foreground">
              View all
              <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>

          <div className="space-y-3">
            {trendingPolls.map((poll, index) => (
              <motion.div
                key={poll.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.25 + index * 0.05 }}
                whileHover={{ scale: 1.01, x: 4 }}
                whileTap={{ scale: 0.99 }}
                className="group cursor-pointer rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50 transition-shadow hover:shadow-md"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1">
                    <div className="mb-2 flex items-center gap-2">
                      <span className="rounded-full bg-secondary px-2.5 py-0.5 text-xs font-medium text-secondary-foreground">
                        {poll.category}
                      </span>
                      {poll.trending && (
                        <span className="flex items-center gap-1 text-xs font-medium text-primary">
                          <TrendingUp className="h-3 w-3" />
                          Trending
                        </span>
                      )}
                    </div>
                    <h3 className="font-semibold text-foreground transition-colors group-hover:text-primary">
                      {poll.question}
                    </h3>
                    <div className="mt-2 flex items-center gap-4 text-sm text-muted-foreground">
                      <span className="flex items-center gap-1">
                        <Users className="h-3.5 w-3.5" />
                        {poll.votes.toLocaleString()} votes
                      </span>
                      <span className="flex items-center gap-1">
                        <Clock className="h-3.5 w-3.5" />
                        {poll.timeLeft}
                      </span>
                    </div>
                  </div>
                  <ChevronRight className="h-5 w-5 text-muted-foreground transition-transform group-hover:translate-x-1" />
                </div>
              </motion.div>
            ))}
          </div>
        </motion.div>

        {/* Create Poll CTA */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="mt-6"
        >
          <motion.div
            whileHover={{ scale: 1.01 }}
            whileTap={{ scale: 0.99 }}
            className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary via-primary to-accent p-6 text-primary-foreground shadow-lg"
          >
            {/* Background decoration */}
            <div className="absolute -right-8 -top-8 h-32 w-32 rounded-full bg-white/10" />
            <div className="absolute -bottom-4 -left-4 h-24 w-24 rounded-full bg-white/5" />

            <div className="relative">
              <h3 className="text-xl font-bold">Create Your First Poll</h3>
              <p className="mt-1 text-sm text-primary-foreground/80">
                Ask the world a question and see what they think!
              </p>
              <Button
                variant="secondary"
                className="mt-4 bg-white text-primary hover:bg-white/90"
              >
                <Zap className="mr-2 h-4 w-4" />
                Create Poll
              </Button>
            </div>
          </motion.div>
        </motion.div>
      </div>
    </AppShell>
  )
}
