"use client"

import { useState, useEffect, useCallback } from "react"
import { pollsApi, votesApi, type ApiCastVoteResponse } from "@/lib/api"
import { mapBackendPoll, type Poll } from "@/lib/poll-data"

interface UsePollsResult {
  polls: Poll[]
  loading: boolean
  error: string | null
  castVote: (pollId: string, optionId: number) => Promise<ApiCastVoteResponse>
  loadMore: () => Promise<void>
  hasMore: boolean
}

const PAGE_SIZE = 20

export function usePolls(category?: string): UsePollsResult {
  const [polls, setPolls] = useState<Poll[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    setPolls([])
    setHasMore(true)
    pollsApi.getPersonalized(PAGE_SIZE, category)
      .then((data) => {
        if (cancelled) return
        setPolls(data.filter((poll) => !poll.hasVoted).map(mapBackendPoll))
        setHasMore(data.length === PAGE_SIZE)
      })
      .catch((err: Error) => { if (!cancelled) setError(err.message) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [category])

  const loadMore = useCallback(async () => {
    try {
      const data = await pollsApi.getAll(category)
      const existing = new Set(polls.map((poll) => poll.id))
      const fresh = data
        .filter((poll) => !poll.hasVoted && !existing.has(String(poll.id)))
        .map(mapBackendPoll)
      setPolls((previous) => [...previous, ...fresh])
      setHasMore(fresh.length > 0)
    } catch (err) {
      setError((err as Error).message)
    }
  }, [category, polls])

  const castVote = useCallback(async (pollId: string, optionId: number) => {
    const result = await votesApi.cast({ pollId: Number(pollId), optionId })
    const mapped = mapBackendPoll(result.poll)
    setPolls((previous) => previous.map((poll) => poll.id === pollId ? mapped : poll))
    return result
  }, [])

  return { polls, loading, error, castVote, loadMore, hasMore }
}
