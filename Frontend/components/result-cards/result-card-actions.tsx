"use client"
import { useState } from "react"
import { Button } from "@/components/ui/button"
import type { ApiResultCard } from "@/lib/api"
import { canNativeShare, copyResultCardLink, nativeShare } from "@/lib/result-card-share"

export function ResultCardActions({ card }: { card: ApiResultCard }) {
  const [message, setMessage] = useState("")
  const data = { title: `PoP Live ${card.payload.mode}`, text: card.payload.accessibleSummary, url: card.publicUrl }
  async function share() { const result = await nativeShare(data); if (result === "failed") setMessage("Sharing failed. Copy the link or download the card instead.") }
  async function copy() { setMessage(await copyResultCardLink(card.publicUrl) ? "Link copied." : "Copy is unavailable. Select this link: " + card.publicUrl) }
  return <div className="mt-3 flex flex-wrap gap-2" aria-label="Share this memory">
    {canNativeShare(data) && <Button type="button" onClick={share}>Share</Button>}
    <Button type="button" variant="outline" onClick={copy}>Copy link</Button>
    <Button asChild variant="outline"><a href={card.imageUrl} download>Download image</a></Button>
    {message && <p className="w-full text-sm" role="status">{message}</p>}
  </div>
}
