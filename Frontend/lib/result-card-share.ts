export type ResultCardShareData = { title: string; text: string; url: string }

export function canNativeShare(data: ResultCardShareData) {
  return typeof navigator !== "undefined" && typeof navigator.share === "function" &&
    (typeof navigator.canShare !== "function" || navigator.canShare(data))
}

export async function nativeShare(data: ResultCardShareData) {
  if (!canNativeShare(data)) return "unsupported" as const
  try { await navigator.share(data); return "shared" as const }
  catch (error) { return (error as DOMException).name === "AbortError" ? "aborted" as const : "failed" as const }
}

export async function copyResultCardLink(url: string) {
  if (!navigator.clipboard?.writeText) return false
  try { await navigator.clipboard.writeText(url); return true } catch { return false }
}
