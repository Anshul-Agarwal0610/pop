"use client"

import { useState } from "react"
import { TopBar } from "./top-bar"
import { Sidebar } from "./sidebar"
import { BottomNav } from "./bottom-nav"
import { ProfileDrawer } from "./profile-drawer"
import { SearchOverlay } from "@/components/search/search-overlay"
import { cn } from "@/lib/utils"

interface AppShellProps {
  children: React.ReactNode
  hideBottomPadding?: boolean
}

export function AppShell({ children, hideBottomPadding }: AppShellProps) {
  const [isProfileOpen, setIsProfileOpen] = useState(false)
  const [isSearchOpen, setIsSearchOpen] = useState(false)

  return (
    <div className="min-h-screen bg-background">
      {/* Top Bar */}
      <TopBar
        onProfileClick={() => setIsProfileOpen(true)}
        onSearchClick={() => setIsSearchOpen(true)}
        notificationCount={3}
      />

      {/* Sidebar - Desktop only */}
      <Sidebar />

      {/* Main Content */}
      <main className="pt-16 md:pl-64">
        <div className={cn(
          "min-h-[calc(100vh-4rem)]",
          hideBottomPadding ? "pb-0" : "pb-20 md:pb-6"
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
