export type PollSide = "Up" | "Against"
export const pollSideLabels: Record<PollSide, string> = { Up: "Up", Against: "Against" }
export type SidedOption = { side?: PollSide | null }
export function canonicalGeneratedOptions<T extends SidedOption>(options?: readonly T[] | null) {
  if (!options || options.length !== 2) return null
  const up = options.filter((option) => option.side === "Up")
  const against = options.filter((option) => option.side === "Against")
  return up.length === 1 && against.length === 1 ? { Up: up[0], Against: against[0] } : null
}
export function pollOptionLabel(option: { text: string; side?: PollSide | null }, generated: boolean) {
  if (!generated) return option.text
  return option.side ? pollSideLabels[option.side] : "Invalid choice"
}
