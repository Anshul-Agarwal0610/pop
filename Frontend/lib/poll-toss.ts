import { appBaseUrl } from "@/lib/share"

export type TossSurface = "feed" | "detail"
export type TossChannel = "qr" | "share_sheet" | "link" | "room_code"
export type TossOutcome = "cancelled" | "failed" | "expired"

export function invitationUrl(token: string) {
  return `${appBaseUrl()}/toss/${encodeURIComponent(token)}`
}

export function isTossEligible(poll: { category?: string | null; isPrivate?: boolean; isWellness?: boolean; isActive?: boolean; moderationStatus?: string; expiresAt?: string }) {
  return poll.isActive !== false && !poll.isPrivate && !poll.isWellness && poll.moderationStatus !== "Rejected" &&
    (!poll.expiresAt || new Date(poll.expiresAt).getTime() > Date.now()) && !poll.category?.toLowerCase().includes("health")
}

export function isShareCancellation(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError"
}
