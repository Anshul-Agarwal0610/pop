"use client"
import { QRCodeSVG } from "qrcode.react"
import { motion } from "framer-motion"
import { Copy, QrCode, Send, Share2 } from "lucide-react"
import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { useOptionalHaptics } from "@/hooks/use-optional-haptics"
import { usePollToss } from "@/hooks/use-poll-toss"
import { track } from "@/lib/analytics/client"
import { invitationUrl, isShareCancellation, isTossEligible, type TossChannel, type TossSurface } from "@/lib/poll-toss"
import { cn } from "@/lib/utils"

type TossPoll = { id: number | string; question: string; category?: string | null; isPrivate?: boolean; isWellness?: boolean; isActive?: boolean; moderationStatus?: string; expiresAt?: string }

export function PollTossButton({ poll, surface, className }: { poll: TossPoll; surface: TossSurface; className?: string }) {
  const [open, setOpen] = useState(false)
  const [channel, setChannel] = useState<TossChannel>("link")
  const { phase, invitation, message, create, toss, cancel, reset } = usePollToss(Number(poll.id))
  const haptics = useOptionalHaptics()
  const url = invitation?.inviteUrl || (invitation?.token ? invitationUrl(invitation.token) : "")

  useEffect(() => { if (phase === "accepted") { haptics.pulse(); track("poll_toss_completed", { surface, channel }) } }, [phase]) // eslint-disable-line react-hooks/exhaustive-deps
  if (!isTossEligible(poll)) return null

  async function openDialog() {
    setOpen(true); track("poll_toss_opened", { surface, reduced_motion: haptics.reducedMotion })
    const made = await create(); if (made) track("poll_toss_invitation_created", { surface })
  }
  async function select(next: TossChannel) {
    setChannel(next); track("poll_toss_channel_selected", { surface, channel: next })
    const value = next === "room_code" ? invitation?.roomCode : url
    if (!value) return
    if (next === "share_sheet" && navigator.share) {
      try { await navigator.share({ title: poll.question, text: "Catch this Poll Toss!", url }) }
      catch (error) { if (isShareCancellation(error)) return }
      return
    }
    if (next !== "qr") await navigator.clipboard.writeText(value)
  }
  function commit(trigger: "button" | "gesture") {
    const used = haptics.pulse(); track("poll_toss_animation_started", { trigger, reduced_motion: haptics.reducedMotion, haptics_used: used }); toss()
  }
  function close(next: boolean) {
    if (!next && !["accepted", "cancelled", "expired", "failed", "idle"].includes(phase)) void cancel()
    setOpen(next); if (!next) window.setTimeout(reset, 200)
  }
  return <>
    <Button aria-label="Toss poll" className={cn("gap-2", className)} onClick={(e) => { e.stopPropagation(); void openDialog() }} onPointerDown={e => e.stopPropagation()} size="sm" type="button" variant="secondary">
      <Send className="h-4 w-4" /><span className="sr-only sm:not-sr-only">Toss</span>
    </Button>
    <Dialog open={open} onOpenChange={close}>
      <DialogContent aria-describedby="poll-toss-description">
        <DialogHeader><DialogTitle>Toss this poll</DialogTitle><DialogDescription id="poll-toss-description">Create a secure, expiring challenge. It only counts as delivered after someone accepts.</DialogDescription></DialogHeader>
        <div aria-live="polite" className="min-h-5 text-sm text-muted-foreground">{message}</div>
        {invitation && <>
          <motion.div className="mx-auto w-full max-w-sm rounded-2xl border bg-card p-5 text-center shadow-lg" drag={phase === "ready" ? "y" : false} dragConstraints={{ top: 0, bottom: 0 }} onDragEnd={(_, info) => { if (info.offset.y < -70) commit("gesture") }} animate={phase === "tossing" ? (haptics.reducedMotion ? { opacity: 0 } : { y: -280, rotate: 8, opacity: 0 }) : { y: 0, rotate: 0, opacity: 1 }} transition={{ duration: haptics.reducedMotion ? 0.1 : 0.45 }}>
            <p className="font-bold">{poll.question}</p><p className="mt-2 text-xs text-muted-foreground">Swipe up or use Toss now</p>
          </motion.div>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            <Button variant="outline" onClick={() => void select("qr")}><QrCode /> QR</Button>
            <Button variant="outline" onClick={() => void select("share_sheet")}><Share2 /> Share</Button>
            <Button variant="outline" onClick={() => void select("link")}><Copy /> Link</Button>
            <Button variant="outline" onClick={() => void select("room_code")}><Copy /> Code</Button>
          </div>
          {channel === "qr" && url && <div className="mx-auto rounded-xl bg-white p-3"><QRCodeSVG aria-label="Invitation QR code" role="img" size={180} value={url} /></div>}
          <div className="rounded-lg bg-muted p-3 text-center"><span className="text-xs text-muted-foreground">Room code</span><div className="font-mono text-xl font-bold tracking-[.25em]">{invitation.roomCode}</div></div>
          <label className="flex items-center gap-2 text-sm"><input checked={haptics.enabled} disabled={!haptics.supported || haptics.reducedMotion} onChange={e => haptics.setEnabled(e.target.checked)} type="checkbox" /> Haptic feedback</label>
          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end"><Button variant="outline" onClick={() => close(false)}>Cancel</Button><Button disabled={phase !== "ready"} onClick={() => commit("button")}><Send /> Toss now</Button></div>
        </>}
        {phase === "failed" && <Button onClick={() => void create()}>Try again</Button>}
      </DialogContent>
    </Dialog>
  </>
}
