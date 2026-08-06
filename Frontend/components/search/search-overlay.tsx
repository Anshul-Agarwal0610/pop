"use client"

import { useEffect, useMemo, useState } from "react"
import { Loader2, Search } from "lucide-react"
import { Input } from "@/components/ui/input"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { SearchResultCard } from "@/components/search/search-result-card"
import { pollsApi, type ApiPoll } from "@/lib/api"

interface SearchOverlayProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function SearchOverlay({ open, onOpenChange }: SearchOverlayProps) {
  const [query, setQuery] = useState("")
  const [results, setResults] = useState<ApiPoll[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const trimmedQuery = useMemo(() => query.trim(), [query])

  useEffect(() => {
    if (!open) {
      setQuery("")
      setResults([])
      setError(null)
    }
  }, [open])

  useEffect(() => {
    if (!open || !trimmedQuery) {
      setResults([])
      setLoading(false)
      setError(null)
      return
    }

    let cancelled = false
    const timeout = window.setTimeout(() => {
      setLoading(true)
      setError(null)

      pollsApi.search(trimmedQuery)
        .then((data) => {
          if (!cancelled) setResults(data)
        })
        .catch((err: Error) => {
          if (!cancelled) setError(err.message)
        })
        .finally(() => {
          if (!cancelled) setLoading(false)
        })
    }, 400)

    return () => {
      cancelled = true
      window.clearTimeout(timeout)
    }
  }, [open, trimmedQuery])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[calc(100dvh-1rem)] w-[calc(100vw-1rem)] gap-0 overflow-hidden p-0 sm:max-w-2xl">
        <DialogHeader className="sr-only">
          <DialogTitle>Search polls</DialogTitle>
          <DialogDescription>Search active polls by keyword.</DialogDescription>
        </DialogHeader>

        <div className="border-b border-border p-4 pr-12">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              autoFocus
              className="h-11 rounded-xl pl-9 text-base"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search polls"
              value={query}
            />
          </div>
        </div>

        <div className="min-h-0 max-h-[calc(100dvh-7rem)] overflow-y-auto p-3">
          {!trimmedQuery && (
            <div className="py-12 text-center text-sm text-muted-foreground">
              Search by keyword to find active polls.
            </div>
          )}

          {trimmedQuery && loading && (
            <div className="flex items-center justify-center gap-2 py-12 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Searching...
            </div>
          )}

          {trimmedQuery && !loading && error && (
            <div className="py-12 text-center text-sm text-destructive">
              Could not search polls.
            </div>
          )}

          {trimmedQuery && !loading && !error && results.length === 0 && (
            <div className="py-12 text-center text-sm text-muted-foreground">
              No polls found for {trimmedQuery}
            </div>
          )}

          {trimmedQuery && !loading && !error && results.length > 0 && (
            <div className="space-y-1">
              {results.map((poll) => (
                <SearchResultCard
                  key={poll.id}
                  poll={poll}
                  onSelect={() => onOpenChange(false)}
                />
              ))}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
