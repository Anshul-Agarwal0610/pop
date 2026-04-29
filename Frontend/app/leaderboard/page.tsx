"use client"

import { useState } from "react"
import { motion, AnimatePresence } from "framer-motion"
import {
  Trophy,
  Medal,
  Crown,
  TrendingUp,
  Flame,
  BarChart3,
  Sparkles,
  Zap,
  Star,
  Target,
  Award,
  ChevronUp,
  ChevronDown,
  Users,
  MessageSquare,
  Share2,
} from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { cn } from "@/lib/utils"

type LeaderboardType = "answered" | "created" | "streak" | "viral" | "reputation"
type TimeFilter = "weekly" | "monthly" | "alltime"

interface LeaderboardUser {
  rank: number
  name: string
  username: string
  avatar: string
  score: number
  scoreLabel: string
  change: number
  badges: string[]
  level: number
  isYou?: boolean
  achievements?: string[]
}

const leaderboardTabs = [
  { id: "answered" as const, label: "Polls Answered", icon: MessageSquare, color: "text-emerald-500" },
  { id: "created" as const, label: "Polls Created", icon: Target, color: "text-blue-500" },
  { id: "streak" as const, label: "Highest Streak", icon: Flame, color: "text-orange-500" },
  { id: "viral" as const, label: "Most Viral", icon: Share2, color: "text-pink-500" },
  { id: "reputation" as const, label: "Reputation", icon: Star, color: "text-amber-500" },
]

const timeFilters = [
  { id: "weekly" as const, label: "This Week" },
  { id: "monthly" as const, label: "This Month" },
  { id: "alltime" as const, label: "All Time" },
]

const generateLeaderboardData = (type: LeaderboardType, time: TimeFilter): LeaderboardUser[] => {
  const baseData: Record<LeaderboardType, LeaderboardUser[]> = {
    answered: [
      { rank: 1, name: "Alex Chen", username: "@alexc", avatar: "alex", score: 2847, scoreLabel: "polls", change: 12, badges: ["speedster", "guru"], level: 42, achievements: ["First 1000", "Speed Demon"] },
      { rank: 2, name: "Sarah Kim", username: "@sarahk", avatar: "sarah", score: 2634, scoreLabel: "polls", change: 8, badges: ["consistent", "helper"], level: 39, achievements: ["Helpful Hand"] },
      { rank: 3, name: "Mike Johnson", username: "@mikej", avatar: "mike", score: 2412, scoreLabel: "polls", change: -2, badges: ["veteran"], level: 37, achievements: ["Early Adopter"] },
      { rank: 4, name: "Emily Davis", username: "@emilyd", avatar: "emily", score: 2198, scoreLabel: "polls", change: 5, badges: ["rising"], level: 34 },
      { rank: 5, name: "Chris Wilson", username: "@chrisw", avatar: "chris", score: 1987, scoreLabel: "polls", change: 3, badges: [], level: 31 },
      { rank: 6, name: "Jane Doe", username: "@janedoe", avatar: "jane", score: 1856, scoreLabel: "polls", change: 15, badges: ["hot"], level: 29, isYou: true },
      { rank: 7, name: "Tom Brown", username: "@tomb", avatar: "tom", score: 1723, scoreLabel: "polls", change: -1, badges: [], level: 27 },
      { rank: 8, name: "Lisa Wang", username: "@lisaw", avatar: "lisa", score: 1645, scoreLabel: "polls", change: 2, badges: [], level: 25 },
      { rank: 9, name: "David Lee", username: "@davidl", avatar: "david", score: 1534, scoreLabel: "polls", change: 0, badges: [], level: 23 },
      { rank: 10, name: "Amy Zhang", username: "@amyz", avatar: "amy", score: 1423, scoreLabel: "polls", change: 4, badges: [], level: 21 },
    ],
    created: [
      { rank: 1, name: "Jordan Taylor", username: "@jordant", avatar: "jordan", score: 847, scoreLabel: "polls", change: 23, badges: ["creator", "trendsetter"], level: 45, achievements: ["Poll Master", "Viral Creator"] },
      { rank: 2, name: "Casey Morgan", username: "@caseym", avatar: "casey", score: 723, scoreLabel: "polls", change: 11, badges: ["influencer"], level: 41, achievements: ["Community Voice"] },
      { rank: 3, name: "Riley Anderson", username: "@rileya", avatar: "riley", score: 654, scoreLabel: "polls", change: 7, badges: ["creative"], level: 38, achievements: ["Original Thinker"] },
      { rank: 4, name: "Alex Chen", username: "@alexc", avatar: "alex", score: 589, scoreLabel: "polls", change: 4, badges: ["guru"], level: 42 },
      { rank: 5, name: "Morgan Smith", username: "@morgans", avatar: "morgan", score: 512, scoreLabel: "polls", change: -3, badges: [], level: 35 },
      { rank: 6, name: "Jane Doe", username: "@janedoe", avatar: "jane", score: 478, scoreLabel: "polls", change: 9, badges: ["rising"], level: 29, isYou: true },
      { rank: 7, name: "Quinn Davis", username: "@quinnd", avatar: "quinn", score: 423, scoreLabel: "polls", change: 2, badges: [], level: 27 },
      { rank: 8, name: "Avery Wilson", username: "@averyw", avatar: "avery", score: 387, scoreLabel: "polls", change: 1, badges: [], level: 24 },
    ],
    streak: [
      { rank: 1, name: "Focus Master", username: "@focusm", avatar: "focus", score: 365, scoreLabel: "days", change: 1, badges: ["legendary", "devoted"], level: 50, achievements: ["Year Streak", "Unstoppable"] },
      { rank: 2, name: "Sarah Kim", username: "@sarahk", avatar: "sarah", score: 234, scoreLabel: "days", change: 1, badges: ["consistent"], level: 39, achievements: ["Half Year"] },
      { rank: 3, name: "Zen Warrior", username: "@zenw", avatar: "zen", score: 189, scoreLabel: "days", change: 1, badges: ["disciplined"], level: 36 },
      { rank: 4, name: "Daily Dan", username: "@dailyd", avatar: "daily", score: 156, scoreLabel: "days", change: 1, badges: ["reliable"], level: 33 },
      { rank: 5, name: "Mike Johnson", username: "@mikej", avatar: "mike", score: 134, scoreLabel: "days", change: 0, badges: [], level: 37 },
      { rank: 6, name: "Habit Hero", username: "@habith", avatar: "habit", score: 112, scoreLabel: "days", change: 1, badges: [], level: 28 },
      { rank: 7, name: "Jane Doe", username: "@janedoe", avatar: "jane", score: 89, scoreLabel: "days", change: 1, badges: ["growing"], level: 29, isYou: true },
      { rank: 8, name: "Steady Steve", username: "@steadys", avatar: "steady", score: 76, scoreLabel: "days", change: 0, badges: [], level: 22 },
    ],
    viral: [
      { rank: 1, name: "Viral Vera", username: "@viralv", avatar: "viral", score: 2400000, scoreLabel: "votes", change: 156, badges: ["viral", "famous"], level: 48, achievements: ["Million Club", "Trend Maker"] },
      { rank: 2, name: "Trending Tara", username: "@trendt", avatar: "trend", score: 1850000, scoreLabel: "votes", change: 89, badges: ["popular"], level: 44, achievements: ["Hot Topic"] },
      { rank: 3, name: "Jordan Taylor", username: "@jordant", avatar: "jordan", score: 1234000, scoreLabel: "votes", change: 45, badges: ["influencer"], level: 45 },
      { rank: 4, name: "Buzz Brian", username: "@buzzb", avatar: "buzz", score: 987000, scoreLabel: "votes", change: 23, badges: [], level: 40 },
      { rank: 5, name: "Casey Morgan", username: "@caseym", avatar: "casey", score: 756000, scoreLabel: "votes", change: 12, badges: [], level: 41 },
      { rank: 6, name: "Hype Hannah", username: "@hypeh", avatar: "hype", score: 623000, scoreLabel: "votes", change: -5, badges: [], level: 35 },
      { rank: 7, name: "Jane Doe", username: "@janedoe", avatar: "jane", score: 234000, scoreLabel: "votes", change: 34, badges: ["rising"], level: 29, isYou: true },
    ],
    reputation: [
      { rank: 1, name: "Elite Emma", username: "@elitee", avatar: "elite", score: 98750, scoreLabel: "XP", change: 234, badges: ["legend", "trusted", "verified"], level: 99, achievements: ["Max Level", "Community Pillar", "Top 1%"] },
      { rank: 2, name: "Alex Chen", username: "@alexc", avatar: "alex", score: 87420, scoreLabel: "XP", change: 189, badges: ["guru", "mentor"], level: 89, achievements: ["Mentor Badge"] },
      { rank: 3, name: "Pro Patrick", username: "@prop", avatar: "pro", score: 76890, scoreLabel: "XP", change: 145, badges: ["expert"], level: 82, achievements: ["Knowledge Seeker"] },
      { rank: 4, name: "Sarah Kim", username: "@sarahk", avatar: "sarah", score: 68340, scoreLabel: "XP", change: 112, badges: ["helper"], level: 75 },
      { rank: 5, name: "Master Maya", username: "@masterm", avatar: "master", score: 59870, scoreLabel: "XP", change: 78, badges: [], level: 68 },
      { rank: 6, name: "Jordan Taylor", username: "@jordant", avatar: "jordan", score: 52340, scoreLabel: "XP", change: 67, badges: [], level: 62 },
      { rank: 7, name: "Wise Will", username: "@wisew", avatar: "wise", score: 45670, scoreLabel: "XP", change: 45, badges: [], level: 56 },
      { rank: 8, name: "Jane Doe", username: "@janedoe", avatar: "jane", score: 38920, scoreLabel: "XP", change: 89, badges: ["rising"], level: 29, isYou: true },
    ],
  }

  const multipliers: Record<TimeFilter, number> = {
    weekly: 0.1,
    monthly: 0.4,
    alltime: 1,
  }

  return baseData[type].map((user) => ({
    ...user,
    score: Math.round(user.score * multipliers[time]),
  }))
}

const badgeConfig: Record<string, { label: string; color: string; icon: typeof Star }> = {
  speedster: { label: "Speed", color: "bg-blue-500", icon: Zap },
  guru: { label: "Guru", color: "bg-purple-500", icon: Sparkles },
  consistent: { label: "Consistent", color: "bg-green-500", icon: Target },
  helper: { label: "Helper", color: "bg-teal-500", icon: Users },
  veteran: { label: "Veteran", color: "bg-slate-500", icon: Medal },
  rising: { label: "Rising", color: "bg-orange-500", icon: TrendingUp },
  hot: { label: "Hot", color: "bg-red-500", icon: Flame },
  creator: { label: "Creator", color: "bg-indigo-500", icon: Target },
  trendsetter: { label: "Trendsetter", color: "bg-pink-500", icon: Sparkles },
  influencer: { label: "Influencer", color: "bg-rose-500", icon: Share2 },
  creative: { label: "Creative", color: "bg-violet-500", icon: Star },
  legendary: { label: "Legend", color: "bg-amber-500", icon: Crown },
  devoted: { label: "Devoted", color: "bg-red-600", icon: Flame },
  disciplined: { label: "Disciplined", color: "bg-emerald-500", icon: Target },
  reliable: { label: "Reliable", color: "bg-cyan-500", icon: Award },
  growing: { label: "Growing", color: "bg-lime-500", icon: TrendingUp },
  viral: { label: "Viral", color: "bg-fuchsia-500", icon: Share2 },
  famous: { label: "Famous", color: "bg-yellow-500", icon: Star },
  popular: { label: "Popular", color: "bg-rose-400", icon: Users },
  legend: { label: "Legend", color: "bg-gradient-to-r from-amber-500 to-orange-500", icon: Crown },
  trusted: { label: "Trusted", color: "bg-blue-600", icon: Award },
  verified: { label: "Verified", color: "bg-sky-500", icon: Star },
  mentor: { label: "Mentor", color: "bg-indigo-600", icon: Users },
  expert: { label: "Expert", color: "bg-purple-600", icon: Sparkles },
}

function formatScore(score: number, label: string): string {
  if (score >= 1000000) {
    return `${(score / 1000000).toFixed(1)}M ${label}`
  }
  if (score >= 1000) {
    return `${(score / 1000).toFixed(1)}K ${label}`
  }
  return `${score.toLocaleString()} ${label}`
}

function PodiumUser({
  user,
  position,
  delay,
}: {
  user: LeaderboardUser
  position: "first" | "second" | "third"
  delay: number
}) {
  const config = {
    first: {
      height: "h-32 md:h-40",
      avatarSize: "h-20 w-20 md:h-24 md:w-24",
      ringColor: "ring-amber-400",
      bgGradient: "from-amber-400/30 via-amber-500/20 to-amber-600/30",
      crownColor: "text-amber-400",
      order: "order-2",
    },
    second: {
      height: "h-24 md:h-32",
      avatarSize: "h-16 w-16 md:h-20 md:w-20",
      ringColor: "ring-slate-400",
      bgGradient: "from-slate-300/30 via-slate-400/20 to-slate-500/30",
      crownColor: "text-slate-400",
      order: "order-1",
    },
    third: {
      height: "h-20 md:h-28",
      avatarSize: "h-14 w-14 md:h-18 md:w-18",
      ringColor: "ring-amber-600",
      bgGradient: "from-amber-600/30 via-amber-700/20 to-amber-800/30",
      crownColor: "text-amber-600",
      order: "order-3",
    },
  }

  const c = config[position]
  const BadgeIcon = position === "first" ? Crown : Medal

  return (
    <motion.div
      initial={{ opacity: 0, y: 60, scale: 0.8 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ delay, type: "spring", stiffness: 200, damping: 20 }}
      className={cn("flex flex-col items-center", c.order)}
    >
      <motion.div
        whileHover={{ scale: 1.05, y: -4 }}
        whileTap={{ scale: 0.98 }}
        className="relative mb-2"
      >
        {position === "first" && (
          <motion.div
            initial={{ opacity: 0, y: -20, rotate: -10 }}
            animate={{ opacity: 1, y: 0, rotate: 0 }}
            transition={{ delay: delay + 0.3, type: "spring" }}
            className="absolute -top-6 left-1/2 z-10 -translate-x-1/2"
          >
            <Crown className="h-8 w-8 text-amber-400 drop-shadow-lg" />
          </motion.div>
        )}
        <Avatar className={cn(c.avatarSize, "ring-4 ring-offset-2 ring-offset-background", c.ringColor)}>
          <AvatarImage src={`https://api.dicebear.com/9.x/notionists/svg?seed=${user.avatar}`} alt={user.name} />
          <AvatarFallback className="text-lg font-bold">{user.name[0]}</AvatarFallback>
        </Avatar>
        <motion.div
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{ delay: delay + 0.2, type: "spring" }}
          className={cn("absolute -bottom-1 -right-1 flex h-7 w-7 items-center justify-center rounded-full bg-card shadow-lg", c.crownColor)}
        >
          <BadgeIcon className="h-4 w-4" />
        </motion.div>
      </motion.div>

      <span className="text-sm font-bold text-foreground md:text-base">{user.name.split(" ")[0]}</span>
      <span className="text-xs text-muted-foreground">Lv.{user.level}</span>
      <span className="mt-0.5 text-xs font-semibold text-primary">{formatScore(user.score, user.scoreLabel)}</span>

      {user.badges.length > 0 && (
        <div className="mt-1 flex gap-1">
          {user.badges.slice(0, 2).map((badge) => {
            const config = badgeConfig[badge]
            if (!config) return null
            return (
              <span
                key={badge}
                className={cn("rounded-full px-1.5 py-0.5 text-[9px] font-semibold text-white", config.color)}
              >
                {config.label}
              </span>
            )
          })}
        </div>
      )}

      <motion.div
        initial={{ height: 0 }}
        animate={{ height: "auto" }}
        transition={{ delay: delay + 0.1, duration: 0.4 }}
        className={cn("mt-3 w-20 overflow-hidden rounded-t-2xl bg-gradient-to-t md:w-24", c.bgGradient, c.height)}
      >
        <div className="flex h-full items-center justify-center">
          <span className="text-3xl font-black text-foreground/60 md:text-4xl">#{user.rank}</span>
        </div>
      </motion.div>
    </motion.div>
  )
}

function RankingCard({ user, index, type }: { user: LeaderboardUser; index: number; type: LeaderboardType }) {
  const tabConfig = leaderboardTabs.find((t) => t.id === type)
  const TabIcon = tabConfig?.icon || Trophy

  return (
    <motion.div
      initial={{ opacity: 0, x: -20 }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ delay: 0.5 + index * 0.05 }}
      whileHover={{ scale: 1.01, x: 4 }}
      className={cn(
        "flex items-center gap-3 rounded-2xl p-3 transition-all md:gap-4 md:p-4",
        user.isYou
          ? "bg-primary/10 ring-2 ring-primary/40"
          : "bg-card ring-1 ring-border/50 hover:ring-border"
      )}
    >
      <div className="flex w-8 items-center justify-center">
        <span className={cn(
          "text-lg font-black",
          user.isYou ? "text-primary" : "text-muted-foreground"
        )}>
          {user.rank}
        </span>
      </div>

      <div className="relative">
        <Avatar className="h-11 w-11 md:h-12 md:w-12">
          <AvatarImage src={`https://api.dicebear.com/9.x/notionists/svg?seed=${user.avatar}`} alt={user.name} />
          <AvatarFallback>{user.name[0]}</AvatarFallback>
        </Avatar>
        <div className="absolute -bottom-0.5 -right-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-secondary text-[10px] font-bold text-secondary-foreground">
          {user.level}
        </div>
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="truncate font-semibold text-foreground">{user.name}</span>
          {user.isYou && (
            <span className="shrink-0 rounded-full bg-primary px-2 py-0.5 text-[10px] font-bold text-primary-foreground">
              YOU
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">{user.username}</span>
          {user.badges.length > 0 && (
            <div className="flex gap-1">
              {user.badges.slice(0, 2).map((badge) => {
                const config = badgeConfig[badge]
                if (!config) return null
                const Icon = config.icon
                return (
                  <span
                    key={badge}
                    className={cn("flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[9px] font-semibold text-white", config.color)}
                  >
                    <Icon className="h-2.5 w-2.5" />
                    {config.label}
                  </span>
                )
              })}
            </div>
          )}
        </div>
      </div>

      <div className="flex flex-col items-end gap-1">
        <div className={cn("flex items-center gap-1 text-sm font-bold", tabConfig?.color)}>
          <TabIcon className="h-4 w-4" />
          {formatScore(user.score, user.scoreLabel)}
        </div>
        <div className={cn(
          "flex items-center gap-0.5 text-xs font-medium",
          user.change > 0 ? "text-emerald-500" : user.change < 0 ? "text-red-500" : "text-muted-foreground"
        )}>
          {user.change > 0 ? (
            <ChevronUp className="h-3 w-3" />
          ) : user.change < 0 ? (
            <ChevronDown className="h-3 w-3" />
          ) : null}
          {user.change !== 0 ? Math.abs(user.change) : "-"}
        </div>
      </div>
    </motion.div>
  )
}

function AchievementHighlight({ achievements }: { achievements: string[] }) {
  if (achievements.length === 0) return null

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="mt-1 flex flex-wrap gap-1"
    >
      {achievements.map((achievement) => (
        <span
          key={achievement}
          className="flex items-center gap-1 rounded-full bg-amber-500/10 px-2 py-0.5 text-[10px] font-medium text-amber-600 dark:text-amber-400"
        >
          <Award className="h-2.5 w-2.5" />
          {achievement}
        </span>
      ))}
    </motion.div>
  )
}

export default function LeaderboardPage() {
  const [activeTab, setActiveTab] = useState<LeaderboardType>("reputation")
  const [timeFilter, setTimeFilter] = useState<TimeFilter>("weekly")

  const leaderboardData = generateLeaderboardData(activeTab, timeFilter)
  const topThree = leaderboardData.slice(0, 3)
  const restOfUsers = leaderboardData.slice(3)
  const currentUser = leaderboardData.find((u) => u.isYou)
  const activeTabConfig = leaderboardTabs.find((t) => t.id === activeTab)

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-4 md:py-6">
        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-4 md:mb-6"
        >
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-2">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-amber-400 to-orange-500 shadow-lg">
                  <Trophy className="h-5 w-5 text-white" />
                </div>
                <div>
                  <h1 className="text-xl font-bold text-foreground md:text-2xl">Leaderboard</h1>
                  <p className="text-xs text-muted-foreground md:text-sm">Compete with the best pollsters</p>
                </div>
              </div>
            </div>
            {currentUser && (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="flex items-center gap-2 rounded-xl bg-primary/10 px-3 py-2"
              >
                <span className="text-xs text-muted-foreground">Your Rank</span>
                <span className="text-lg font-black text-primary">#{currentUser.rank}</span>
              </motion.div>
            )}
          </div>
        </motion.div>

        {/* Leaderboard Type Tabs */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="mb-4 overflow-x-auto scrollbar-hide"
        >
          <div className="flex gap-2 pb-2">
            {leaderboardTabs.map((tab) => {
              const Icon = tab.icon
              const isActive = activeTab === tab.id
              return (
                <motion.button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  whileHover={{ scale: 1.02 }}
                  whileTap={{ scale: 0.98 }}
                  className={cn(
                    "flex shrink-0 items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium transition-all md:px-4",
                    isActive
                      ? "bg-primary text-primary-foreground shadow-lg"
                      : "bg-card text-muted-foreground ring-1 ring-border/50 hover:text-foreground"
                  )}
                >
                  <Icon className={cn("h-4 w-4", isActive ? "" : tab.color)} />
                  <span className="hidden sm:inline">{tab.label}</span>
                  <span className="sm:hidden">{tab.label.split(" ")[0]}</span>
                </motion.button>
              )
            })}
          </div>
        </motion.div>

        {/* Time Filters */}
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.15 }}
          className="mb-6 flex justify-center gap-2"
        >
          {timeFilters.map((filter) => (
            <motion.button
              key={filter.id}
              onClick={() => setTimeFilter(filter.id)}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              className={cn(
                "rounded-full px-4 py-1.5 text-sm font-medium transition-all",
                timeFilter === filter.id
                  ? "bg-secondary text-secondary-foreground"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              {filter.label}
            </motion.button>
          ))}
        </motion.div>

        {/* Top 3 Podium */}
        <AnimatePresence mode="wait">
          <motion.div
            key={`${activeTab}-${timeFilter}`}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="mb-6"
          >
            <div className="flex items-end justify-center gap-2 md:gap-4">
              <PodiumUser user={topThree[1]} position="second" delay={0.2} />
              <PodiumUser user={topThree[0]} position="first" delay={0.1} />
              <PodiumUser user={topThree[2]} position="third" delay={0.3} />
            </div>

            {/* Achievement Highlights for Top 3 */}
            {topThree.some((u) => u.achievements && u.achievements.length > 0) && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.5 }}
                className="mt-4 rounded-2xl bg-gradient-to-r from-amber-500/10 via-orange-500/10 to-red-500/10 p-3"
              >
                <div className="flex items-center gap-2 text-xs font-semibold text-amber-600 dark:text-amber-400">
                  <Award className="h-4 w-4" />
                  Top Achievements
                </div>
                <div className="mt-2 flex flex-wrap gap-2">
                  {topThree.flatMap((u) => u.achievements || []).slice(0, 6).map((achievement, i) => (
                    <span
                      key={`${achievement}-${i}`}
                      className="rounded-full bg-card px-2 py-1 text-xs font-medium text-foreground ring-1 ring-border/50"
                    >
                      {achievement}
                    </span>
                  ))}
                </div>
              </motion.div>
            )}
          </motion.div>
        </AnimatePresence>

        {/* Rest of Leaderboard */}
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.4 }}
          className="space-y-2"
        >
          <div className="flex items-center justify-between px-2">
            <span className="text-sm font-semibold text-muted-foreground">Rankings</span>
            <span className="text-xs text-muted-foreground">{activeTabConfig?.label}</span>
          </div>
          <AnimatePresence mode="wait">
            <motion.div
              key={`${activeTab}-${timeFilter}-list`}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="space-y-2"
            >
              {restOfUsers.map((user, index) => (
                <RankingCard key={user.username} user={user} index={index} type={activeTab} />
              ))}
            </motion.div>
          </AnimatePresence>
        </motion.div>

        {/* Your Position Sticky Footer */}
        {currentUser && currentUser.rank > 3 && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.6 }}
            className="sticky bottom-20 mt-4 md:bottom-4"
          >
            <div className="rounded-2xl bg-card/95 p-3 shadow-xl ring-2 ring-primary/20 backdrop-blur-xl">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10">
                  <span className="text-lg font-black text-primary">#{currentUser.rank}</span>
                </div>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-foreground">Your Position</span>
                    <span className={cn(
                      "flex items-center gap-0.5 text-xs font-medium",
                      currentUser.change > 0 ? "text-emerald-500" : "text-muted-foreground"
                    )}>
                      {currentUser.change > 0 && <ChevronUp className="h-3 w-3" />}
                      {currentUser.change > 0 ? `+${currentUser.change} this week` : "No change"}
                    </span>
                  </div>
                  <span className="text-sm text-muted-foreground">
                    {formatScore(currentUser.score, currentUser.scoreLabel)} to reach #{currentUser.rank - 1}
                  </span>
                </div>
                <motion.button
                  whileHover={{ scale: 1.05 }}
                  whileTap={{ scale: 0.95 }}
                  className="rounded-xl bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground"
                >
                  Climb Up
                </motion.button>
              </div>
            </div>
          </motion.div>
        )}
      </div>
    </AppShell>
  )
}
