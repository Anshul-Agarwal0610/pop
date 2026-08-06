"use client"

import { useEffect, useState } from "react"
import { TopBar } from "./top-bar"
import { Sidebar } from "./sidebar"
import { BottomNav } from "./bottom-nav"
import { ProfileDrawer } from "./profile-drawer"
import { SearchOverlay } from "@/components/search/search-overlay"
import { useAuth } from "@/contexts/auth-context"
import { notificationsApi } from "@/lib/api"
import { cn } from "@/lib/utils"

interface AppShellProps {
  children: React.ReactNode
  hideBottomPadding?: boolean
}

export function AppShell({ children, hideBottomPadding }: AppShellProps) {
  const { isAuthenticated } = useAuth()
  const [isProfileOpen, setIsProfileOpen] = useState(false)
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [notificationCount, setNotificationCount] = useState(0)

  useEffect(() => {
    if (!isAuthenticated) {
      setNotificationCount(0)
      return
    }

    notificationsApi.getAll()
      .then(({ unreadCount }) => setNotificationCount(unreadCount))
      .catch(() => setNotificationCount(0))
  }, [isAuthenticated])

  return (
    <div className="min-h-dvh min-w-0 bg-background">
      {/* Top Bar */}
      <TopBar
        onProfileClick={() => setIsProfileOpen(true)}
        onSearchClick={() => setIsSearchOpen(true)}
        notificationCount={notificationCount}
      />

      {/* Sidebar - Desktop only */}
      <Sidebar />

      {/* Main Content */}
      <main className="min-w-0 pt-16 lg:pl-64">
        <div className={cn(
          "min-h-[calc(100dvh-4rem)] min-w-0",
          hideBottomPadding ? "pb-0" : "safe-bottom lg:pb-6"
        )}>
          {children}
        </div>
      </main>

      {/* Bottom Navigation - Mobile only */}
      <BottomNav />

      {/* Profile Drawer */}
      <ProfileDrawer
        isOpen={isProfileOpen}
        onClose={() => setIsProfileOpen(false)}
      />

      <SearchOverlay
        open={isSearchOpen}
        onOpenChange={setIsSearchOpen}
      />
    </div>
  )
}
