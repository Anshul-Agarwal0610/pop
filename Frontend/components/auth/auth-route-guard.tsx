"use client"

import { useEffect, type ReactNode } from "react"
import Link from "next/link"
import { usePathname, useRouter, useSearchParams } from "next/navigation"
import { Loader2 } from "lucide-react"
import { useAuth } from "@/contexts/auth-context"
import { Button } from "@/components/ui/button"

export function isPublicRoute(pathname: string) {
  return pathname === "/" || pathname === "/login"
}

export function AuthRouteGuard({ children }: { children: ReactNode }) {
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const router = useRouter()
  const { isAuthenticated, isLoading } = useAuth()
  const isPublic = isPublicRoute(pathname)
  const query = searchParams.toString()
  const destination = `${pathname}${query ? `?${query}` : ""}`

  useEffect(() => {
    if (!isPublic && !isLoading && !isAuthenticated) {
      const params = new URLSearchParams({
        message: "Sign in to continue",
        redirect: destination,
      })
      router.replace(`/login?${params.toString()}`)
    }
  }, [destination, isAuthenticated, isLoading, isPublic, router])

  if (isPublic) return children
  if (!isLoading && isAuthenticated) return children

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="text-center" role="status" aria-live="polite">
        {isLoading ? (
          <>
            <Loader2 className="mx-auto h-7 w-7 animate-spin text-primary" />
            <p className="mt-3 text-sm text-muted-foreground">Checking your session…</p>
          </>
        ) : (
          <>
            <h1 className="text-xl font-semibold">Sign in required</h1>
            <p className="mt-2 text-sm text-muted-foreground">Sign in to view this page.</p>
            <Button asChild className="mt-4">
              <Link href={`/login?redirect=${encodeURIComponent(destination)}`}>Sign In</Link>
            </Button>
          </>
        )}
      </div>
    </main>
  )
}
