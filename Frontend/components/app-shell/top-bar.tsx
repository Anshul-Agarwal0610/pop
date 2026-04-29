"use client"

import { motion } from "framer-motion"
import { Bell } from "lucide-react"
import { AppLogo } from "./app-logo"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"

interface TopBarProps {
  onProfileClick: () => void
  notificationCount?: number
}

export function TopBar({ onProfileClick, notificationCount = 3 }: TopBarProps) {
  return (
    <header className="fixed left-0 right-0 top-0 z-40 h-16 border-b border-border/50 bg-background/80 glass">
      <div className="mx-auto flex h-full max-w-7xl items-center justify-between px-4 lg:px-6">
        {/* Logo - always visible */}
        <AppLogo />

        {/* Right side actions */}
        <div className="flex items-center gap-2">
          {/* Notifications */}
          <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
            <Button
              variant="ghost"
              size="icon"
              className="relative h-10 w-10 rounded-xl"
            >
              <Bell className="h-5 w-5" />
              {notificationCount > 0 && (
                <motion.span
                  initial={{ scale: 0 }}
                  animate={{ scale: 1 }}
                  className="absolute -right-0.5 -top-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground"
                >
                  {notificationCount > 9 ? "9+" : notificationCount}
                </motion.span>
              )}
              <span className="sr-only">
                {notificationCount} notifications
              </span>
            </Button>
          </motion.div>

          {/* Profile Avatar */}
          <motion.button
            onClick={onProfileClick}
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            className="flex items-center gap-2 rounded-xl p-1.5 transition-colors hover:bg-secondary"
          >
            <Avatar className="h-9 w-9 ring-2 ring-primary/20 ring-offset-2 ring-offset-background">
              <AvatarImage src="https://api.dicebear.com/9.x/notionists/svg?seed=pollify" alt="User avatar" />
              <AvatarFallback className="bg-primary text-primary-foreground font-semibold">
                JD
              </AvatarFallback>
            </Avatar>
            <div className="hidden flex-col items-start md:flex">
              <span className="text-sm font-semibold leading-tight">Jane Doe</span>
              <span className="text-xs text-muted-foreground">Level 12</span>
            </div>
          </motion.button>
        </div>
      </div>
    </header>
  )
}
