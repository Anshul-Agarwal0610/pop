"use client"

import { motion } from "framer-motion"
import {
  User,
  Trophy,
  Target,
  Flame,
  BarChart3,
  Calendar,
  MapPin,
  Link as LinkIcon,
  Settings,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

const stats = [
  { icon: BarChart3, label: "Polls Created", value: "34", color: "text-primary" },
  { icon: Trophy, label: "Polls Won", value: "47", color: "text-amber-500" },
  { icon: Target, label: "Accuracy", value: "89%", color: "text-emerald-500" },
  { icon: Flame, label: "Day Streak", value: "7", color: "text-orange-500" },
]

const recentPolls = [
  { id: 1, question: "Best coffee shop in town?", votes: 234, status: "active" },
  { id: 2, question: "Favorite weekend activity?", votes: 512, status: "ended" },
  { id: 3, question: "Morning or night person?", votes: 1024, status: "ended" },
]

const badges = [
  { name: "Poll Pioneer", icon: "🚀", earned: true },
  { name: "Trend Setter", icon: "🔥", earned: true },
  { name: "Community Star", icon: "⭐", earned: true },
  { name: "Fact Checker", icon: "✓", earned: false },
]

export default function ProfilePage() {
  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        {/* Profile Header */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
        >
          <div className="relative rounded-2xl bg-gradient-to-br from-primary/20 via-accent/10 to-primary/5 p-6">
            {/* Settings button */}
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-4 top-4 rounded-xl"
            >
              <Settings className="h-5 w-5" />
              <span className="sr-only">Settings</span>
            </Button>

            <div className="flex flex-col items-center text-center sm:flex-row sm:items-start sm:text-left">
              <motion.div whileHover={{ scale: 1.05 }} className="relative">
                <Avatar className="h-24 w-24 ring-4 ring-background shadow-xl">
                  <AvatarImage src="https://api.dicebear.com/9.x/notionists/svg?seed=pollify" alt="Jane Doe" />
                  <AvatarFallback className="bg-primary text-2xl text-primary-foreground">
                    JD
                  </AvatarFallback>
                </Avatar>
                <motion.div
                  className="absolute -bottom-1 -right-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground shadow-lg"
                  animate={{ scale: [1, 1.1, 1] }}
                  transition={{ duration: 2, repeat: Infinity }}
                >
                  12
                </motion.div>
              </motion.div>

              <div className="mt-4 sm:ml-6 sm:mt-0">
                <h1 className="text-2xl font-bold text-foreground">Jane Doe</h1>
                <p className="text-muted-foreground">@janedoe</p>
                
                <div className="mt-3 flex flex-wrap items-center justify-center gap-3 text-sm text-muted-foreground sm:justify-start">
                  <span className="flex items-center gap-1">
                    <Calendar className="h-4 w-4" />
                    Joined Jan 2024
                  </span>
                  <span className="flex items-center gap-1">
                    <MapPin className="h-4 w-4" />
                    San Francisco
                  </span>
                  <span className="flex items-center gap-1">
                    <LinkIcon className="h-4 w-4" />
                    pollify.app/janedoe
                  </span>
                </div>

                {/* Level progress */}
                <div className="mt-4 w-full max-w-xs">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Level 12</span>
                    <span className="font-medium text-primary">2,450 / 3,000 XP</span>
                  </div>
                  <div className="mt-1 h-2 overflow-hidden rounded-full bg-background">
                    <motion.div
                      className="h-full rounded-full bg-gradient-to-r from-primary to-accent"
                      initial={{ width: 0 }}
                      animate={{ width: "82%" }}
                      transition={{ delay: 0.3, duration: 0.8 }}
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </motion.div>

        {/* Stats Grid */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 grid grid-cols-2 gap-3 sm:grid-cols-4"
        >
          {stats.map((stat, index) => (
            <motion.div
              key={stat.label}
              whileHover={{ scale: 1.03, y: -2 }}
              className="flex flex-col items-center rounded-2xl bg-card p-4 shadow-sm ring-1 ring-border/50"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.15 + index * 0.05 }}
            >
              <stat.icon className={cn("h-5 w-5", stat.color)} />
              <span className="mt-2 text-2xl font-bold text-foreground">
                {stat.value}
              </span>
              <span className="text-xs text-muted-foreground">{stat.label}</span>
            </motion.div>
          ))}
        </motion.div>

        {/* Badges */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="mb-6"
        >
          <h2 className="mb-3 text-lg font-semibold text-foreground">Badges</h2>
          <div className="flex flex-wrap gap-2">
            {badges.map((badge) => (
              <motion.div
                key={badge.name}
                whileHover={{ scale: 1.05 }}
                className={cn(
                  "flex items-center gap-2 rounded-full px-4 py-2",
                  badge.earned
                    ? "bg-primary/10 text-foreground"
                    : "bg-muted text-muted-foreground opacity-50"
                )}
              >
                <span>{badge.icon}</span>
                <span className="text-sm font-medium">{badge.name}</span>
              </motion.div>
            ))}
          </div>
        </motion.div>

        {/* Recent Polls */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3 }}
        >
          <h2 className="mb-3 text-lg font-semibold text-foreground">
            Recent Polls
          </h2>
          <div className="space-y-2">
            {recentPolls.map((poll, index) => (
              <motion.div
                key={poll.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.35 + index * 0.05 }}
                whileHover={{ scale: 1.01, x: 4 }}
                className="flex items-center justify-between rounded-2xl bg-card p-4 ring-1 ring-border/50"
              >
                <div>
                  <h3 className="font-medium text-foreground">{poll.question}</h3>
                  <p className="text-sm text-muted-foreground">
                    {poll.votes.toLocaleString()} votes
                  </p>
                </div>
                <span
                  className={cn(
                    "rounded-full px-3 py-1 text-xs font-medium",
                    poll.status === "active"
                      ? "bg-emerald-500/10 text-emerald-500"
                      : "bg-muted text-muted-foreground"
                  )}
                >
                  {poll.status === "active" ? "Active" : "Ended"}
                </span>
              </motion.div>
            ))}
          </div>
        </motion.div>
      </div>
    </AppShell>
  )
}
