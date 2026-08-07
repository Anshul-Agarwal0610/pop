import { describe, expect, it } from "vitest"
import { isShareCancellation, isTossEligible } from "./poll-toss"

describe("poll toss safety", () => {
  it("only permits active public non-health polls", () => {
    expect(isTossEligible({ category: "News", isActive: true, expiresAt: new Date(Date.now()+10000).toISOString() })).toBe(true)
    expect(isTossEligible({ category: "Health" })).toBe(false)
    expect(isTossEligible({ category: "News", isPrivate: true })).toBe(false)
    expect(isTossEligible({ category: "News", isWellness: true })).toBe(false)
    expect(isTossEligible({ category: "News", isActive: false })).toBe(false)
    expect(isTossEligible({ category: "News", expiresAt: new Date(Date.now()-10000).toISOString() })).toBe(false)
  })
  it("recognizes native share cancellation", () => {
    expect(isShareCancellation(new DOMException("cancelled", "AbortError"))).toBe(true)
    expect(isShareCancellation(new Error("failed"))).toBe(false)
  })
})
