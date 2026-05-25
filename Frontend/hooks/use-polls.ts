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

export function usePolls(category?: string): UsePollsResult {
  const [polls, setPolls]   = useState<Poll[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]   = useState<string | null>(null)
  const [hasMore, setHasMore] = useState(true)

  // Initial load
  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(null)
    setPolls([])
    setHasMore(true)

    pollsApi.getTrending(PAGE_SIZE, category)
      .then((data) => {
        if (cancelled) return
        // US-19: filter out polls the user has already voted on
        const unvoted = data.filter((p) => !p.hasVoted)
        setPolls(unvoted.map(mapBackendPoll))
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
  }, [category])

  // Load more (fetch all and append new ones)
  const loadMore = useCallback(async () => {
    try {
      const data = await pollsApi.getAll(category)
      const existing = new Set(polls.map((p) => p.id))
      // US-19: exclude already-voted and already-loaded polls
      const fresh = data
        .filter((p) => !p.hasVoted && !existing.has(String(p.id)))
        .map(mapBackendPoll)
      setPolls((prev) => [...prev, ...fresh])
      setHasMore(fresh.length > 0)
    } catch (err) {
      setError((err as Error).message)
    }
  }, [category, polls])

  // Cast a vote — optimistically update percentages
  const castVote = useCallback(async (pollId: string, optionId: number) => {
    try {
      const result = await votesApi.cast({
        pollId:   Number(pollId),
        optionId,
      })
      const mapped = mapBackendPoll(result.poll)
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
