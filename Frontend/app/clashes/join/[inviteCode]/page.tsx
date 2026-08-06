"use client"
import { use, useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { pollClashesApi, type ApiPollClash } from "@/lib/api"
export default function JoinClashPage({params}:{params:Promise<{inviteCode:string}>}){const {inviteCode}=use(params);const router=useRouter();const [clash,setClash]=useState<ApiPollClash|null>(null);const [error,setError]=useState<string|null>(null);useEffect(()=>{pollClashesApi.invite(inviteCode).then(setClash).catch(e=>setError(e.message))},[inviteCode]);return <AppShell><main className="mx-auto max-w-lg px-4 py-12 text-center"><h1 className="text-3xl font-black">Join Poll Clash</h1><p className="mt-2 text-muted-foreground">A short private prediction game. Opinions are never ranked as correct.</p>{error&&<p role="alert" className="mt-6 text-destructive">{error}</p>}{clash&&<><p className="mt-6">{clash.roundCount} round{clash.roundCount===1?"":"s"}</p><Button className="mt-4" onClick={async()=>{const joined=await pollClashesApi.join(clash.id);router.push(`/clashes/${joined.id}`)}}>Join Clash</Button></>}</main></AppShell>}
