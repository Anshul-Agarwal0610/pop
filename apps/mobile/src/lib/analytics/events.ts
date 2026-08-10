export interface AnalyticsEvents {
  gamification_hub_viewed: { surface: 'home' | 'profile' | 'leaderboard'; challenge_count: number; level: number };
  game_round_started: { round_id: string; surface: 'feed' | 'detail'; category: string };
  game_round_completed: { round_id: string; surface: 'feed' | 'detail'; outcome: 'voted'; xp_awarded: number };
  gamification_satisfaction_submitted: { score: 1 | 2 | 3 | 4 | 5; reason_code?: 'fun' | 'motivating' | 'confusing' | 'distracting' };
  pop_live_toss_shown: PopLiveProperties; pop_live_invitation_created: PopLiveProperties; pop_live_invitation_opened: PopLiveProperties;
  pop_live_session_joined: PopLiveProperties; pop_live_first_response_locked: PopLiveProperties; pop_live_session_completed: PopLiveProperties;
  pop_live_result_shared: PopLiveProperties; pop_live_rematch_requested: PopLiveProperties; pop_live_rematch_started: PopLiveProperties;
  pop_live_relay_handoff: PopLiveProperties & { handoff_index: number };
}
export type PopLiveMode = 'poll_toss' | 'poll_clash' | 'poll_relay' | 'poll_bomb' | 'live_room';
export interface PopLiveProperties { journey_id: string; mode: PopLiveMode; platform: 'ios' | 'android'; source: 'client'; invitation_channel: 'link' | 'room_code' | 'native_share' | 'in_app' | 'none'; completion_reason: 'completed' | 'expired' | 'cancelled' | 'target_not_reached' | 'none'; experiment_id: 'pop_live_funnel_v1'; experiment_variant: 'control' | 'treatment'; }
export type AnalyticsEventName = keyof AnalyticsEvents;
const popLive = ['journey_id', 'mode', 'platform', 'source', 'invitation_channel', 'completion_reason', 'experiment_id', 'experiment_variant'] as const;
export const allowedProperties: Record<AnalyticsEventName, readonly string[]> = {
  gamification_hub_viewed: ['surface', 'challenge_count', 'level'], game_round_started: ['round_id', 'surface', 'category'],
  game_round_completed: ['round_id', 'surface', 'outcome', 'xp_awarded'], gamification_satisfaction_submitted: ['score', 'reason_code'],
  pop_live_toss_shown: popLive, pop_live_invitation_created: popLive, pop_live_invitation_opened: popLive,
  pop_live_session_joined: popLive, pop_live_first_response_locked: popLive, pop_live_session_completed: popLive,
  pop_live_result_shared: popLive, pop_live_rematch_requested: popLive, pop_live_rematch_started: popLive,
  pop_live_relay_handoff: [...popLive, 'handoff_index'],
};
export function sanitize<N extends AnalyticsEventName>(name: N, values: AnalyticsEvents[N]) {
  const forbidden = /answer|option|question|description|wellness|health|email|username|token|url|error|text/i;
  for (const key of Object.keys(values)) if (forbidden.test(key) || !allowedProperties[name].includes(key)) throw new Error(`Analytics property is not allowed: ${key}`);
  return values;
}
