"use client"
import { useEffect } from "react"
import { useAuth } from "@/contexts/auth-context"
import { configureAnalytics, identify, resetAnalytics } from "./client"
export function AnalyticsProvider({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth()
  useEffect(() => {
    const endpoint = process.env.NEXT_PUBLIC_ANALYTICS_CAPTURE_URL
    configureAnalytics(endpoint ? { capture: payload => { void fetch(endpoint, { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify(payload), keepalive: true }).catch(() => undefined) } } : null)
    return () => configureAnalytics(null)
  }, [])
  useEffect(() => { if (!isLoading) { if (user) identify(user.id); else resetAnalytics() } }, [isLoading, user])
  return children
}
