"use client"

import { motion } from "framer-motion"
import { Bell, LogIn, LogOut, Search } from "lucide-react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { AppLogo } from "./app-logo"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"

interface TopBarProps {
  onProfileClick: () => void
  onSearchClick: () => void
  notificationCount?: number
}

export function TopBar({
  onProfileClick,
  onSearchClick,
  notificationCount = 0,
}: TopBarProps) {
  const { user, isAuthenticated, logout } = useAuth()
  const router = useRouter()

  const initials = user
    ? user.displayName.split(" ").map((n) => n[0]).join("").slice(0, 2).toUpperCase()
    : "?"

  const avatarSrc = user?.avatarUrl
    ?? `https://api.dicebear.com/9.x/notionists/svg?seed=${user?.username ?? "guest"}`

  return (
    <header className="fixed left-0 right-0 top-0 z-40 h-16 border-b border-border/50 bg-background/80 glass">
      <div className="mx-auto flex h-full max-w-7xl items-center justify-between px-4 lg:px-6">
        {/* Logo */}
        <AppLogo />

        {/* Right side */}
        <div className="flex items-center gap-2">
          <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
            <Button
              aria-label="Search polls"
              className="h-10 w-10 rounded-xl"
              onClick={onSearchClick}
              size="icon"
              type="button"
              variant="ghost"
            >
              <Search className="h-5 w-5" />
            </Button>
          </motion.div>

          {isAuthenticated ? (
            <>
              {/* Notifications */}
              <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                <Button
                  variant="ghost"
                  size="icon"
                  className="relative h-10 w-10 rounded-xl"
                  asChild
                >
                  <Link href="/notifications">
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
                    <span className="sr-only">{notificationCount} notifications</span>
                  </Link>
                </Button>
              </motion.div>

              {/* Profile */}
              <motion.button
                onClick={onProfileClick}
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 0.95 }}
                className="flex items-center gap-2 rounded-xl p-1.5 transition-colors hover:bg-secondary"
              >
                <Avatar className="h-9 w-9 ring-2 ring-primary/20 ring-offset-2 ring-offset-background">
                  <AvatarImage src={avatarSrc} alt={user?.displayName} />
                  <AvatarFallback className="bg-primary text-primary-foreground font-semibold">
                    {initials}
                  </AvatarFallback>
                </Avatar>
                <div className="hidden flex-col items-start md:flex">
                  <span className="text-sm font-semibold leading-tight">
                    {user?.displayName}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {user?.xp ?? 0} XP
                  </span>
                </div>
              </motion.button>

              {/* Logout */}
              <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-10 w-10 rounded-xl text-muted-foreground hover:text-foreground"
                  onClick={() => { logout(); router.push("/login") }}
                  title="Sign out"
                >
                  <LogOut className="h-5 w-5" />
                </Button>
              </motion.div>
            </>
          ) : (
            /* Not logged in → show Sign In button */
            <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
              <Button asChild size="sm" className="gap-1.5 rounded-xl">
                <Link href="/login">
                  <LogIn className="h-4 w-4" />
                  Sign In
                </Link>
              </Button>
            </motion.div>
          )}
        </div>
      </div>
    </header>
  )
}
