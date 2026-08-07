"use client"
import { motion, useReducedMotion } from "framer-motion"
import { AlertCircle, Loader2, Send } from "lucide-react"
import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { useOptionalHaptics } from "@/hooks/use-optional-haptics"
import { pollTossApi, type PollTossInvitation } from "@/lib/api"
import { track } from "@/lib/analytics/client"

export function PollTossIncoming({ token }: { token: string }) {
  const [invite, setInvite] = useState<PollTossInvitation | null>(null)
  const [error, setError] = useState("")
  const [accepting, setAccepting] = useState(false)
  const reduce = Boolean(useReducedMotion()); const haptics = useOptionalHaptics(); const router = useRouter()
  useEffect(() => { track("poll_toss_recipient_arrived", { entry_method: "link" }); void pollTossApi.preview(token).then(setInvite).catch(() => setError("This invitation is unavailable or has expired.")) }, [token])
  async function accept() { setAccepting(true); try { const accepted = await pollTossApi.accept(token); setInvite(accepted); haptics.pulse(); window.setTimeout(() => router.push(`/polls/${accepted.pollId}`), reduce ? 100 : 850) } catch { setError("This invitation could not be accepted. It may already be used or expired.") } finally { setAccepting(false) } }
  if (error) return <State text={error} />
  if (!invite) return <div className="flex items-center gap-2"><Loader2 className="animate-spin" /> Catching the poll…</div>
  if (invite.status !== "Pending") return <State text={invite.status === "Expired" ? "This Poll Toss expired." : invite.status === "Cancelled" ? "The sender cancelled this Poll Toss." : "This Poll Toss has already been accepted."} />
  return <motion.article className="w-full max-w-md rounded-3xl border bg-card p-6 text-center shadow-2xl" initial={reduce ? { opacity: 0 } : { y: -180, rotate: -6, opacity: 0 }} animate={{ y: 0, rotate: 0, opacity: 1 }} transition={{ duration: reduce ? 0.1 : 0.55 }}>
    <Send className="mx-auto mb-3 h-9 w-9 text-primary" /><p className="text-sm font-medium text-primary">Incoming Poll Toss</p><h1 className="mt-2 text-2xl font-black">{invite.poll?.question}</h1><p className="mt-3 text-sm text-muted-foreground">Accept deliberately to catch this challenge.</p><Button className="mt-6 w-full" disabled={accepting} onClick={() => void accept()}>{accepting ? "Accepting…" : "Catch poll"}</Button>
  </motion.article>
}
function State({ text }: { text: string }) { return <div className="max-w-md rounded-2xl border bg-card p-6 text-center"><AlertCircle className="mx-auto mb-3" /><h1 className="text-xl font-bold">Poll Toss unavailable</h1><p className="mt-2 text-muted-foreground">{text}</p></div> }
