"use client"
import { useParams } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { GameRound } from "@/components/game-round/game-round"
export default function GameSessionPage(){const {sessionId}=useParams<{sessionId:string}>();return <AppShell><GameRound id={sessionId}/></AppShell>}
