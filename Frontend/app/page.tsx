"use client"

import { AppShell } from "@/components/app-shell"
import { PollFeed } from "@/components/poll-feed"

export default function HomePage() {
  return (
    <AppShell hideBottomPadding>
      <div className="h-[calc(100vh-4rem-4.5rem)] md:h-[calc(100vh-4rem)]">
        <PollFeed />
      </div>
    </AppShell>
  )
}
