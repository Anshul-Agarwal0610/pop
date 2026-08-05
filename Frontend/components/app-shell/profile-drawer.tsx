"use client"

import { motion, AnimatePresence } from "framer-motion"
import {
  X,
  Settings,
  LogOut,
  Trophy,
  Target,
  Flame,
  Moon,
  Sun,
  ChevronRight,
} from "lucide-react"
import { useTheme } from "next-themes"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { useAuth } from "@/contexts/auth-context"

interface ProfileDrawerProps {
  isOpen: boolean
  onClose: () => void
}

const achievements = [
  { icon: Trophy, label: "Polls Won", value: "47", color: "text-amber-500" },
  { icon: Target, label: "Accuracy", value: "89%", color: "text-emerald-500" },
  { icon: Flame, label: "Streak", value: "7 days", color: "text-orange-500" },
]

const menuItems = [
  { icon: Settings, label: "Settings", href: "/settings" },
  { icon: LogOut, label: "Sign Out", href: "/logout", danger: true },
]

export function ProfileDrawer({ isOpen, onClose }: ProfileDrawerProps) {
  const { theme, setTheme } = useTheme()
  const { user } = useAuth()

  return (
    <AnimatePresence>
      {isOpen && (
        <>
          {/* Backdrop */}
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="fixed inset-0 z-50 bg-foreground/20 backdrop-blur-sm"
          />

          {/* Drawer */}
          <motion.div
            initial={{ x: "100%" }}
            animate={{ x: 0 }}
            exit={{ x: "100%" }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            className="fixed bottom-0 right-0 top-0 z-50 w-full max-w-sm border-l border-border bg-background shadow-2xl"
          >
            <div className="flex h-full flex-col">
              {/* Header */}
              <div className="flex items-center justify-between border-b border-border p-4">
                <h2 className="text-lg font-semibold">Profile</h2>
                <motion.div
                  whileHover={{ scale: 1.1, rotate: 90 }}
                  whileTap={{ scale: 0.9 }}
                >
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={onClose}
                    className="rounded-xl"
                  >
                    <X className="h-5 w-5" />
                    <span className="sr-only">Close profile</span>
                  </Button>
                </motion.div>
              </div>

              {/* Content */}
              <div className="flex-1 overflow-y-auto p-4">
                {/* User info */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.1 }}
                  className="flex flex-col items-center text-center"
                >
                  <motion.div
                    whileHover={{ scale: 1.05 }}
                    className="relative"
                  >
                    <Avatar className="h-24 w-24 ring-4 ring-primary/20 ring-offset-4 ring-offset-background">
                      <AvatarImage src={user?.avatarUrl} alt="User avatar" />
                      <AvatarFallback className="bg-primary text-2xl text-primary-foreground">
                        {(user?.displayName ?? user?.username ?? "U").slice(0, 2).toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <motion.div
                      className="absolute -bottom-1 -right-1 flex h-8 w-8 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground shadow-lg"
                      animate={{ scale: [1, 1.1, 1] }}
                      transition={{
                        duration: 2,
                        repeat: Infinity,
                        ease: "easeInOut",
                      }}
                    >
                      {user?.progression.level ?? 1}
                    </motion.div>
                  </motion.div>

                  <h3 className="mt-4 text-xl font-bold">{user?.displayName ?? "Guest"}</h3>
                  <p className="text-sm text-muted-foreground">@{user?.username ?? "guest"}</p>

                  {/* Level progress */}
                  <div className="mt-4 w-full max-w-xs">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">Level {user?.progression.level ?? 1}</span>
                      <span className="font-medium text-primary">
                        {user?.progression.totalXp ?? 0} / {user?.progression.nextLevelXp ?? 1000} XP
                      </span>
                    </div>
                    <div className="mt-2 h-2 overflow-hidden rounded-full bg-secondary">
                      <motion.div
                        className="h-full rounded-full bg-gradient-to-r from-primary to-accent"
                        initial={{ width: 0 }}
                        animate={{ width: `${user?.progression.progressPercent ?? 0}%` }}
                        aria-label="Progress to next level"
                        role="progressbar"
                        aria-valuemin={0}
                        aria-valuemax={100}
                        aria-valuenow={user?.progression.progressPercent ?? 0}
                        transition={{ delay: 0.3, duration: 0.8, ease: "easeOut" }}
                      />
                    </div>
                  </div>
                </motion.div>

                {/* Achievements */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.2 }}
                  className="mt-6"
                >
                  <h4 className="mb-3 text-sm font-semibold text-muted-foreground">
                    Achievements
                  </h4>
                  <div className="grid grid-cols-3 gap-3">
                    {achievements.map((achievement, index) => (
                      <motion.div
                        key={achievement.label}
                        whileHover={{ scale: 1.05, y: -2 }}
                        className="flex flex-col items-center rounded-2xl bg-secondary/50 p-4"
                        initial={{ opacity: 0, y: 20 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: 0.2 + index * 0.05 }}
                      >
                        <achievement.icon
                          className={cn("h-6 w-6", achievement.color)}
                        />
                        <span className="mt-2 text-lg font-bold">
                          {achievement.value}
                        </span>
                        <span className="text-[10px] text-muted-foreground">
                          {achievement.label}
                        </span>
                      </motion.div>
                    ))}
                  </div>
                </motion.div>

                {/* Theme Toggle */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.3 }}
                  className="mt-6"
                >
                  <h4 className="mb-3 text-sm font-semibold text-muted-foreground">
                    Appearance
                  </h4>
                  <div className="flex gap-2 rounded-2xl bg-secondary/50 p-2">
                    <Button
                      variant={theme === "light" ? "default" : "ghost"}
                      className="flex-1 rounded-xl"
                      onClick={() => setTheme("light")}
                    >
                      <Sun className="mr-2 h-4 w-4" />
                      Light
                    </Button>
                    <Button
                      variant={theme === "dark" ? "default" : "ghost"}
                      className="flex-1 rounded-xl"
                      onClick={() => setTheme("dark")}
                    >
                      <Moon className="mr-2 h-4 w-4" />
                      Dark
                    </Button>
                  </div>
                </motion.div>

                {/* Menu items */}
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: 0.4 }}
                  className="mt-6 space-y-1"
                >
                  {menuItems.map((item) => (
                    <motion.button
                      key={item.label}
                      whileHover={{ x: 4 }}
                      whileTap={{ scale: 0.98 }}
                      className={cn(
                        "flex w-full items-center justify-between rounded-xl px-4 py-3 transition-colors",
                        item.danger
                          ? "text-destructive hover:bg-destructive/10"
                          : "text-foreground hover:bg-secondary"
                      )}
                    >
                      <div className="flex items-center gap-3">
                        <item.icon className="h-5 w-5" />
                        <span className="font-medium">{item.label}</span>
                      </div>
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </motion.button>
                  ))}
                </motion.div>
              </div>
            </div>
          </motion.div>
        </>
      )}
    </AnimatePresence>
  )
}
