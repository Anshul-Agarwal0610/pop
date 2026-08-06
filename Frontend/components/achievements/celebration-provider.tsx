"use client"
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { useAuth } from "@/contexts/auth-context"
import { achievementsApi, type ApiUserBadge } from "@/lib/api"

const Context = createContext<{ enqueue: (items: ApiUserBadge[]) => void }>({ enqueue: () => undefined })
export const useAchievementCelebrations = () => useContext(Context)

export function AchievementCelebrationProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth(); const [queue,setQueue]=useState<ApiUserBadge[]>([]); const seen=useRef(new Set<number>())
  const enqueue=useCallback((items:ApiUserBadge[]) => setQueue(q => [...q,...items.filter(x => !seen.current.has(x.id) && !!seen.current.add(x.id))]),[])
  useEffect(() => { if (!isLoading && isAuthenticated) achievementsApi.claimCelebrations().then(enqueue).catch(() => undefined) },[isAuthenticated,isLoading,enqueue])
  const active=queue[0]
  return <Context.Provider value={{enqueue}}>{children}<Dialog open={!!active} onOpenChange={open => { if(!open) setQueue(q=>q.slice(1)) }}>
    <DialogContent><DialogHeader><DialogTitle>Achievement unlocked</DialogTitle><DialogDescription>{active?.name}</DialogDescription></DialogHeader>
      {active && <div><p>{active.description}</p><p className="mt-2 font-medium">Reward: {active.rewardXp} XP{active.rewardTitle ? ` + ${active.rewardTitle} title` : ""}</p></div>}
    </DialogContent></Dialog></Context.Provider>
}
