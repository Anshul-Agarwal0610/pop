"use client";import { QRCodeSVG } from "qrcode.react"
export function JoinCodeCard({code,url}:{code:string;url:string}){return <section className="rounded-xl border p-6 text-center"><QRCodeSVG className="mx-auto" value={url} size={180}/><p className="mt-4 text-sm">Scan or enter</p><strong className="font-mono text-4xl tracking-widest">{code}</strong></section>}
