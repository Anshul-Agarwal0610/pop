"use client"
import { useCallback, useEffect, useRef, useState } from "react"
import { pollTossApi, type PollTossInvitation } from "@/lib/api"

export type PollTossPhase = "idle" | "creating" | "ready" | "tossing" | "waiting" | "accepted" | "failed" | "cancelled" | "expired"

export function usePollToss(pollId: number) {
  const [phase, setPhase] = useState<PollTossPhase>("idle")
  const [invitation, setInvitation] = useState<PollTossInvitation | null>(null)
  const [message, setMessage] = useState("")
  const latestVersion = useRef(0)

  const reconcile = useCallback((next: PollTossInvitation) => {
    if (next.stateVersion < latestVersion.current) return
    latestVersion.current = next.stateVersion
    setInvitation(current => ({ ...current, ...next }))
    if (next.status === "Accepted") { setPhase("accepted"); setMessage("Your poll arrived.") }
    if (next.status === "Cancelled") { setPhase("cancelled"); setMessage("Toss cancelled. The poll stayed here.") }
    if (next.status === "Expired") { setPhase("expired"); setMessage("The invitation expired. The poll stayed here.") }
  }, [])

  const create = useCallback(async () => {
    setPhase("creating"); setMessage("Creating a secure invitation…")
    try { const next = await pollTossApi.create(pollId); latestVersion.current = next.stateVersion; setInvitation(next); setPhase("ready"); setMessage("Invitation ready."); return next }
    catch { setPhase("failed"); setMessage("Could not create the invitation. The poll stayed here."); return null }
  }, [pollId])
  const toss = useCallback(() => { if (!invitation) return; setPhase("tossing"); setMessage("Tossing poll…"); window.setTimeout(() => { setPhase("waiting"); setMessage("Waiting for someone to accept…") }, 450) }, [invitation])
  const cancel = useCallback(async () => { if (invitation) { try { reconcile(await pollTossApi.cancel(invitation.id)) } catch { setPhase("failed"); setMessage("Could not cancel. Check the invitation status.") } } else setPhase("idle") }, [invitation, reconcile])
  const reset = useCallback(() => { latestVersion.current = 0; setInvitation(null); setPhase("idle"); setMessage("") }, [])

  useEffect(() => {
    if (phase !== "waiting" || !invitation) return
    const poll = window.setInterval(() => void pollTossApi.get(invitation.id).then(reconcile).catch(() => undefined), 3000)
    return () => window.clearInterval(poll)
  }, [phase, invitation, reconcile])
  return { phase, invitation, message, create, toss, cancel, reset, reconcile }
}
