"use client"
import { useCallback, useEffect, useRef, useState } from "react"
import { HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr"
import { API_BASE_URL } from "@/lib/config"

export function useLiveRoom<T extends {version:number}>(roomId:string,audience:"host"|"participants"|"display",load:()=>Promise<T>) {
  const [snapshot,setSnapshot]=useState<T|null>(null); const [connected,setConnected]=useState(false)
  const version=useRef(0)
  const refresh=useCallback(async()=>{const next=await load();if(next.version>=version.current){version.current=next.version;setSnapshot(next)}},[load])
  useEffect(()=>{void refresh();const connection=new HubConnectionBuilder().withUrl(`${API_BASE_URL}/hubs/live-rooms`).withAutomaticReconnect().build()
    connection.on("roomChanged",(next:number)=>{if(next>version.current)void refresh()});connection.onreconnecting(()=>setConnected(false));connection.onreconnected(()=>{setConnected(true);void refresh()})
    void connection.start().then(()=>{setConnected(true);return connection.invoke("Watch",roomId,audience)}).catch(()=>setConnected(false))
    return()=>{if(connection.state!==HubConnectionState.Disconnected)void connection.stop()}
  },[audience,refresh,roomId]);return{snapshot,connected,refresh}
}
