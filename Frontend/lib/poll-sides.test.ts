import { describe, expect, it } from "vitest"
import { canonicalGeneratedOptions, pollOptionLabel } from "./poll-sides"
describe("generated poll sides", () => {
  it("maps reversed API order by stable side", () => {
    const against = { id: 2, side: "Against" as const, text: "wrong" }
    const up = { id: 1, side: "Up" as const, text: "wrong" }
    expect(canonicalGeneratedOptions([against, up])).toEqual({ Up: up, Against: against })
    expect(pollOptionLabel(up, true)).toBe("Up")
  })
  it.each([[], [{ side: "Up" as const }], [{ side: "Up" as const }, { side: "Up" as const }], [{ side: "Up" as const }, { side: null }]])("rejects invalid shapes", (options) => expect(canonicalGeneratedOptions(options)).toBeNull())
  it("preserves custom labels", () => expect(pollOptionLabel({ text: "Maybe", side: null }, false)).toBe("Maybe"))
})
