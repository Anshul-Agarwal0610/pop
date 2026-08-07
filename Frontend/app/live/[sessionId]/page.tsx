import { AppShell } from "@/components/app-shell"
import { PollBombRoom } from "@/components/poll-bomb/poll-bomb-room"
export default async function Page({params}:{params:Promise<{sessionId:string}>}){const {sessionId}=await params;return <AppShell><main className="mx-auto max-w-xl px-4 py-8"><PollBombRoom publicId={sessionId}/></main></AppShell>}
