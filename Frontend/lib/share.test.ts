import { afterEach, describe, expect, it } from "vitest"
import { popLiveInvitationUrl } from "./share"

describe("PoP Live invitation URLs", () => {
  const original = process.env.NEXT_PUBLIC_BASE_URL
  afterEach(() => { process.env.NEXT_PUBLIC_BASE_URL = original })

  it("builds the canonical HTTPS fallback", () => {
    process.env.NEXT_PUBLIC_BASE_URL = "https://pollify.example.com/"
    expect(popLiveInvitationUrl("abcdefghijklmnop")).toBe("https://pollify.example.com/live/join/abcdefghijklmnop")
  })

  it("rejects non-opaque tokens and non-HTTPS hosts", () => {
    process.env.NEXT_PUBLIC_BASE_URL = "https://pollify.example.com"
    expect(() => popLiveInvitationUrl("session/42?vote=yes")).toThrow(/Invalid/)
    process.env.NEXT_PUBLIC_BASE_URL = "http://localhost:3000"
    expect(() => popLiveInvitationUrl("abcdefghijklmnop")).toThrow(/HTTPS/)
  })
})
