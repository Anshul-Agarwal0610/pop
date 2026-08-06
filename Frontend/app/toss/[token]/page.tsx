import { PollTossIncoming } from "@/components/poll-toss/poll-toss-incoming"
export default async function TossPage({ params }: { params: Promise<{ token: string }> }) {
  const { token } = await params
  return <main className="flex min-h-[calc(100dvh-5rem)] items-center justify-center px-4 py-10"><PollTossIncoming token={token} /></main>
}
