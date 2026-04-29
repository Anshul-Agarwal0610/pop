"use client"

import { useState } from "react"
import { motion, AnimatePresence } from "framer-motion"
import {
  Bell,
  TrendingUp,
  Sparkles,
  Heart,
  Brain,
  Users,
  Trophy,
  Flame,
  Check,
  CheckCheck,
  ChevronRight,
  Play,
  X,
  Filter,
  Zap,
  Clock,
  Star,
  Gift,
  Target,
  MessageCircle,
  ThumbsUp,
  Crown,
  Lightbulb,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type NotificationType =
  | "trending"
  | "personalized"
  | "health"
  | "mental_health"
  | "social"
  | "achievement"
  | "streak"

type NotificationPriority = "high" | "medium" | "low"

interface Notification {
  id: string
  type: NotificationType
  priority: NotificationPriority
  title: string
  description: string
  time: string
  unread: boolean
  icon: typeof Bell
  iconBg: string
  iconColor: string
  category: string
  categoryColor: string
  thumbnail?: string
  cta?: {
    label: string
    action: string
  }
  avatar?: string
  xpReward?: number
  progress?: number
}

const notifications: Notification[] = [
  {
    id: "1",
    type: "trending",
    priority: "high",
    title: "Hot Topic Alert!",
    description: "\"Should AI have voting rights?\" is exploding with 2.3K votes in the last hour",
    time: "2 min ago",
    unread: true,
    icon: TrendingUp,
    iconBg: "bg-gradient-to-br from-orange-500 to-red-500",
    iconColor: "text-white",
    category: "Trending",
    categoryColor: "bg-orange-500/10 text-orange-600 dark:text-orange-400",
    thumbnail: "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=120&h=80&fit=crop",
    cta: { label: "Vote Now", action: "vote" },
    xpReward: 15,
  },
  {
    id: "2",
    type: "personalized",
    priority: "high",
    title: "Perfect Match for You",
    description: "Based on your tech interests: \"Will quantum computing replace classical computers by 2030?\"",
    time: "5 min ago",
    unread: true,
    icon: Sparkles,
    iconBg: "bg-gradient-to-br from-violet-500 to-purple-600",
    iconColor: "text-white",
    category: "For You",
    categoryColor: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
    thumbnail: "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=120&h=80&fit=crop",
    cta: { label: "Check It Out", action: "view" },
    xpReward: 10,
  },
  {
    id: "3",
    type: "health",
    priority: "medium",
    title: "Daily Wellness Check",
    description: "Quick poll: How would you rate your energy levels today?",
    time: "30 min ago",
    unread: true,
    icon: Heart,
    iconBg: "bg-gradient-to-br from-rose-400 to-pink-500",
    iconColor: "text-white",
    category: "Wellness",
    categoryColor: "bg-rose-500/10 text-rose-600 dark:text-rose-400",
    cta: { label: "Take Poll", action: "poll" },
    xpReward: 20,
  },
  {
    id: "4",
    type: "mental_health",
    priority: "high",
    title: "Mindful Moment",
    description: "Your weekly mental wellness check-in is ready. How are you feeling this week?",
    time: "1 hour ago",
    unread: true,
    icon: Brain,
    iconBg: "bg-gradient-to-br from-teal-400 to-cyan-500",
    iconColor: "text-white",
    category: "Mental Health",
    categoryColor: "bg-teal-500/10 text-teal-600 dark:text-teal-400",
    cta: { label: "Check In", action: "checkin" },
    xpReward: 25,
  },
  {
    id: "5",
    type: "social",
    priority: "medium",
    title: "Emma liked your poll!",
    description: "\"Best productivity app?\" just got a like from Emma Wilson",
    time: "2 hours ago",
    unread: true,
    icon: ThumbsUp,
    iconBg: "bg-gradient-to-br from-blue-400 to-indigo-500",
    iconColor: "text-white",
    category: "Social",
    categoryColor: "bg-blue-500/10 text-blue-600 dark:text-blue-400",
    avatar: "emma",
  },
  {
    id: "6",
    type: "achievement",
    priority: "high",
    title: "Achievement Unlocked!",
    description: "You earned \"Opinion Leader\" - Create 10 polls that each get 100+ votes",
    time: "3 hours ago",
    unread: true,
    icon: Trophy,
    iconBg: "bg-gradient-to-br from-amber-400 to-yellow-500",
    iconColor: "text-white",
    category: "Achievement",
    categoryColor: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
    xpReward: 100,
  },
  {
    id: "7",
    type: "streak",
    priority: "high",
    title: "Streak in Danger!",
    description: "Vote on 1 more poll today to keep your 7-day streak alive",
    time: "4 hours ago",
    unread: false,
    icon: Flame,
    iconBg: "bg-gradient-to-br from-orange-500 to-red-600",
    iconColor: "text-white",
    category: "Streak",
    categoryColor: "bg-orange-500/10 text-orange-600 dark:text-orange-400",
    cta: { label: "Keep Streak", action: "vote" },
    progress: 85,
  },
  {
    id: "8",
    type: "social",
    priority: "low",
    title: "New comment on your poll",
    description: "Alex Chen: \"This is such a thought-provoking question!\"",
    time: "5 hours ago",
    unread: false,
    icon: MessageCircle,
    iconBg: "bg-gradient-to-br from-emerald-400 to-green-500",
    iconColor: "text-white",
    category: "Social",
    categoryColor: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
    avatar: "alex",
  },
  {
    id: "9",
    type: "personalized",
    priority: "medium",
    title: "Community Pick",
    description: "Users like you loved this poll: \"Should coding be taught in elementary school?\"",
    time: "6 hours ago",
    unread: false,
    icon: Lightbulb,
    iconBg: "bg-gradient-to-br from-yellow-400 to-orange-500",
    iconColor: "text-white",
    category: "Recommended",
    categoryColor: "bg-yellow-500/10 text-yellow-600 dark:text-yellow-400",
    thumbnail: "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=120&h=80&fit=crop",
    cta: { label: "Vote", action: "vote" },
  },
  {
    id: "10",
    type: "achievement",
    priority: "medium",
    title: "Level Up!",
    description: "Congratulations! You reached Level 12 - \"Poll Enthusiast\"",
    time: "Yesterday",
    unread: false,
    icon: Crown,
    iconBg: "bg-gradient-to-br from-purple-500 to-pink-500",
    iconColor: "text-white",
    category: "Level Up",
    categoryColor: "bg-purple-500/10 text-purple-600 dark:text-purple-400",
    xpReward: 50,
  },
]

const filterOptions = [
  { label: "All", value: "all", icon: Bell },
  { label: "Trending", value: "trending", icon: TrendingUp },
  { label: "For You", value: "personalized", icon: Sparkles },
  { label: "Wellness", value: "health", icon: Heart },
  { label: "Social", value: "social", icon: Users },
  { label: "Achievements", value: "achievement", icon: Trophy },
]

export default function NotificationsPage() {
  const [activeFilter, setActiveFilter] = useState("all")
  const [dismissedIds, setDismissedIds] = useState<string[]>([])
  const [readIds, setReadIds] = useState<string[]>([])

  const filteredNotifications = notifications.filter((n) => {
    if (dismissedIds.includes(n.id)) return false
    if (activeFilter === "all") return true
    if (activeFilter === "health") return n.type === "health" || n.type === "mental_health"
    return n.type === activeFilter
  })

  const unreadCount = notifications.filter(
    (n) => n.unread && !readIds.includes(n.id) && !dismissedIds.includes(n.id)
  ).length

  const markAsRead = (id: string) => {
    if (!readIds.includes(id)) {
      setReadIds([...readIds, id])
    }
  }

  const markAllAsRead = () => {
    setReadIds(notifications.map((n) => n.id))
  }

  const dismissNotification = (id: string) => {
    setDismissedIds([...dismissedIds, id])
  }

  const isUnread = (notification: Notification) => {
    return notification.unread && !readIds.includes(notification.id)
  }

  const getPriorityIndicator = (priority: NotificationPriority) => {
    switch (priority) {
      case "high":
        return "border-l-4 border-l-primary"
      case "medium":
        return "border-l-4 border-l-amber-500"
      default:
        return ""
    }
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-6"
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="relative">
                <div className="rounded-2xl bg-gradient-to-br from-primary to-primary/80 p-3">
                  <Bell className="h-6 w-6 text-primary-foreground" />
                </div>
                {unreadCount > 0 && (
                  <motion.span
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    className="absolute -right-1 -top-1 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-xs font-bold text-destructive-foreground"
                  >
                    {unreadCount}
                  </motion.span>
                )}
              </div>
              <div>
                <h1 className="text-2xl font-bold text-foreground md:text-3xl">
                  Notifications
                </h1>
                <p className="text-sm text-muted-foreground">
                  {unreadCount > 0 ? `${unreadCount} new updates` : "You're all caught up!"}
                </p>
              </div>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={markAllAsRead}
              className="text-muted-foreground hover:text-foreground"
              disabled={unreadCount === 0}
            >
              <CheckCheck className="mr-2 h-4 w-4" />
              Mark all read
            </Button>
          </div>
        </motion.div>

        {/* Filter Pills */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-6 flex gap-2 overflow-x-auto pb-2 scrollbar-hide"
        >
          {filterOptions.map((filter, index) => (
            <motion.button
              key={filter.value}
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: 0.05 * index }}
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              onClick={() => setActiveFilter(filter.value)}
              className={cn(
                "flex shrink-0 items-center gap-2 rounded-full px-4 py-2 text-sm font-medium transition-all",
                activeFilter === filter.value
                  ? "bg-primary text-primary-foreground shadow-lg shadow-primary/25"
                  : "bg-secondary text-secondary-foreground hover:bg-secondary/80"
              )}
            >
              <filter.icon className="h-4 w-4" />
              {filter.label}
            </motion.button>
          ))}
        </motion.div>

        {/* Notifications List */}
        <div className="space-y-3">
          <AnimatePresence mode="popLayout">
            {filteredNotifications.map((notification, index) => (
              <motion.div
                key={notification.id}
                layout
                initial={{ opacity: 0, x: -20, scale: 0.95 }}
                animate={{ opacity: 1, x: 0, scale: 1 }}
                exit={{ opacity: 0, x: 100, scale: 0.9 }}
                transition={{
                  delay: 0.05 * index,
                  layout: { type: "spring", stiffness: 500, damping: 30 },
                }}
                whileHover={{ scale: 1.01 }}
                onClick={() => markAsRead(notification.id)}
                className={cn(
                  "group relative cursor-pointer overflow-hidden rounded-2xl bg-card p-4 ring-1 ring-border/50 transition-all hover:shadow-lg",
                  isUnread(notification) && "bg-primary/5 ring-primary/20",
                  getPriorityIndicator(notification.priority)
                )}
              >
                {/* Dismiss Button */}
                <motion.button
                  initial={{ opacity: 0 }}
                  whileHover={{ scale: 1.1 }}
                  className="absolute right-3 top-3 rounded-full p-1.5 text-muted-foreground opacity-0 transition-opacity hover:bg-secondary hover:text-foreground group-hover:opacity-100"
                  onClick={(e) => {
                    e.stopPropagation()
                    dismissNotification(notification.id)
                  }}
                >
                  <X className="h-4 w-4" />
                </motion.button>

                <div className="flex gap-4">
                  {/* Icon or Avatar */}
                  {notification.avatar ? (
                    <div className="relative">
                      <Avatar className="h-12 w-12 ring-2 ring-background">
                        <AvatarImage
                          src={`https://api.dicebear.com/9.x/notionists/svg?seed=${notification.avatar}`}
                          alt="User"
                        />
                        <AvatarFallback>
                          {notification.avatar[0].toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <div
                        className={cn(
                          "absolute -bottom-1 -right-1 rounded-full p-1",
                          notification.iconBg
                        )}
                      >
                        <notification.icon className={cn("h-3 w-3", notification.iconColor)} />
                      </div>
                    </div>
                  ) : (
                    <motion.div
                      whileHover={{ rotate: [0, -10, 10, 0] }}
                      transition={{ duration: 0.3 }}
                      className={cn(
                        "flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl shadow-lg",
                        notification.iconBg
                      )}
                    >
                      <notification.icon className={cn("h-6 w-6", notification.iconColor)} />
                    </motion.div>
                  )}

                  {/* Content */}
                  <div className="min-w-0 flex-1">
                    {/* Category Badge */}
                    <div className="mb-1.5 flex items-center gap-2">
                      <span
                        className={cn(
                          "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
                          notification.categoryColor
                        )}
                      >
                        {notification.category}
                      </span>
                      {notification.xpReward && (
                        <span className="inline-flex items-center gap-1 rounded-full bg-amber-500/10 px-2 py-0.5 text-xs font-medium text-amber-600 dark:text-amber-400">
                          <Zap className="h-3 w-3" />+{notification.xpReward} XP
                        </span>
                      )}
                      {isUnread(notification) && (
                        <span className="h-2 w-2 rounded-full bg-primary" />
                      )}
                    </div>

                    {/* Title */}
                    <h3
                      className={cn(
                        "font-semibold text-foreground",
                        isUnread(notification) && "text-primary"
                      )}
                    >
                      {notification.title}
                    </h3>

                    {/* Description */}
                    <p className="mt-0.5 text-sm text-muted-foreground line-clamp-2">
                      {notification.description}
                    </p>

                    {/* Progress Bar (for streak) */}
                    {notification.progress !== undefined && (
                      <div className="mt-3">
                        <div className="mb-1 flex items-center justify-between text-xs">
                          <span className="text-muted-foreground">Daily Progress</span>
                          <span className="font-medium text-foreground">
                            {notification.progress}%
                          </span>
                        </div>
                        <div className="h-2 overflow-hidden rounded-full bg-secondary">
                          <motion.div
                            initial={{ width: 0 }}
                            animate={{ width: `${notification.progress}%` }}
                            transition={{ delay: 0.3, duration: 0.8, ease: "easeOut" }}
                            className="h-full rounded-full bg-gradient-to-r from-orange-500 to-red-500"
                          />
                        </div>
                      </div>
                    )}

                    {/* Thumbnail & CTA Row */}
                    <div className="mt-3 flex items-center gap-3">
                      {notification.thumbnail && (
                        <div className="relative h-16 w-24 shrink-0 overflow-hidden rounded-xl">
                          <img
                            src={notification.thumbnail}
                            alt=""
                            className="h-full w-full object-cover transition-transform group-hover:scale-110"
                          />
                          <div className="absolute inset-0 flex items-center justify-center bg-black/30 opacity-0 transition-opacity group-hover:opacity-100">
                            <Play className="h-6 w-6 text-white" />
                          </div>
                        </div>
                      )}

                      {notification.cta && (
                        <motion.button
                          whileHover={{ scale: 1.05 }}
                          whileTap={{ scale: 0.95 }}
                          className="flex items-center gap-2 rounded-xl bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground shadow-lg shadow-primary/25 transition-colors hover:bg-primary/90"
                          onClick={(e) => e.stopPropagation()}
                        >
                          {notification.cta.label}
                          <ChevronRight className="h-4 w-4" />
                        </motion.button>
                      )}
                    </div>

                    {/* Time */}
                    <div className="mt-2 flex items-center gap-1.5 text-xs text-muted-foreground">
                      <Clock className="h-3 w-3" />
                      {notification.time}
                    </div>
                  </div>
                </div>
              </motion.div>
            ))}
          </AnimatePresence>

          {/* Empty State */}
          {filteredNotifications.length === 0 && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex flex-col items-center justify-center py-16 text-center"
            >
              <div className="mb-4 rounded-3xl bg-secondary p-6">
                <Bell className="h-12 w-12 text-muted-foreground" />
              </div>
              <h3 className="text-lg font-semibold text-foreground">No notifications</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                {activeFilter === "all"
                  ? "You're all caught up! Check back later."
                  : `No ${filterOptions.find((f) => f.value === activeFilter)?.label.toLowerCase()} notifications right now.`}
              </p>
              <Button
                variant="outline"
                className="mt-4"
                onClick={() => setActiveFilter("all")}
              >
                View all notifications
              </Button>
            </motion.div>
          )}
        </div>

        {/* Quick Actions Footer */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="mt-8 rounded-2xl bg-gradient-to-r from-primary/10 via-accent/10 to-primary/10 p-6"
        >
          <div className="flex items-center gap-4">
            <div className="rounded-2xl bg-gradient-to-br from-primary to-accent p-3">
              <Gift className="h-6 w-6 text-primary-foreground" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-foreground">Daily Bonus Available!</h3>
              <p className="text-sm text-muted-foreground">
                Vote on 5 polls today to earn 50 bonus XP
              </p>
            </div>
            <Button size="sm" className="shrink-0">
              <Target className="mr-2 h-4 w-4" />
              Claim
            </Button>
          </div>
        </motion.div>
      </div>
    </AppShell>
  )
}
