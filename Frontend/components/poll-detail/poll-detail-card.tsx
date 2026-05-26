"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle2,
  Clock,
  Flag,
  ImageOff,
  Loader2,
  Megaphone,
  Newspaper,
  Sparkles,
  Users,
  Zap,
} from "lucide-react"
import { motion } from "framer-motion"
import { CategoryBadge } from "@/components/category-badge"
import { ShareButton } from "@/components/share-button"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { pollsApi, votesApi, type ApiPoll, type ApiPollOption } from "@/lib/api"
import { SOURCE_COLORS, SOURCE_LABELS, type IngestionSource } from "@/lib/poll-data"
import { pollShareText, resultShareText } from "@/lib/share"
import { cn } from "@/lib/utils"

interface PollDetailCardProps {
  pollId: string
}

function relativeTime(iso: string) {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.max(0, Math.floor(diff / 60_000))
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

function isNotFound(error: string | null) {
  return Boolean(error?.startsWith("API 404"))
}

function sourceLabel(sourceType: string | null) {
  const source = (sourceType ?? "manual") as IngestionSource
  return SOURCE_LABELS[source] ?? "Pollify"
}

function sourceStyle(sourceType: string | null) {
  const source = (sourceType ?? "manual") as IngestionSource
  return SOURCE_COLORS[source] ?? SOURCE_COLORS.manual
}

function ResultBar({
  isSelected,
  option,
}: {
  isSelected: boolean
  option: ApiPollOption
}) {
  const percentage = Math.round(option.votePercentage ?? 0)

  return (
    <div className="space-y-2 rounded-xl bg-secondary/40 p-3">
      <div className="flex items-center justify-between gap-3 text-sm font-semibold">
        <span
          className={cn(
            "flex min-w-0 items-center gap-2",
            isSelected ? "text-primary" : "text-foreground"
          )}
        >
          {isSelected && <CheckCircle2 className="h-4 w-4 flex-shrink-0" />}
          <span className="truncate">{option.text}</span>
        </span>
        <span className="text-muted-foreground">{percentage}%</span>
      </div>
      <div className="h-2.5 overflow-hidden rounded-full bg-background">
        <motion.div
          className={cn(
            "h-full rounded-full",
            isSelected ? "bg-primary" : "bg-muted-foreground/35"
          )}
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ duration: 0.45, ease: "easeOut" }}
        />
      </div>
    </div>
  )
}

export function PollDetailCard({ pollId }: PollDetailCardProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [poll, setPoll] = useState<ApiPoll | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [voteError, setVoteError] = useState<string | null>(null)
  const [reportMessage, setReportMessage] = useState<string | null>(null)
  const [authNotice, setAuthNotice] = useState<string | null>(null)
  const [votingOptionId, setVotingOptionId] = useState<number | null>(null)
  const [isReporting, setIsReporting] = useState(false)
  const [xpAwarded, setXpAwarded] = useState<number | null>(null)

  const loadPoll = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setPoll(await pollsApi.getById(pollId))
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load poll")
    } finally {
      setLoading(false)
    }
  }, [pollId])

  useEffect(() => {
    loadPoll()
  }, [loadPoll])

  useEffect(() => {
    if (!poll?.isSponsored) return
    pollsApi.recordImpression(poll.id).catch(() => undefined)
  }, [poll?.id, poll?.isSponsored])

  const expired = useMemo(() => {
    if (!poll) return false
    return !poll.isActive || new Date(poll.expiresAt).getTime() < Date.now()
  }, [poll])

  async function handleVote(optionId: number) {
    if (authLoading || !poll) return

    if (!isAuthenticated) {
      setAuthNotice("Sign in to vote")
      router.push(
        `/login?message=${encodeURIComponent("Sign in to vote")}&redirect=${encodeURIComponent(`/polls/${pollId}`)}`
      )
      return
    }

    setVoteError(null)
    setVotingOptionId(optionId)
    try {
      const response = await votesApi.cast({
        pollId: Number(pollId),
        optionId,
      })
      setPoll(response.poll)
      setXpAwarded(response.reward.xpAwarded)
    } catch (err) {
      setVoteError(err instanceof Error ? err.message : "Could not record your vote")
    } finally {
      setVotingOptionId(null)
    }
  }

  async function handleReport() {
    if (authLoading || !poll || isReporting) return

    if (!isAuthenticated) {
      setAuthNotice("Sign in to report polls")
      router.push(
        `/login?message=${encodeURIComponent("Sign in to report polls")}&redirect=${encodeURIComponent(`/polls/${pollId}`)}`
      )
      return
    }

    setIsReporting(true)
    setReportMessage(null)
    try {
      const response = await pollsApi.report(
        pollId,
        "User reported this poll from the poll detail page."
      )
      setReportMessage(response.message)
    } catch (err) {
      setReportMessage(err instanceof Error ? err.message : "Could not report this poll")
    } finally {
      setIsReporting(false)
    }
  }

  if (loading) {
    return (
      <div className="mx-auto flex min-h-[calc(100vh-6rem)] w-full max-w-3xl flex-col gap-4 px-4 py-6">
        <div className="h-10 w-24 animate-pulse rounded-lg bg-secondary" />
        <div className="overflow-hidden rounded-2xl bg-card shadow-sm ring-1 ring-border/50">
          <div className="h-72 animate-pulse bg-secondary" />
          <div className="space-y-4 p-5">
            <div className="h-6 w-24 animate-pulse rounded bg-secondary" />
            <div className="h-9 w-full animate-pulse rounded bg-secondary" />
            <div className="h-9 w-4/5 animate-pulse rounded bg-secondary" />
            <div className="h-24 animate-pulse rounded-xl bg-secondary" />
          </div>
        </div>
      </div>
    )
  }

  if (isNotFound(error)) {
    return (
      <div className="mx-auto flex min-h-[calc(100vh-6rem)] w-full max-w-xl flex-col items-center justify-center gap-4 px-4 text-center">
        <AlertCircle className="h-12 w-12 text-muted-foreground" />
        <h1 className="text-2xl font-bold text-foreground">Poll not found</h1>
        <p className="text-muted-foreground">
          This poll may have been removed or the link is incorrect.
        </p>
        <Button onClick={() => router.push("/polls")}>Back to polls</Button>
      </div>
    )
  }

  if (error || !poll) {
    return (
      <div className="mx-auto flex min-h-[calc(100vh-6rem)] w-full max-w-xl flex-col items-center justify-center gap-4 px-4 text-center">
        <AlertCircle className="h-12 w-12 text-destructive" />
        <h1 className="text-2xl font-bold text-foreground">Could not load poll</h1>
        <p className="text-muted-foreground">{error ?? "Unknown error"}</p>
        <Button variant="outline" onClick={loadPoll}>
          Try again
        </Button>
      </div>
    )
  }

  const style = sourceStyle(poll.sourceType)
  const resultView = searchParams.get("view") === "results"
  const showResults = poll.hasVoted || expired || resultView
  const selectedOption = poll.options.find((option) => option.id === poll.userVotedOptionId)
  const leadingOption = [...poll.options].sort((a, b) => (b.votePercentage ?? 0) - (a.votePercentage ?? 0))[0]
  const resultOption = selectedOption ?? leadingOption

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6">
      <Button
        variant="ghost"
        className="mb-4 gap-2"
        onClick={() => router.back()}
      >
        <ArrowLeft className="h-4 w-4" />
        Back
      </Button>

      <article className="overflow-hidden rounded-2xl bg-card shadow-sm ring-1 ring-border/50">
        <div className="relative h-72 overflow-hidden bg-muted md:h-96">
          {poll.thumbnailUrl ? (
            <img
              alt=""
              className="h-full w-full object-cover"
              src={poll.thumbnailUrl}
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-primary/20 via-accent/10 to-primary/5">
              <div className="flex flex-col items-center gap-2 text-muted-foreground">
                <ImageOff className="h-10 w-10 opacity-40" />
                <span className="text-xs opacity-60">No preview available</span>
              </div>
            </div>
          )}

          <div className="absolute inset-0 bg-gradient-to-t from-card via-card/20 to-transparent" />

          <div className="absolute left-4 right-4 top-4 flex items-start justify-between gap-3">
            <div className="flex flex-wrap gap-2">
              {poll.isTrending && (
                <span className="rounded-full bg-primary px-3 py-1.5 text-xs font-semibold text-primary-foreground">
                  Trending
                </span>
              )}
              {poll.isAIGenerated && (
                <span className="flex items-center gap-1.5 rounded-full bg-violet-500/15 px-3 py-1.5 text-xs font-medium text-violet-400 ring-1 ring-violet-500/30">
                  <Sparkles className="h-3 w-3" />
                  AI Poll
                </span>
              )}
              {poll.isSponsored && (
                <span className="flex items-center gap-1.5 rounded-full bg-amber-500 px-3 py-1.5 text-xs font-bold text-amber-950">
                  <Megaphone className="h-3.5 w-3.5" />
                  Sponsored{poll.sponsorName ? ` by ${poll.sponsorName}` : ""}
                </span>
              )}
            </div>

            <ShareButton
              category={poll.category}
              className="bg-background/85 shadow-lg backdrop-blur-sm hover:bg-background"
              pollId={poll.id}
              text={pollShareText(poll)}
              title={poll.question}
            />
          </div>

          <div
            className={cn(
              "absolute bottom-4 left-4 flex items-center gap-2 rounded-full px-3 py-1.5 shadow-lg backdrop-blur-sm",
              style.bg
            )}
          >
            <Newspaper className={cn("h-4 w-4", style.icon)} />
            <span className={cn("text-xs font-medium", style.text)}>
              {sourceLabel(poll.sourceType)}
            </span>
          </div>
        </div>

        <div className="space-y-5 p-5 md:p-7">
          <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
            <CategoryBadge category={poll.category} />
            <span className="flex items-center gap-1.5">
              <Clock className="h-4 w-4" />
              {relativeTime(poll.createdAt)}
            </span>
            <span className="flex items-center gap-1.5">
              <Users className="h-4 w-4" />
              {poll.totalVotes.toLocaleString()} votes
            </span>
          </div>

          <div className="space-y-3">
            <h1 className="text-3xl font-black leading-tight text-foreground md:text-4xl">
              {poll.question}
            </h1>
            {poll.description && (
              <p className="text-base leading-7 text-muted-foreground">
                {poll.description}
              </p>
            )}
          </div>

          {xpAwarded != null && (
            <div className="flex items-center gap-2 rounded-xl bg-amber-500/15 px-4 py-3 text-sm font-semibold text-amber-600 dark:text-amber-400">
              <Zap className="h-4 w-4 fill-current" />
              +{xpAwarded} XP earned
            </div>
          )}

          {authNotice && (
            <div className="rounded-xl bg-primary/10 px-4 py-3 text-sm font-medium text-primary">
              {authNotice}
            </div>
          )}

          {voteError && (
            <div className="rounded-xl bg-destructive/10 px-4 py-3 text-sm font-medium text-destructive">
              {voteError}
            </div>
          )}

          {reportMessage && (
            <div className="rounded-xl bg-secondary/70 px-4 py-3 text-sm font-medium text-muted-foreground">
              {reportMessage}
            </div>
          )}

          {showResults ? (
            <div className="space-y-3">
              {poll.options.map((option) => (
                <ResultBar
                  isSelected={poll.userVotedOptionId === option.id}
                  key={option.id}
                  option={option}
                />
              ))}
              <p className="text-center text-sm text-muted-foreground">
                {poll.hasVoted ? "You already voted" : expired ? "This poll has ended" : "Results preview"}
              </p>
              <div className="flex justify-center">
                <ShareButton
                  category={poll.category}
                  path={`/polls/${poll.id}?view=results`}
                  pollId={poll.id}
                  text={resultShareText(poll, resultOption)}
                  title={`Poll result: ${poll.question}`}
                  variant="outline"
                />
              </div>
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2">
              {poll.options.slice(0, 2).map((option, index) => (
                <Button
                  className={cn(
                    "h-14 rounded-2xl text-base font-black",
                    index === 0
                      ? "bg-emerald-500 text-white hover:bg-emerald-600"
                      : "bg-red-500 text-white hover:bg-red-600"
                  )}
                  disabled={votingOptionId != null}
                  key={option.id}
                  onClick={() => handleVote(option.id)}
                >
                  {votingOptionId === option.id ? (
                    <Loader2 className="h-5 w-5 animate-spin" />
                  ) : (
                    option.text
                  )}
                </Button>
              ))}
            </div>
          )}

          <div className="flex justify-end">
            <Button
              className="gap-2 text-muted-foreground"
              disabled={isReporting}
              onClick={handleReport}
              size="sm"
              type="button"
              variant="ghost"
            >
              {isReporting ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Flag className="h-4 w-4" />
              )}
              Report
            </Button>
          </div>
        </div>
      </article>
    </div>
  )
}
