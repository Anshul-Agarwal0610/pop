import { getAnalyticsConsent, getAnonymousId } from "./privacy"
import { sanitizeProperties, type AnalyticsEventName, type AnalyticsEvents } from "./events"

export interface AnalyticsAdapter {
  capture(payload: Record<string, unknown>): void | Promise<void>
  identify?(userId: string): void | Promise<void>
  alias?(anonymousId: string, userId: string): void | Promise<void>
  reset?(): void | Promise<void>
}
let adapter: AnalyticsAdapter | null = null
let identifiedUser: string | null = null
const recent = new Map<string, number>()
export function configureAnalytics(next: AnalyticsAdapter | null) { adapter = next }
export function pseudonymousUserId(id: number | string) { return `usr_${id}` }
export function track<N extends AnalyticsEventName>(name: N, properties: AnalyticsEvents[N], semanticKey?: string): void {
  if (getAnalyticsConsent() !== "granted" || !adapter) return
  try {
    const now = Date.now(); const key = `${name}:${semanticKey ?? JSON.stringify(properties)}`
    if ((recent.get(key) ?? 0) > now - 5000) return
    recent.set(key, now)
    const eventId = crypto.randomUUID()
    void Promise.resolve(adapter.capture({ event: name, properties: sanitizeProperties(name, properties), event_id: eventId,
      occurred_at: new Date().toISOString(), schema_version: 1, source: "client", platform: "web",
      app_version: process.env.NEXT_PUBLIC_APP_VERSION ?? "unknown", anonymous_id: getAnonymousId(), user_id: identifiedUser }))
      .catch(() => undefined)
  } catch { /* Analytics must never affect product flows. */ }
}
export function identify(userId: number | string): void {
  if (getAnalyticsConsent() !== "granted" || !adapter) return
  const key = pseudonymousUserId(userId); identifiedUser = key
  try { void Promise.resolve(adapter.alias?.(getAnonymousId(), key)).catch(() => undefined); void Promise.resolve(adapter.identify?.(key)).catch(() => undefined) } catch {}
}
export function resetAnalytics(): void { identifiedUser = null; try { void Promise.resolve(adapter?.reset?.()).catch(() => undefined) } catch {} }
export function clearAnalyticsDedupeForTests() { recent.clear() }
