"use client"

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react"
import {
  BarChart3,
  BriefcaseBusiness,
  Download,
  Loader2,
  Megaphone,
  Plus,
  RefreshCw,
  LogOut,
  Palette,
  CreditCard,
} from "lucide-react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Progress } from "@/components/ui/progress"
import { Textarea } from "@/components/ui/textarea"
import { useAuth } from "@/contexts/auth-context"
import {
  businessApi,
  type ApiBusinessAccount,
  type ApiBusinessCampaign,
  type ApiCampaignAnalytics,
  type ApiCampaignOptionBreakdown,
} from "@/lib/api"
import { cn } from "@/lib/utils"

export default function BusinessPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading, user, logout } = useAuth()
  const [accounts, setAccounts] = useState<ApiBusinessAccount[]>([])
  const [campaigns, setCampaigns] = useState<ApiBusinessCampaign[]>([])
  const [selectedCampaignId, setSelectedCampaignId] = useState<number | null>(null)
  const [analytics, setAnalytics] = useState<ApiCampaignAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [analyticsLoading, setAnalyticsLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [campaignName, setCampaignName] = useState("")
  const [objective, setObjective] = useState("")
  const [pollQuestion, setPollQuestion] = useState("")
  const [pollOptions, setPollOptions] = useState("Yes\nNo")

  const selectedAccount = accounts[0]
  const selectedCampaign = campaigns.find((campaign) => campaign.id === selectedCampaignId) ?? campaigns[0]

  const loadBusiness = useCallback(async () => {
    if (!isAuthenticated || user?.accountType !== "Business") return

    setLoading(true)
    setError(null)
    try {
      const [loadedAccounts, loadedCampaigns] = await Promise.all([
        businessApi.getAccounts(),
        businessApi.getCampaigns(),
      ])
      setAccounts(loadedAccounts)
      setCampaigns(loadedCampaigns)
      setSelectedCampaignId((current) =>
        current && loadedCampaigns.some((campaign) => campaign.id === current)
          ? current
          : loadedCampaigns[0]?.id ?? null
      )
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load business workspace")
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated, user?.accountType])

  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) router.replace("/business/login")
    else if (user?.accountType !== "Business") router.replace("/")
  }, [authLoading, isAuthenticated, router, user?.accountType])

  const loadAnalytics = useCallback(async (campaignId: number | null) => {
    if (!campaignId) {
      setAnalytics(null)
      return
    }

    setAnalyticsLoading(true)
    setError(null)
    try {
      setAnalytics(await businessApi.getCampaignAnalytics(campaignId))
    } catch (err) {
      setAnalytics(null)
      setError(err instanceof Error ? err.message : "Could not load campaign analytics")
    } finally {
      setAnalyticsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isAuthenticated && user?.accountType === "Business") loadBusiness()
  }, [isAuthenticated, loadBusiness, user?.accountType])

  useEffect(() => {
    loadAnalytics(selectedCampaignId)
  }, [loadAnalytics, selectedCampaignId])

  const totals = useMemo(() => {
    return campaigns.reduce(
      (acc, campaign) => ({
        impressions: acc.impressions + campaign.impressions,
        votes: acc.votes + campaign.votes,
        completions: acc.completions + campaign.completions,
      }),
      { impressions: 0, votes: 0, completions: 0 }
    )
  }, [campaigns])

  async function createCampaign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedAccount) return

    setSaving(true)
    try {
      const created = await businessApi.createCampaign(selectedAccount.id, {
        name: campaignName,
        objective,
        status: "Active",
      })
      setCampaignName("")
      setObjective("")
      setSelectedCampaignId(created.id)
      await loadBusiness()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create campaign")
    } finally {
      setSaving(false)
    }
  }

  async function createSponsoredPoll(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedCampaign) return

    const options = pollOptions
      .split("\n")
      .map((option) => option.trim())
      .filter(Boolean)

    setSaving(true)
    try {
      await businessApi.createSponsoredPoll(selectedCampaign.id, {
        question: pollQuestion,
        description: selectedCampaign.objective,
        category: "General",
        expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        options,
        sourceType: "business",
        isAIGenerated: false,
      })
      setPollQuestion("")
      setPollOptions("Yes\nNo")
      await Promise.all([loadBusiness(), loadAnalytics(selectedCampaign.id)])
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create sponsored poll")
    } finally {
      setSaving(false)
    }
  }

  async function exportCsv() {
    if (!selectedCampaign) return

    setExporting(true)
    try {
      const blob = await businessApi.exportCampaignCsv(selectedCampaign.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement("a")
      link.href = url
      link.download = `campaign-${selectedCampaign.id}-analytics.csv`
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not export campaign analytics")
    } finally {
      setExporting(false)
    }
  }

  if (authLoading || !isAuthenticated || user?.accountType !== "Business") return <div className="flex min-h-screen items-center justify-center bg-background"><Loader2 className="h-7 w-7 animate-spin text-primary" /></div>

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-20 border-b border-border bg-card/95 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4">
          <div><p className="flex items-center gap-2 text-lg font-extrabold"><BriefcaseBusiness className="h-5 w-5 text-primary" />Pop Business</p><p className="text-xs text-muted-foreground">{accounts[0]?.name ?? user.displayName}</p></div>
          <Button variant="outline" onClick={() => { logout(); router.replace("/business/login") }}><LogOut className="mr-2 h-4 w-4" />Sign out</Button>
        </div>
      </header>
      <main className="mx-auto w-full max-w-6xl px-4 py-6">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-foreground">
              <BriefcaseBusiness className="h-6 w-6 text-primary" />
              Business Campaigns
            </h1>
            <p className="text-sm text-muted-foreground">
              Create sponsored polls, review performance, and export campaign results.
            </p>
          </div>
          <div className="flex w-full flex-wrap gap-2 sm:w-auto">
            <Button className="min-w-0 flex-1 gap-2 sm:flex-none" disabled={!selectedCampaign || exporting} onClick={exportCsv} variant="outline">
              {exporting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
              Export CSV
            </Button>
            <Button className="min-w-0 flex-1 gap-2 sm:flex-none" onClick={loadBusiness} variant="outline">
              <RefreshCw className="h-4 w-4" />
              Refresh
            </Button>
          </div>
        </div>

        {error && (
          <div className="mb-4 rounded-lg border border-destructive/20 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center gap-3 py-20 text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading business workspace...
          </div>
        ) : accounts.length === 0 ? (
          <div className="mx-auto max-w-md rounded-2xl border border-destructive/30 bg-card p-6 text-center"><h2 className="font-semibold">Business workspace unavailable</h2><p className="mt-2 text-sm text-muted-foreground">This business identity has no workspace attached. Contact support to restore it.</p></div>
        ) : (
          <div className="grid min-w-0 gap-4 xl:grid-cols-[280px_minmax(0,1fr)]">
            <aside className="min-w-0 space-y-4">
              <div className="rounded-lg border border-border/60 bg-card p-4">
                <p className="text-xs text-muted-foreground">Account</p>
                <p className="mt-1 font-semibold text-foreground">{selectedAccount?.name}</p>
                <p className="text-sm text-muted-foreground">{selectedAccount?.websiteUrl ?? "No website"}</p>
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-4">
                <p className="flex items-center gap-2 text-sm font-semibold"><CreditCard className="h-4 w-4 text-primary" />Subscription</p>
                <p className="mt-2 text-lg font-bold">{selectedAccount.subscriptionPlan}</p>
                <p className="text-sm text-muted-foreground">{selectedAccount.subscriptionStatus}{selectedAccount.trialEndsAt ? ` · trial ends ${new Date(selectedAccount.trialEndsAt).toLocaleDateString()}` : ""}</p>
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-4">
                <p className="flex items-center gap-2 text-sm font-semibold"><Palette className="h-4 w-4 text-primary" />Brand studio</p>
                <p className="mt-2 text-sm text-muted-foreground">Logo, colours and sponsored disclosure controls will be added here next.</p>
                <span className="mt-3 inline-flex rounded-full bg-primary/10 px-2 py-1 text-xs font-semibold text-primary">Coming next</span>
              </div>

              <div className="grid grid-cols-1 gap-2 min-[360px]:grid-cols-3 xl:grid-cols-1">
                <Metric label="Impressions" value={totals.impressions} />
                <Metric label="Votes" value={totals.votes} />
                <Metric label="Completions" value={totals.completions} />
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-3">
                <h2 className="mb-3 font-semibold text-foreground">Campaigns</h2>
                <div className="space-y-2">
                  {campaigns.map((campaign) => (
                    <button
                      className={cn(
                        "w-full rounded-md border border-border/50 p-3 text-left transition hover:border-primary/40",
                        selectedCampaign?.id === campaign.id && "border-primary/60 bg-primary/5"
                      )}
                      key={campaign.id}
                      onClick={() => setSelectedCampaignId(campaign.id)}
                      type="button"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <p className="font-medium text-foreground">{campaign.name}</p>
                        <span className="rounded-full bg-primary/10 px-2 py-1 text-xs font-semibold text-primary">
                          {campaign.status}
                        </span>
                      </div>
                      <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{campaign.objective}</p>
                    </button>
                  ))}
                  {campaigns.length === 0 && (
                    <p className="text-sm text-muted-foreground">No campaigns yet.</p>
                  )}
                </div>
              </div>
            </aside>

            <section className="min-w-0 space-y-4">
              <div className="grid grid-cols-1 gap-3 min-[360px]:grid-cols-2 md:grid-cols-4">
                <Metric label="Campaign impressions" value={analytics?.campaign.impressions ?? 0} />
                <Metric label="Campaign votes" value={analytics?.campaign.votes ?? 0} />
                <Metric label="Completions" value={analytics?.campaign.completions ?? 0} />
                <Metric label="Completion rate" value={analytics?.campaign.completionRate ?? 0} suffix="%" />
              </div>

              <div className="grid min-w-0 gap-4 lg:grid-cols-[minmax(0,1fr)_340px]">
                <div className="rounded-lg border border-border/60 bg-card p-4">
                  <div className="mb-3 flex items-center justify-between gap-3">
                    <h2 className="flex items-center gap-2 font-semibold text-foreground">
                      <BarChart3 className="h-4 w-4 text-primary" />
                      Sponsored polls
                    </h2>
                    {analyticsLoading && <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />}
                  </div>
                  {analytics?.polls.length ? (
                    <div className="space-y-3">
                      {analytics.polls.map((poll) => (
                        <PollAnalyticsRow
                          key={poll.pollId}
                          options={analytics.optionBreakdown.filter((option) => option.pollId === poll.pollId)}
                          poll={poll}
                        />
                      ))}
                    </div>
                  ) : (
                    <EmptyState text="No sponsored polls have been submitted for this campaign." />
                  )}
                </div>

                <div className="space-y-4">
                  <form className="rounded-lg border border-border/60 bg-card p-4" onSubmit={createCampaign}>
                    <h2 className="mb-3 font-semibold text-foreground">New campaign</h2>
                    <div className="space-y-3">
                      <Input
                        disabled={saving}
                        onChange={(event) => setCampaignName(event.target.value)}
                        placeholder="Campaign name"
                        required
                        value={campaignName}
                      />
                      <Textarea
                        disabled={saving}
                        onChange={(event) => setObjective(event.target.value)}
                        placeholder="Objective"
                        required
                        value={objective}
                      />
                      <Button className="w-full gap-2 sm:w-auto" disabled={saving}>
                        <Plus className="h-4 w-4" />
                        Create campaign
                      </Button>
                    </div>
                  </form>

                  <form className="rounded-lg border border-border/60 bg-card p-4" onSubmit={createSponsoredPoll}>
                    <h2 className="mb-3 flex items-center gap-2 font-semibold text-foreground">
                      <Megaphone className="h-4 w-4 text-amber-500" />
                      Sponsored poll
                    </h2>
                    <div className="space-y-3">
                      <Input
                        disabled={!selectedCampaign || saving}
                        onChange={(event) => setPollQuestion(event.target.value)}
                        placeholder="Question"
                        required
                        value={pollQuestion}
                      />
                      <Textarea
                        disabled={!selectedCampaign || saving}
                        onChange={(event) => setPollOptions(event.target.value)}
                        placeholder="One option per line"
                        required
                        value={pollOptions}
                      />
                      <Button className="w-full gap-2 sm:w-auto" disabled={!selectedCampaign || saving}>
                        {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                        Submit for review
                      </Button>
                    </div>
                  </form>

                  <TrendPanel dailyVotes={analytics?.dailyVotes ?? []} />
                </div>
              </div>
            </section>
          </div>
        )}
      </main>
    </div>
  )
}

function Metric({ label, value, suffix = "" }: { label: string; value: number; suffix?: string }) {
  return (
    <div className="rounded-lg border border-border/60 bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-bold text-foreground">
        {value.toLocaleString(undefined, { maximumFractionDigits: suffix ? 1 : 0 })}
        {suffix}
      </p>
    </div>
  )
}

function PollAnalyticsRow({
  options,
  poll,
}: {
  options: ApiCampaignOptionBreakdown[]
  poll: ApiCampaignAnalytics["polls"][number]
}) {
  return (
    <div className="rounded-md border border-border/50 p-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-medium text-foreground">{poll.question}</p>
          <p className="mt-1 text-xs text-muted-foreground">
            {poll.moderationStatus} - {new Date(poll.createdAt).toLocaleDateString()}
          </p>
        </div>
        <div className="grid w-full grid-cols-2 gap-2 text-left text-xs sm:w-auto sm:grid-cols-4 sm:text-right">
          <MiniMetric label="Imp." value={poll.impressions} />
          <MiniMetric label="Votes" value={poll.votes} />
          <MiniMetric label="Done" value={poll.completions} />
          <MiniMetric label="Rate" value={`${poll.completionRate.toFixed(1)}%`} />
        </div>
      </div>
      <div className="mt-3 space-y-2">
        {options.map((option) => (
          <div key={option.optionId}>
            <div className="mb-1 flex items-center justify-between gap-3 text-xs">
              <span className="min-w-0 break-words text-muted-foreground">{option.optionText}</span>
              <span className="shrink-0 text-right font-medium text-foreground">
                {option.voteCount} votes - {Math.round(option.votePercentage)}%
              </span>
            </div>
            <Progress value={option.votePercentage} />
          </div>
        ))}
      </div>
    </div>
  )
}

function MiniMetric({ label, value }: { label: string; value: number | string }) {
  return (
    <div>
      <p className="text-muted-foreground">{label}</p>
      <p className="font-semibold text-foreground">{typeof value === "number" ? value.toLocaleString() : value}</p>
    </div>
  )
}

function TrendPanel({ dailyVotes }: { dailyVotes: ApiCampaignAnalytics["dailyVotes"] }) {
  const maxVotes = Math.max(1, ...dailyVotes.map((day) => day.votes))

  return (
    <div className="rounded-lg border border-border/60 bg-card p-4">
      <h2 className="mb-3 font-semibold text-foreground">Vote trend</h2>
      {dailyVotes.length === 0 ? (
        <EmptyState text="Trend data appears after sponsored polls receive votes." />
      ) : (
        <div className="space-y-2">
          {dailyVotes.map((day) => (
            <div className="grid grid-cols-[80px_1fr_40px] items-center gap-2 text-xs" key={day.date}>
              <span className="text-muted-foreground">{new Date(day.date).toLocaleDateString()}</span>
              <div className="h-2 overflow-hidden rounded-full bg-secondary">
                <div
                  className="h-full rounded-full bg-primary"
                  style={{ width: `${Math.max(4, (day.votes / maxVotes) * 100)}%` }}
                />
              </div>
              <span className="text-right font-medium text-foreground">{day.votes}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function EmptyState({ text }: { text: string }) {
  return (
    <div className="rounded-md border border-dashed border-border/70 px-4 py-8 text-center text-sm text-muted-foreground">
      {text}
    </div>
  )
}
