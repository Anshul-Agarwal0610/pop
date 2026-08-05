"use client"

import { useCallback, useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Bell, CheckCheck, Loader2, RefreshCw, Sparkles, TrendingUp, Trophy } from "lucide-react"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { useAuth } from "@/contexts/auth-context"
import { notificationsApi, type ApiNotification, type ApiNotificationPreference } from "@/lib/api"
import { cn } from "@/lib/utils"

function relativeTime(iso: string) {
  const diff = Date.now() - new Date(iso).getTime()
  const mins = Math.max(0, Math.floor(diff / 60_000))
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

function notificationIcon(type: ApiNotification["type"]) {
  switch (type) {
    case "VoteMilestone":
    case "StreakMilestone":
      return TrendingUp
    case "LevelUp":
      return Trophy
    case "PollTrending":
      return TrendingUp
    case "ChallengeAvailable":
    case "StreakReminder":
    case "PollExpiring":
      return Bell
    default:
      return Sparkles
  }
}

const preferenceLabels: Record<ApiNotification["type"], string> = {
  VoteMilestone: "Vote milestones",
  LevelUp: "Level ups",
  PollTrending: "Trending polls",
  DailyReminder: "Daily reminders",
  ChallengeAvailable: "Daily challenges",
  StreakReminder: "Streak risk",
  StreakMilestone: "Streak milestones",
  PollExpiring: "Expiring polls",
}

export default function NotificationsPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [notifications, setNotifications] = useState<ApiNotification[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [preferences, setPreferences] = useState<ApiNotificationPreference[]>([])
  const [savingPreference, setSavingPreference] = useState<ApiNotification["type"] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadNotifications = useCallback(async () => {
    if (!isAuthenticated) return

    setLoading(true)
    setError(null)
    try {
      const [response, loadedPreferences] = await Promise.all([
        notificationsApi.getAll(),
        notificationsApi.getPreferences(),
      ])
      setNotifications(response.notifications)
      setUnreadCount(response.unreadCount)
      setPreferences(loadedPreferences)
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load notifications")
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated])

  useEffect(() => {
    if (authLoading) return
    if (!isAuthenticated) {
      router.push("/login?message=Sign in to view notifications&redirect=/notifications")
      return
    }
    loadNotifications()
  }, [authLoading, isAuthenticated, loadNotifications, router])

  async function markRead(notification: ApiNotification) {
    if (!notification.isRead) {
      setNotifications((current) => current.map((item) =>
        item.id === notification.id ? { ...item, isRead: true } : item))
      setUnreadCount((count) => Math.max(0, count - 1))
      await notificationsApi.markRead(notification.id).catch(() => loadNotifications())
    }
    if (notification.pollId) router.push(`/polls/${notification.pollId}`)
  }

  async function markAllRead() {
    setNotifications((current) => current.map((item) => ({ ...item, isRead: true })))
    setUnreadCount(0)
    await notificationsApi.markAllRead().catch(() => loadNotifications())
  }

  async function togglePreference(type: ApiNotification["type"], isEnabled: boolean) {
    setSavingPreference(type)
    const next = preferences.map((preference) =>
      preference.type === type ? { ...preference, isEnabled } : preference
    )
    setPreferences(next)

    try {
      const disabledTypes = next
        .filter((preference) => !preference.isEnabled)
        .map((preference) => preference.type)
      const updated = await notificationsApi.updatePreferences(disabledTypes)
      setPreferences(updated)
    } catch {
      loadNotifications()
    } finally {
      setSavingPreference(null)
    }
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-3xl px-4 py-6">
        <div className="mb-6 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="relative rounded-2xl bg-primary p-3 text-primary-foreground">
              <Bell className="h-6 w-6" />
              {unreadCount > 0 && (
                <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full bg-destructive px-1 text-xs font-bold text-destructive-foreground">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground md:text-3xl">
                Notifications
              </h1>
              <p className="text-sm text-muted-foreground">
                {unreadCount > 0 ? `${unreadCount} unread updates` : "You're all caught up"}
              </p>
            </div>
          </div>

          <div className="flex gap-2">
            <Button
              aria-label="Refresh notifications"
              onClick={loadNotifications}
              size="icon"
              variant="outline"
            >
              <RefreshCw className="h-4 w-4" />
            </Button>
            <Button
              className="gap-2"
              disabled={unreadCount === 0}
              onClick={markAllRead}
              variant="outline"
            >
              <CheckCheck className="h-4 w-4" />
              Mark all read
            </Button>
          </div>
        </div>

        {loading && (
          <div className="flex items-center justify-center gap-3 py-20 text-muted-foreground">
            <Loader2 className="h-5 w-5 animate-spin" />
            Loading notifications...
          </div>
        )}

        {!loading && error && (
          <div className="rounded-2xl border border-destructive/20 bg-destructive/10 p-6 text-center">
            <p className="font-semibold text-destructive">Could not load notifications</p>
            <p className="mt-1 text-sm text-muted-foreground">{error}</p>
          </div>
        )}

        {!loading && !error && notifications.length === 0 && (
          <div className="flex flex-col items-center justify-center rounded-2xl border border-border/60 bg-card px-6 py-16 text-center">
            <Bell className="h-12 w-12 text-muted-foreground" />
            <h2 className="mt-4 text-lg font-semibold text-foreground">
              No notifications yet
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Start voting to earn XP!
            </p>
          </div>
        )}

        {!loading && !error && preferences.length > 0 && (
          <section className="mb-6 rounded-2xl border border-border/60 bg-card p-4">
            <div className="mb-3">
              <h2 className="font-semibold text-foreground">Notification preferences</h2>
              <p className="text-sm text-muted-foreground">
                Control which retention reminders appear in your inbox.
              </p>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              {preferences.map((preference) => (
                <label
                  className="flex items-center justify-between gap-3 rounded-xl border border-border/50 px-3 py-2"
                  key={preference.type}
                >
                  <span className="text-sm font-medium text-foreground">
                    {preferenceLabels[preference.type]}
                  </span>
                  <Switch
                    checked={preference.isEnabled}
                    disabled={savingPreference === preference.type}
                    onCheckedChange={(checked) => togglePreference(preference.type, checked)}
                  />
                </label>
              ))}
            </div>
          </section>
        )}

        {!loading && !error && notifications.length > 0 && (
          <div className="space-y-3">
            {notifications.map((notification) => {
              const Icon = notificationIcon(notification.type)

              return (
                <button
                  className={cn(
                    "flex w-full gap-4 rounded-2xl bg-card p-4 text-left ring-1 ring-border/50 transition hover:shadow-md",
                    !notification.isRead && "bg-primary/5 ring-primary/25"
                  )}
                  key={notification.id}
                  onClick={() => markRead(notification)}
                  type="button"
                >
                  <div className="flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                    <Icon className="h-5 w-5" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-3">
                      <h3 className="font-semibold text-foreground">
                        {notification.title}
                      </h3>
                      {!notification.isRead && (
                        <span className="mt-1 h-2.5 w-2.5 flex-shrink-0 rounded-full bg-primary" />
                      )}
                    </div>
                    <p className="mt-1 text-sm leading-6 text-muted-foreground">
                      {notification.body}
                    </p>
                    <p className="mt-2 text-xs text-muted-foreground">
                      {relativeTime(notification.createdAt)}
                    </p>
                  </div>
                </button>
              )
            })}
          </div>
        )}
      </div>
    </AppShell>
  )
}
