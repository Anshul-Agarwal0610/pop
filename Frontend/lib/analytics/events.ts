export const analyticsEventProperties = {
  gamification_hub_viewed: ["surface", "challenge_count", "level"],
  challenge_started: ["challenge_id", "challenge_type", "required_actions"],
  challenge_progressed: ["challenge_id", "progress", "required_actions"],
  challenge_completed: ["challenge_id", "reward_xp", "badge_granted"],
  streak_changed: ["previous_streak", "current_streak", "change_reason"],
  level_up: ["previous_level", "current_level"],
  achievement_unlocked: ["achievement_code", "reward_xp"],
  game_round_started: ["round_id", "surface", "category"],
  game_round_completed: ["round_id", "surface", "outcome", "xp_awarded"],
  gamification_satisfaction_submitted: ["score", "reason_code"],
  poll_toss_opened: ["surface", "reduced_motion"],
  poll_toss_invitation_created: ["surface"],
  poll_toss_channel_selected: ["surface", "channel"],
  poll_toss_animation_started: ["trigger", "reduced_motion", "haptics_used"],
  poll_toss_recipient_arrived: ["entry_method"],
  poll_toss_completed: ["surface", "channel"],
  poll_toss_ended: ["surface", "outcome"],
} as const

export interface AnalyticsEvents {
  gamification_hub_viewed: { surface: "home" | "profile" | "leaderboard"; challenge_count: number; level: number }
  challenge_started: { challenge_id: string; challenge_type: string; required_actions: number }
  challenge_progressed: { challenge_id: string; progress: number; required_actions: number }
  challenge_completed: { challenge_id: string; reward_xp: number; badge_granted: boolean }
  streak_changed: { previous_streak: number; current_streak: number; change_reason: "advanced" | "reset" }
  level_up: { previous_level: number; current_level: number }
  achievement_unlocked: { achievement_code: string; reward_xp: number }
  game_round_started: { round_id: string; surface: "feed" | "detail"; category: string }
  game_round_completed: { round_id: string; surface: "feed" | "detail"; outcome: "voted"; xp_awarded: number }
  gamification_satisfaction_submitted: { score: 1 | 2 | 3 | 4 | 5; reason_code?: "fun" | "motivating" | "confusing" | "distracting" }
  poll_toss_opened: { surface: "feed" | "detail"; reduced_motion: boolean }
  poll_toss_invitation_created: { surface: "feed" | "detail" }
  poll_toss_channel_selected: { surface: "feed" | "detail"; channel: "qr" | "share_sheet" | "link" | "room_code" }
  poll_toss_animation_started: { trigger: "button" | "gesture"; reduced_motion: boolean; haptics_used: boolean }
  poll_toss_recipient_arrived: { entry_method: "link" | "room_code" }
  poll_toss_completed: { surface: "feed" | "detail"; channel: "qr" | "share_sheet" | "link" | "room_code" }
  poll_toss_ended: { surface: "feed" | "detail"; outcome: "cancelled" | "failed" | "expired" }
}

export type AnalyticsEventName = keyof AnalyticsEvents

const forbidden = /answer|option|question|description|wellness|health|email|username|display.?name|token|jwt|url|error|free.?text/i

export function sanitizeProperties<N extends AnalyticsEventName>(name: N, properties: AnalyticsEvents[N]): AnalyticsEvents[N] {
  const allowed = new Set<string>(analyticsEventProperties[name])
  const output: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(properties)) {
    if (forbidden.test(key) || !allowed.has(key)) throw new Error(`Analytics property is not allowed: ${key}`)
    if (typeof value !== "string" && typeof value !== "number" && typeof value !== "boolean" && value !== undefined) {
      throw new Error(`Analytics property has an invalid type: ${key}`)
    }
    if (value !== undefined) output[key] = value
  }
  return output as AnalyticsEvents[N]
}
