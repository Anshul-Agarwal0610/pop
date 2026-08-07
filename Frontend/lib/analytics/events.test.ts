import { describe, expect, it } from "vitest"
import { sanitizeProperties } from "./events"
describe("analytics event contract", () => {
  it("accepts documented typed properties", () => expect(sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 })).toEqual({ round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 }))
  it.each(["email", "token", "wellness_response", "selected_option_id", "free_text"])("rejects %s", (key) => expect(() => sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 1, [key]: "secret" } as never)).toThrow(/not allowed/))
  it("allows only coarse Poll Toss funnel fields", () => expect(sanitizeProperties("poll_toss_channel_selected", { surface: "feed", channel: "room_code" })).toEqual({ surface: "feed", channel: "room_code" }))
  it.each(["token", "url", "question", "poll_id", "recipient_id"])("rejects Poll Toss %s", (key) => expect(() => sanitizeProperties("poll_toss_opened", { surface: "feed", reduced_motion: false, [key]: "secret" } as never)).toThrow(/not allowed/))
})
