import { describe, expect, it } from "vitest"
import { getSafeRedirect } from "@/lib/auth-redirect"

describe("getSafeRedirect", () => {
  it("preserves internal paths and query strings", () => {
    expect(getSafeRedirect("/polls/42?view=results")).toBe("/polls/42?view=results")
  })

  it.each([null, "", "https://example.com", "//example.com"])(
    "falls back Home for an unsafe redirect (%s)",
    (value) => expect(getSafeRedirect(value)).toBe("/"),
  )
})
