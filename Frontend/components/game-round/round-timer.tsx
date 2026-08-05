"use client"
import { useEffect, useRef, useState } from "react"

export function RoundTimer({ expiresAt, serverNow, onExpire }: { expiresAt: string | null; serverNow: string; onExpire: () => void }) {
  const offset=useRef(Date.parse(serverNow)-Date.now()); const seconds=()=>expiresAt?Math.max(0,Math.ceil((Date.parse(expiresAt)-(Date.now()+offset.current))/1000)):null
  const [left,setLeft]=useState(seconds)
  useEffect(()=>{if(!expiresAt)return;const timer=setInterval(()=>{const next=seconds();setLeft(next);if(next===0){clearInterval(timer);onExpire()}},1000);return()=>clearInterval(timer)},[expiresAt,onExpire])
  if(left===null)return <p className="text-sm font-semibold">Untimed round</p>
  const announce=[60,30,10,5,0].includes(left)
  return <p className="text-sm font-bold" aria-live={announce?"polite":"off"}>Time remaining: {Math.floor(left/60)}:{String(left%60).padStart(2,"0")}</p>
}
