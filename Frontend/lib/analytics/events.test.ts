import { describe, expect, it } from "vitest"
import { analyticsEventProperties, sanitizeProperties } from "./events"
describe("analytics event contract", () => {
  it("defines the complete PoP Live funnel", () => {
    expect(Object.keys(analyticsEventProperties).filter(name => name.startsWith("pop_live_")).sort()).toEqual([
      "pop_live_first_response_locked", "pop_live_invitation_created", "pop_live_invitation_opened", "pop_live_relay_handoff",
      "pop_live_rematch_requested", "pop_live_rematch_started", "pop_live_result_shared", "pop_live_session_completed",
      "pop_live_session_joined", "pop_live_toss_shown",
    ])
  })
  it("accepts documented typed properties", () => expect(sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 })).toEqual({ round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 }))
  it.each(["email", "token", "wellness_response", "selected_option_id", "free_text"])("rejects %s", (key) => expect(() => sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 1, [key]: "secret" } as never)).toThrow(/not allowed/))
})
