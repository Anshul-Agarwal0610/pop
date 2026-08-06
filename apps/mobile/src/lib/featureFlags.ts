export type FeatureFlag = 'gamification_challenges_v1' | 'gamification_streaks_v1' | 'gamification_achievements_v1' | 'gamification_round_experience_v1' | 'pop_live_shareplay_spike_v1' | 'pop_live_nfc_spike_v1';
export function rolloutBucket(flag: FeatureFlag, subject: string) { let hash = 2166136261; for (const char of `${flag}:${subject}`) { hash ^= char.charCodeAt(0); hash = Math.imul(hash, 16777619); } return (hash >>> 0) % 10000; }
export function getFeatureFlag(flag: FeatureFlag, subject: string, rolloutPercent = 100) { return rolloutBucket(flag, subject) < rolloutPercent * 100; }
