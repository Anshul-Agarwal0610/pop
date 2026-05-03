"use client"

import { useState, useEffect, useCallback } from "react"
import { pollsApi, votesApi } from "@/lib/api"
import { mapBackendPoll, type Poll } from "@/lib/poll-data"

interface UsePollsResult {
  polls:       Poll[]
  loading:     boolean
  error:       string | null
  castVote:    (pollId: string, optionId: number) => Promise<void>
  loadMore:    () => Promise<void>
  hasMore:     boolean
}

const PAGE_SIZE = 20

export function usePolls(): UsePollsResult {
  const [polls, setPolls]   = useState<Poll[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]   = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(true)

  // Initial load
  useEffect(() => {
    let cancelled = false

    pollsApi.getTrending(PAGE_SIZE)
      .then((data) => {
        if (cancelled) return
        setPolls(data.map(mapBackendPoll))
        setHasMore(data.length === PAGE_SIZE)
      })
      .catch((err: Error) => {
        if (cancelled) return
        setError(err.message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [])

  // Load more (fetch all and append new ones)
  const loadMore = useCallback(async () => {
    try {
      const data = await pollsApi.getAll()
      const mapped = data.map(mapBackendPoll)
      // Only add polls not already in state
      const existing = new Set(polls.map((p) => p.id))
      const fresh = mapped.filter((p) => !existing.has(p.id))
      setPolls((prev) => [...prev, ...fresh])
      setHasMore(fresh.length > 0)
    } catch (err) {
      setError((err as Error).message)
    }
  }, [polls])

  // Cast a vote — optimistically update percentages
  const castVote = useCallback(async (pollId: string, optionId: number) => {
    try {
      const updated = await votesApi.cast({
        pollId:   Number(pollId),
        optionId,
      })
      const mapped = mapBackendPoll(updated)
      setPolls((prev) =>
        prev.map((p) => (p.id === pollId ? mapped : p))
      )
    } catch (err) {
      // Non-fatal — vote still registers in the UI via VoteFeedback
      console.error("Vote failed:", err)
    }
  }, [])

  return { polls, loading, error, castVote, loadMore, hasMore }
}
