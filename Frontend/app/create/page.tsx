import { Suspense } from "react"
import { PollCreator } from "@/components/poll-create"

export default function CreatePollPage() {
  return <Suspense fallback={<div className="p-6 text-muted-foreground">Loading poll creator…</div>}><PollCreator /></Suspense>
}
