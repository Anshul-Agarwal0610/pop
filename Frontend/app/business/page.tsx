"use client"

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { BriefcaseBusiness, Loader2, Megaphone, Plus, RefreshCw } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { useAuth } from "@/contexts/auth-context"
import {
  businessApi,
  type ApiBusinessAccount,
  type ApiBusinessCampaign,
} from "@/lib/api"

export default function BusinessPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [accounts, setAccounts] = useState<ApiBusinessAccount[]>([])
  const [campaigns, setCampaigns] = useState<ApiBusinessCampaign[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [accountName, setAccountName] = useState("")
  const [websiteUrl, setWebsiteUrl] = useState("")
  const [campaignName, setCampaignName] = useState("")
  const [objective, setObjective] = useState("")
  const [pollQuestion, setPollQuestion] = useState("")
  const [pollOptions, setPollOptions] = useState("Yes\nNo")

  const selectedAccount = accounts[0]
  const selectedCampaign = campaigns[0]

  const loadBusiness = useCallback(async () => {
    if (!isAuthenticated) return

    setLoading(true)
    setError(null)
    try {
      const [loadedAccounts, loadedCampaigns] = await Promise.all([
        businessApi.getAccounts(),
        businessApi.getCampaigns(),
      ])
      setAccounts(loadedAccounts)
      setCampaigns(loadedCampaigns)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load business workspace")
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated])

  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) {
      router.push("/login?message=Sign in to manage business campaigns&redirect=/business")
      return
    }
    loadBusiness()
  }, [authLoading, isAuthenticated, loadBusiness, router])

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

  async function createAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    try {
      await businessApi.createAccount({
        name: accountName,
        websiteUrl: websiteUrl || undefined,
      })
      setAccountName("")
      setWebsiteUrl("")
      await loadBusiness()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create business account")
    } finally {
      setSaving(false)
    }
  }

  async function createCampaign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedAccount) return

    setSaving(true)
    try {
      await businessApi.createCampaign(selectedAccount.id, {
        name: campaignName,
        objective,
        status: "Active",
      })
      setCampaignName("")
      setObjective("")
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
        description: objective,
        category: "General",
        expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        options,
        sourceType: "business",
        isAIGenerated: false,
      })
      setPollQuestion("")
      setPollOptions("Yes\nNo")
      await loadBusiness()
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create sponsored poll")
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppShell>
      <main className="mx-auto w-full max-w-5xl px-4 py-6">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-2xl font-bold text-foreground">
              <BriefcaseBusiness className="h-6 w-6 text-primary" />
              Business Campaigns
            </h1>
            <p className="text-sm text-muted-foreground">
              Sponsored polls are labeled clearly and routed through campaign review.
            </p>
          </div>
          <Button className="gap-2" onClick={loadBusiness} variant="outline">
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
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
        ) : (
          <div className="grid gap-4 lg:grid-cols-[1fr_1.2fr]">
            <section className="space-y-4">
              <div className="rounded-lg border border-border/60 bg-card p-4">
                <h2 className="mb-3 font-semibold text-foreground">Account</h2>
                {selectedAccount ? (
                  <div className="space-y-1 text-sm">
                    <p className="font-medium text-foreground">{selectedAccount.name}</p>
                    <p className="text-muted-foreground">{selectedAccount.websiteUrl ?? "No website"}</p>
                  </div>
                ) : (
                  <form className="space-y-3" onSubmit={createAccount}>
                    <Input
                      onChange={(event) => setAccountName(event.target.value)}
                      placeholder="Business name"
                      required
                      value={accountName}
                    />
                    <Input
                      onChange={(event) => setWebsiteUrl(event.target.value)}
                      placeholder="Website URL"
                      value={websiteUrl}
                    />
                    <Button className="w-full gap-2" disabled={saving}>
                      <Plus className="h-4 w-4" />
                      Create account
                    </Button>
                  </form>
                )}
              </div>

              <div className="grid grid-cols-3 gap-2">
                <Metric label="Impressions" value={totals.impressions} />
                <Metric label="Votes" value={totals.votes} />
                <Metric label="Completions" value={totals.completions} />
              </div>

              <div className="rounded-lg border border-border/60 bg-card p-4">
                <h2 className="mb-3 font-semibold text-foreground">Campaigns</h2>
                <div className="space-y-2">
                  {campaigns.map((campaign) => (
                    <div className="rounded-md border border-border/50 p-3" key={campaign.id}>
                      <div className="flex items-center justify-between gap-3">
                        <p className="font-medium text-foreground">{campaign.name}</p>
                        <span className="rounded-full bg-primary/10 px-2 py-1 text-xs font-semibold text-primary">
                          {campaign.status}
                        </span>
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground">{campaign.objective}</p>
                    </div>
                  ))}
                  {campaigns.length === 0 && (
                    <p className="text-sm text-muted-foreground">No campaigns yet.</p>
                  )}
                </div>
              </div>
            </section>

            <section className="space-y-4">
              <form className="rounded-lg border border-border/60 bg-card p-4" onSubmit={createCampaign}>
                <h2 className="mb-3 font-semibold text-foreground">New campaign</h2>
                <div className="space-y-3">
                  <Input
                    disabled={!selectedAccount || saving}
                    onChange={(event) => setCampaignName(event.target.value)}
                    placeholder="Campaign name"
                    required
                    value={campaignName}
                  />
                  <Textarea
                    disabled={!selectedAccount || saving}
                    onChange={(event) => setObjective(event.target.value)}
                    placeholder="Objective"
                    required
                    value={objective}
                  />
                  <Button className="gap-2" disabled={!selectedAccount || saving}>
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
                  <Button className="gap-2" disabled={!selectedCampaign || saving}>
                    {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                    Submit for review
                  </Button>
                </div>
              </form>
            </section>
          </div>
        )}
      </main>
    </AppShell>
  )
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border border-border/60 bg-card p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-bold text-foreground">{value.toLocaleString()}</p>
    </div>
  )
}
