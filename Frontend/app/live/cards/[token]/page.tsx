import type { Metadata } from "next"
import Link from "next/link"
import { ResultCard } from "@/components/result-cards/result-card"
import { ResultCardActions } from "@/components/result-cards/result-card-actions"
import { API_BASE_URL } from "@/lib/config"
import type { ApiResultCard } from "@/lib/api"

type PublicCardResult = { kind: "card"; card: ApiResultCard } | { kind: "expired" | "missing" }
async function getCard(token: string): Promise<PublicCardResult> {
  const response = await fetch(`${API_BASE_URL}/api/result-cards/public/${encodeURIComponent(token)}`, { next: { revalidate: 300 } })
  if(response.status===410) return {kind:"expired"}; if(!response.ok) return {kind:"missing"}
  return {kind:"card",card:await response.json() as ApiResultCard}
}
export async function generateMetadata({params}:{params:Promise<{token:string}>}):Promise<Metadata>{
  const result=await getCard((await params).token)
  if(result.kind!=="card") return {title:"PoP Live memory unavailable",description:"This shared PoP Live memory is no longer available.",robots:{index:false,follow:false}}
  const {card}=result, title=`PoP Live ${card.payload.mode} memory`
  return {title,description:card.payload.accessibleSummary,alternates:{canonical:card.publicUrl},robots:{index:false,follow:false},
    openGraph:{title,description:card.payload.accessibleSummary,images:[{url:card.imageUrl,width:1200,height:630,alt:card.payload.accessibleSummary}]},
    twitter:{card:"summary_large_image",title,description:card.payload.accessibleSummary,images:[card.imageUrl]}}
}
export default async function PublicResultCardPage({params}:{params:Promise<{token:string}>}){
  const result=await getCard((await params).token)
  if(result.kind!=="card") return <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center px-4 text-center"><h1 className="text-3xl font-bold">Memory unavailable</h1><p className="mt-3 text-muted-foreground">This link is expired, revoked, or invalid. No participant details are available.</p><Link className="mt-6 underline" href="/">Explore PoP</Link></main>
  const {card}=result
  return <main className="mx-auto min-h-screen max-w-xl px-4 py-12"><ResultCard card={card}/>{card.payload.state==="Active"?<div className="mt-6 text-center"><h1 className="text-2xl font-bold">You’re invited</h1><Link className="mt-4 inline-block rounded-md bg-primary px-5 py-3 text-primary-foreground" href="/game">Join PoP Live</Link></div>:<ResultCardActions card={card}/>}</main>
}
