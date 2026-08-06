import { RelayHandoffCard } from "@/components/relay/relay-handoff-card"

export default async function RelayPage({params}:{params:Promise<{token:string}>}) {
  const {token}=await params
  return <main className="min-h-[calc(100vh-5rem)] px-4 py-8"><RelayHandoffCard token={token}/></main>
}
