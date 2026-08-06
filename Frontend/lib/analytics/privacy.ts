export type AnalyticsConsent = "unknown" | "granted" | "denied"
const CONSENT_KEY = "pollify_analytics_consent"
const ANONYMOUS_KEY = "pollify_analytics_anonymous_id"

export function getAnalyticsConsent(): AnalyticsConsent {
  if (typeof window === "undefined") return "unknown"
  const value = localStorage.getItem(CONSENT_KEY)
  return value === "granted" || value === "denied" ? value : "unknown"
}
export function setAnalyticsConsent(value: AnalyticsConsent) { if (typeof window !== "undefined") localStorage.setItem(CONSENT_KEY, value) }
export function getAnonymousId(): string {
  if (typeof window === "undefined") return ""
  let id = localStorage.getItem(ANONYMOUS_KEY)
  if (!id) { id = crypto.randomUUID(); localStorage.setItem(ANONYMOUS_KEY, id) }
  return id
}
