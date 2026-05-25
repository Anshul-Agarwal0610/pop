import type { Metadata } from "next"
import { AppShell } from "@/components/app-shell"
import { PollDetailCard } from "@/components/poll-detail/poll-detail-card"
import { API_BASE_URL } from "@/lib/config"
import type { ApiPoll } from "@/lib/api"

interface PollDetailPageProps {
  params: Promise<{ id: string }>
}

async function fetchPollForMetadata(id: string): Promise<ApiPoll | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/polls/${id}`, {
      cache: "no-store",
    })
    if (!res.ok) return null
    return (await res.json()) as ApiPoll
  } catch {
    return null
  }
}

export async function generateMetadata({
  params,
}: PollDetailPageProps): Promise<Metadata> {
  const { id } = await params
  const poll = await fetchPollForMetadata(id)

  if (!poll) {
    return {
      title: "Poll not found | Pollify",
      description: "This Pollify poll could not be found.",
    }
  }

  const description = `${poll.totalVotes} votes - ${poll.category}`

  return {
    title: `${poll.question} | Pollify`,
    description,
    openGraph: {
      title: poll.question,
      description,
      images: poll.thumbnailUrl ? [{ url: poll.thumbnailUrl }] : undefined,
      type: "article",
    },
    twitter: {
      card: poll.thumbnailUrl ? "summary_large_image" : "summary",
      title: poll.question,
      description,
      images: poll.thumbnailUrl ? [poll.thumbnailUrl] : undefined,
    },
  }
}

export default async function PollDetailPage({ params }: PollDetailPageProps) {
  const { id } = await params

  return (
    <AppShell>
      <PollDetailCard pollId={id} />
    </AppShell>
  )
}
