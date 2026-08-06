"use client"

import { Suspense } from "react"
import { AppShell } from "@/components/app-shell"
import { PollFeed } from "@/components/poll-feed"
import { useSearchParams } from "next/navigation"

function PollsPageContent() {
  const category = useSearchParams().get("category")

  return (
    <AppShell hideBottomPadding>
      <div className="h-[calc(100dvh-4rem-4.5rem-env(safe-area-inset-bottom))] min-h-[28rem] lg:h-[calc(100dvh-4rem)]">
        <PollFeed initialCategory={category} />
      </div>
    </AppShell>
  )
}

export default function PollsPage() {
  return (
    <Suspense fallback={null}>
      <PollsPageContent />
    </Suspense>
  )
}
