import { describe, expect, it } from "vitest"
import { sanitizeProperties } from "./events"
describe("analytics event contract", () => {
  it("accepts documented typed properties", () => expect(sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 })).toEqual({ round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 10 }))
  it.each(["email", "token", "wellness_response", "selected_option_id", "free_text"])("rejects %s", (key) => expect(() => sanitizeProperties("game_round_completed", { round_id: "r", surface: "feed", outcome: "voted", xp_awarded: 1, [key]: "secret" } as never)).toThrow(/not allowed/))
})
