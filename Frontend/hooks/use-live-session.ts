"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { liveSessionsApi, type ApiLiveSessionState } from "@/lib/api"
import { getToken } from "@/lib/auth"
import { API_BASE_URL } from "@/lib/config"
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr"

export function useLiveSession(publicId: string) {
  const [state, setState] = useState<ApiLiveSessionState | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [connected, setConnected] = useState(false)
  const version = useRef(0)

  const reconcile = useCallback(async () => {
    try {
      const next = await liveSessionsApi.get(publicId)
      if (next.stateVersion >= version.current) {
        version.current = next.stateVersion
        setState(next)
      }
      setError(null)
      return next
    } catch (value) {
      setError(value instanceof Error ? value.message : "Could not load Poll Bomb")
      throw value
    }
  }, [publicId])

  useEffect(() => {
    let active = true
    void reconcile().catch(() => liveSessionsApi.join(publicId)
      .then(next => { if(active){version.current=next.stateVersion;setState(next);setError(null)}})
      .catch(value => { if(active)setError(value instanceof Error?value.message:"Could not join Poll Bomb") }))
    const timer = window.setInterval(() => { if (active && !connected) void reconcile().catch(() => undefined) }, 5000)
    return () => { active = false; window.clearInterval(timer) }
  }, [publicId, reconcile, connected])

  useEffect(() => {
    if (!state) return
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/live-sessions`, { accessTokenFactory: () => getToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    connection.on("liveSessionEvent", () => void reconcile().catch(() => undefined))
    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => { setConnected(true); void connection.invoke("Subscribe", publicId); void reconcile() })
    void connection.start().then(async () => { await connection.invoke("Subscribe", publicId); setConnected(true); await reconcile() }).catch(() => setConnected(false))
    return () => { setConnected(false); void connection.stop() }
  }, [publicId, reconcile, state?.participantId])

  const vote = async (optionId: number) => {
    const next = await liveSessionsApi.vote(publicId, optionId, crypto.randomUUID())
    version.current = next.stateVersion; setState(next)
  }
  const setNotifications = async (enabled: boolean) => {
    const next = await liveSessionsApi.notifications(publicId, enabled)
    version.current = next.stateVersion; setState(next)
  }
  return { state, error, reconcile, vote, setNotifications }
}
