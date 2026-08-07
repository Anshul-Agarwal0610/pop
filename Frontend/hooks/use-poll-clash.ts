"use client"
import { useCallback, useEffect, useState } from "react"
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr"
import { pollClashesApi, type ApiPollClash } from "@/lib/api"
import { getToken } from "@/lib/auth"
import { API_BASE_URL } from "@/lib/config"

export function usePollClash(clashId: number) {
  const [clash,setClash]=useState<ApiPollClash|null>(null); const [loading,setLoading]=useState(true); const [error,setError]=useState<string|null>(null)
  const refetch=useCallback(async()=>{try{setClash(await pollClashesApi.get(clashId));setError(null)}catch(e){setError(e instanceof Error?e.message:"Could not load Clash")}finally{setLoading(false)}},[clashId])
  useEffect(()=>{const timer=window.setTimeout(()=>void refetch(),0);const onVisible=()=>{if(document.visibilityState==="visible")void refetch()};document.addEventListener("visibilitychange",onVisible);return()=>{window.clearTimeout(timer);document.removeEventListener("visibilitychange",onVisible)}},[refetch])
  useEffect(()=>{const token=getToken();if(!token)return;const connection=new HubConnectionBuilder().withUrl(`${API_BASE_URL}/hubs/poll-clashes`,{accessTokenFactory:()=>token}).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();connection.on("stateChanged",()=>void refetch());connection.onreconnected(()=>{void connection.invoke("Watch",clashId);void refetch()});void connection.start().then(()=>connection.invoke("Watch",clashId)).catch(()=>{/* REST and visibility refetch remain the recovery path. */});return()=>{void connection.stop()}},[clashId,refetch])
  return {clash,loading,error,refetch,setClash}
}
