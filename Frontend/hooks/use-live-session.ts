"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import type { ApiLiveSessionState } from "@/lib/api"
import { liveSessionsApi } from "@/lib/api"
import { createLiveSessionConnection, type LiveSessionEvent, serverClockOffset } from "@/lib/live-session"
import { millisecondsUntil } from "@/lib/live-session"

const FALLBACK_POLL_MS = 4_000

export function useLiveSession(sessionId: string) {
  const [state, setState] = useState<ApiLiveSessionState | null>(null)
  const [connected, setConnected] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const version = useRef(0)
  const clockOffsetMs = useRef(0)
  const voting = useRef(false)

  const reconcile = useCallback(async () => {
    try {
      const next = await liveSessionsApi.get(sessionId)
      version.current = next.stateVersion
      clockOffsetMs.current = serverClockOffset(next.serverNow)
      setState(next)
      setError(null)
      return next
    } catch (cause) {
      setError(cause instanceof Error ? cause : new Error("Unable to refresh live session"))
      throw cause
    }
  }, [sessionId])

  useEffect(() => {
    if (!sessionId) return
    let disposed = false
    const connection = createLiveSessionConnection()
    const onEvent = (event: LiveSessionEvent) => {
      if (event.sessionId.toLowerCase() !== sessionId.toLowerCase()) return
      if (event.stateVersion > 0 && event.stateVersion <= version.current) return
      if (event.serverNow) clockOffsetMs.current = serverClockOffset(event.serverNow)
      void reconcile()
    }
    connection.on("liveSessionEvent", onEvent)
    connection.onreconnecting(() => { if (!disposed) setConnected(false) })
    connection.onreconnected(async () => {
      if (disposed) return
      await connection.invoke("JoinSession", sessionId)
      setConnected(true)
      await reconcile()
    })
    connection.onclose(() => { if (!disposed) setConnected(false) })

    void reconcile()
    void connection.start()
      .then(() => connection.invoke("JoinSession", sessionId))
      .then(() => { if (!disposed) setConnected(true) })
      .catch(() => { if (!disposed) setConnected(false) })

    return () => {
      disposed = true
      connection.off("liveSessionEvent", onEvent)
      void connection.stop()
    }
  }, [sessionId, reconcile])

  useEffect(() => {
    if (connected || !sessionId) return
    const timer = window.setInterval(() => { void reconcile() }, FALLBACK_POLL_MS)
    return () => window.clearInterval(timer)
  }, [connected, sessionId, reconcile])

  // The event is only a wake-up hint. At the persisted deadline, reconcile through REST;
  // this also covers a reveal event lost while the connection was switching transports.
  useEffect(() => {
    if (!state?.revealAt || state.status !== "Voting") return
    const delay = millisecondsUntil(state.revealAt, clockOffsetMs.current)
    const timer = window.setTimeout(() => { void reconcile() }, delay + 25)
    return () => window.clearTimeout(timer)
  }, [state?.revealAt, state?.status, reconcile])

  const setReady = useCallback(async (isReady: boolean) => {
    const next = await liveSessionsApi.ready(sessionId, isReady)
    version.current = next.stateVersion
    setState(next)
  }, [sessionId])

  const lockVote = useCallback(async (optionId: number, idempotencyKey = crypto.randomUUID()) => {
    if (!state || state.myOptionId !== null || voting.current) return null
    voting.current = true
    try {
      const result = await liveSessionsApi.vote(sessionId, state.currentRound, optionId, idempotencyKey)
      version.current = result.state.stateVersion
      clockOffsetMs.current = serverClockOffset(result.state.serverNow)
      setState(result.state)
      return result
    } finally { voting.current = false }
  }, [sessionId, state])

  return { state, connected, error, clockOffsetMs: clockOffsetMs.current, refresh: reconcile, setReady, lockVote }
}
