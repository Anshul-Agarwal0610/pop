import * as SecureStore from 'expo-secure-store';
export type AnalyticsConsent = 'unknown' | 'granted' | 'denied';
const CONSENT = 'pollify_analytics_consent'; const ANON = 'pollify_analytics_anonymous_id';
export async function getAnalyticsConsent(): Promise<AnalyticsConsent> { const value = await SecureStore.getItemAsync(CONSENT).catch(() => null); return value === 'granted' || value === 'denied' ? value : 'unknown'; }
export async function setAnalyticsConsent(value: AnalyticsConsent) { await SecureStore.setItemAsync(CONSENT, value); }
export async function getAnonymousId() { let id = await SecureStore.getItemAsync(ANON); if (!id) { id = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`; await SecureStore.setItemAsync(ANON, id); } return id; }
