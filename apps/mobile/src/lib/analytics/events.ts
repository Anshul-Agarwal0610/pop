export interface AnalyticsEvents {
  gamification_hub_viewed: { surface: 'home' | 'profile' | 'leaderboard'; challenge_count: number; level: number };
  game_round_started: { round_id: string; surface: 'feed' | 'detail'; category: string };
  game_round_completed: { round_id: string; surface: 'feed' | 'detail'; outcome: 'voted'; xp_awarded: number };
  gamification_satisfaction_submitted: { score: 1 | 2 | 3 | 4 | 5; reason_code?: 'fun' | 'motivating' | 'confusing' | 'distracting' };
}
export type AnalyticsEventName = keyof AnalyticsEvents;
export const allowedProperties: Record<AnalyticsEventName, readonly string[]> = {
  gamification_hub_viewed: ['surface', 'challenge_count', 'level'], game_round_started: ['round_id', 'surface', 'category'],
  game_round_completed: ['round_id', 'surface', 'outcome', 'xp_awarded'], gamification_satisfaction_submitted: ['score', 'reason_code'],
};
export function sanitize<N extends AnalyticsEventName>(name: N, values: AnalyticsEvents[N]) {
  const forbidden = /answer|option|question|description|wellness|health|email|username|token|url|error|text/i;
  for (const key of Object.keys(values)) if (forbidden.test(key) || !allowedProperties[name].includes(key)) throw new Error(`Analytics property is not allowed: ${key}`);
  return values;
}
