import { getAnalyticsConsent, getAnonymousId } from './privacy';
import { sanitize, type AnalyticsEventName, type AnalyticsEvents } from './events';
import { Platform } from 'react-native';
export interface AnalyticsAdapter { capture(payload: Record<string, unknown>): void | Promise<void>; identify?(id: string): void | Promise<void>; alias?(anonymousId: string, id: string): void | Promise<void>; reset?(): void | Promise<void>; }
let adapter: AnalyticsAdapter | null = null; const recent = new Map<string, number>(); let userId: string | null = null;
export function configureAnalytics(value: AnalyticsAdapter | null) { adapter = value; }
export function configureHttpAnalytics(endpoint?: string) { configureAnalytics(endpoint ? { capture: async payload => { await fetch(endpoint, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(payload) }); } } : null); }
export async function track<N extends AnalyticsEventName>(name: N, properties: AnalyticsEvents[N], semanticKey?: string) { try { if (!adapter || await getAnalyticsConsent() !== 'granted') return; const key = `${name}:${semanticKey ?? JSON.stringify(properties)}`; const now = Date.now(); if ((recent.get(key) ?? 0) > now - 5000) return; recent.set(key, now); await adapter.capture({ event: name, properties: sanitize(name, properties), event_id: `${now}-${Math.random()}`, occurred_at: new Date().toISOString(), schema_version: 1, source: 'client', platform: Platform.OS === 'ios' ? 'ios' : 'android', app_version: '1.0.0', anonymous_id: await getAnonymousId(), user_id: userId }); } catch {} }
export async function identify(id: number | string) { try { if (!adapter || await getAnalyticsConsent() !== 'granted') return; userId = `usr_${id}`; await adapter.alias?.(await getAnonymousId(), userId); await adapter.identify?.(userId); } catch {} }
export async function resetAnalytics() { userId = null; try { await adapter?.reset?.(); } catch {} }
