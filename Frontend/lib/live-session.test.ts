import { describe, expect, it } from "vitest"
import { millisecondsUntil, serverClockOffset } from "@/lib/live-session"

describe("live session clock synchronization", () => {
  it("uses server time when the client clock is fast", () => {
    const clientNow = Date.parse("2026-08-06T12:00:05Z")
    const offset = serverClockOffset("2026-08-06T12:00:00Z", clientNow)
    expect(offset).toBe(-5_000)
    expect(millisecondsUntil("2026-08-06T12:00:02Z", offset, clientNow)).toBe(2_000)
  })

  it("reveals immediately when an event arrives late", () => {
    expect(millisecondsUntil("2026-08-06T12:00:02Z", 0, Date.parse("2026-08-06T12:00:03Z"))).toBe(0)
  })
})
