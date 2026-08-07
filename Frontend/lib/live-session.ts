import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr"
import { API_BASE_URL } from "@/lib/config"
import { getToken } from "@/lib/auth"

export interface LiveSessionEvent {
  type: "participantJoined" | "participantLeft" | "participantReadyChanged" | "voteLockChanged" |
    "roundRevealScheduled" | "roundRevealed" | "sessionCompleted" | "stateChanged"
  sessionId: string
  stateVersion: number
  serverNow: string
  revealAt?: string | null
}

export function createLiveSessionConnection() {
  return new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/live-sessions`, { accessTokenFactory: () => getToken() ?? "" })
    .withAutomaticReconnect([0, 1_000, 3_000, 10_000])
    .configureLogging(LogLevel.Warning)
    .build()
}

export function serverClockOffset(serverNow: string, receivedClientNow = Date.now()) {
  return new Date(serverNow).getTime() - receivedClientNow
}

export function millisecondsUntil(revealAt: string, offsetMs: number, clientNow = Date.now()) {
  return Math.max(0, new Date(revealAt).getTime() - (clientNow + offsetMs))
}

export { HubConnectionState }
