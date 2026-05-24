"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { PollCreator } from "@/components/poll-create"
import { useAuth } from "@/contexts/auth-context"

export default function CreatePollPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading } = useAuth()

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace("/login")
    }
  }, [isAuthenticated, isLoading, router])

  if (isLoading || !isAuthenticated) return null

  return <PollCreator />
}
