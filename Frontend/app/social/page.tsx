"use client"

import { FormEvent, useCallback, useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { AppShell } from "@/components/app-shell"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useAuth } from "@/contexts/auth-context"
import { ApiError, socialApi, type FriendConnection, type SocialGroup, type SocialUser } from "@/lib/api"

export default function SocialPage() {
  const { isAuthenticated, isLoading } = useAuth(); const router = useRouter()
  const [friends,setFriends]=useState<FriendConnection[]>([]),[groups,setGroups]=useState<SocialGroup[]>([]),[results,setResults]=useState<SocialUser[]>([])
  const [query,setQuery]=useState(""),[groupName,setGroupName]=useState(""),[busy,setBusy]=useState<string|null>(null),[message,setMessage]=useState("")
  const [inviteToken,setInviteToken]=useState("")
  const refresh=useCallback(async()=>{const [f,g]=await Promise.all([socialApi.friends(),socialApi.groups()]);setFriends(f.items);setGroups(g.items)},[])
  useEffect(()=>{if(!isLoading&&!isAuthenticated)router.replace("/login");else if(isAuthenticated)void Promise.resolve().then(refresh).catch(e=>setMessage(e.message))},[isLoading,isAuthenticated,router,refresh])
  const act=async(key:string,fn:()=>Promise<unknown>)=>{if(busy)return;setBusy(key);setMessage("");try{await fn();await refresh()}catch(e){setMessage(e instanceof ApiError&&e.status===429?"Invitation limit reached. Try again later.":(e as Error).message)}finally{setBusy(null)}}
  const search=async(e:FormEvent)=>{e.preventDefault();if(query.trim().length<2)return;setResults((await socialApi.searchUsers(query)).items)}
  if(isLoading||!isAuthenticated)return null
  return <AppShell><main className="mx-auto max-w-3xl space-y-5 px-4 py-6"><div><h1 className="text-2xl font-bold">Friends & groups</h1><p className="text-sm text-muted-foreground">Everything here is opt-in. Private and wellness activity never contributes to social rankings.</p></div>{message&&<p role="alert" className="rounded-lg bg-muted p-3 text-sm">{message}</p>}
    <Tabs defaultValue="friends"><TabsList><TabsTrigger value="friends">Friends</TabsTrigger><TabsTrigger value="groups">Private groups</TabsTrigger></TabsList>
      <TabsContent value="friends" className="space-y-4"><form className="flex gap-2" onSubmit={search}><Input aria-label="Search users" value={query} onChange={e=>setQuery(e.target.value)} placeholder="Search username or display name"/><Button type="submit">Search</Button></form>
        {results.map(u=><div className="flex items-center justify-between rounded-xl border p-3" key={u.id}><span><b>{u.displayName}</b> <span className="text-muted-foreground">@{u.username}</span></span><Button disabled={!!busy} onClick={()=>act(`add-${u.id}`,()=>socialApi.sendFriendRequest(u.id))}>Add friend</Button></div>)}
        {friends.length===0&&<p className="rounded-xl border p-6 text-center text-muted-foreground">No connections yet.</p>}{friends.map(f=><div className="flex items-center gap-2 rounded-xl border p-3" key={f.id}><span className="flex-1"><b>{f.user.displayName}</b><small className="ml-2 text-muted-foreground">{f.state}</small></span>{f.state==="Pending"&&f.incoming&&<><Button disabled={!!busy} onClick={()=>act(`accept-${f.id}`,()=>socialApi.acceptFriendRequest(f.id))}>Accept</Button><Button variant="outline" disabled={!!busy} onClick={()=>act(`decline-${f.id}`,()=>socialApi.declineFriendRequest(f.id))}>Decline</Button></>}{f.state==="Pending"&&!f.incoming&&<Button variant="outline" disabled={!!busy} onClick={()=>act(`cancel-${f.id}`,()=>socialApi.removeFriend(f.user.id))}>Cancel request</Button>}{f.state==="Accepted"&&<Button variant="outline" disabled={!!busy} onClick={()=>act(`remove-${f.id}`,()=>socialApi.removeFriend(f.user.id))}>Remove</Button>}<Button variant="destructive" disabled={!!busy} onClick={()=>confirm(`Block ${f.user.displayName}?`)&&act(`block-${f.id}`,()=>socialApi.block(f.user.id))}>Block</Button></div>)}</TabsContent>
      <TabsContent value="groups" className="space-y-4"><form className="flex gap-2" onSubmit={e=>{e.preventDefault();act("create",()=>socialApi.createGroup(groupName).then(()=>setGroupName("")))}}><Input aria-label="Group name" value={groupName} onChange={e=>setGroupName(e.target.value)} placeholder="New private group"/><Button disabled={!!busy}>Create</Button></form><form className="flex gap-2" onSubmit={e=>{e.preventDefault();act("join",()=>socialApi.acceptInvite(inviteToken).then(()=>setInviteToken("")))}}><Input aria-label="Invite token" value={inviteToken} onChange={e=>setInviteToken(e.target.value)} placeholder="Paste a private invite token"/><Button disabled={!!busy||!inviteToken}>Join</Button><Button type="button" variant="outline" disabled={!!busy||!inviteToken} onClick={()=>act("decline-invite",()=>socialApi.declineInvite(inviteToken).then(()=>setInviteToken("")))}>Decline</Button></form>{groups.length===0&&<p className="rounded-xl border p-6 text-center text-muted-foreground">You have not joined a private group.</p>}{groups.map(g=><div className="flex items-center justify-between rounded-xl border p-4" key={g.id}><span><b>{g.name}</b><small className="ml-2 text-muted-foreground">{g.memberCount} members · {g.role}</small></span>{g.role!=="Owner"&&<Button variant="outline" disabled={!!busy} onClick={()=>confirm(`Leave ${g.name}?`)&&act(`leave-${g.id}`,()=>socialApi.leaveGroup(g.id))}>Leave</Button>}</div>)}</TabsContent>
    </Tabs></main></AppShell>
}
