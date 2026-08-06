"use client"

import { AppShell } from "@/components/app-shell"
import { PollFeed } from "@/components/poll-feed"

export default function PollsPage() {
  return (
    <AppShell hideBottomPadding>
      <div className="h-[calc(100dvh-4rem-4.5rem-env(safe-area-inset-bottom))] min-h-[28rem] lg:h-[calc(100dvh-4rem)]">
        <PollFeed />
      </div>
    </AppShell>
  )
}
