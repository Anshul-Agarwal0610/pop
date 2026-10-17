"use client"

import { useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { Loader2, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { usersApi } from "@/lib/api"

export default function DeleteAccountPage() {
  const router = useRouter()
  const { user, isLoading, isAuthenticated, logout } = useAuth()
  const [confirmed, setConfirmed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState("")

  async function deleteAccount() {
    if (!confirmed) return
    setBusy(true)
    setError("")
    try {
      await usersApi.deleteAccount()
      logout()
      router.replace("/login?message=Your+Pop+account+has+been+deleted")
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Account deletion failed")
      setBusy(false)
    }
  }

  return <main className="min-h-dvh bg-background px-5 py-10 sm:py-16">
    <article className="mx-auto max-w-2xl space-y-8 rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-10">
      <header className="space-y-3 border-b border-border pb-7">
        <Link href="/privacy" className="text-sm font-semibold text-primary hover:underline">← Privacy Policy</Link>
        <div className="flex items-center gap-3"><Trash2 className="h-7 w-7 text-destructive"/><h1 className="text-3xl font-bold tracking-tight">Delete your Pop account</h1></div>
        <p className="leading-7 text-muted-foreground">This page is the public web deletion resource for the Pop Android app.</p>
      </header>

      <section className="space-y-3">
        <h2 className="text-xl font-semibold">What is deleted</h2>
        <p className="leading-7 text-muted-foreground">Deletion removes your credentials, profile details, private wellness history, category and notification preferences, device tokens, and social connections. You are signed out immediately.</p>
        <p className="leading-7 text-muted-foreground">Public polls and votes may remain in anonymized form so published discussions and aggregate results remain accurate. They will no longer identify you. Limited security logs and encrypted backups may remain for their normal retention period.</p>
      </section>

      {isLoading ? <div className="flex items-center gap-2 text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin"/>Checking your account…</div> : !isAuthenticated ? (
        <section className="space-y-4 rounded-2xl border border-border bg-secondary/40 p-5">
          <h2 className="font-semibold">Sign in to verify your identity</h2>
          <p className="text-sm leading-6 text-muted-foreground">You can initiate deletion on the web by signing in to the Pop account you want removed.</p>
          <Button asChild><Link href="/login?redirect=%2Fdelete-account">Sign in to delete account</Link></Button>
        </section>
      ) : (
        <section className="space-y-4 rounded-2xl border border-destructive/30 bg-destructive/5 p-5">
          <p className="font-semibold">Signed in as {user?.displayName} (@{user?.username})</p>
          <label className="flex items-start gap-3 text-sm leading-6 text-muted-foreground">
            <input type="checkbox" checked={confirmed} onChange={event => setConfirmed(event.target.checked)} className="mt-1 h-4 w-4 accent-destructive" />
            <span>I understand this permanently disables my account and removes the associated personal data described above.</span>
          </label>
          {error && <p className="rounded-xl bg-destructive/10 p-3 text-sm text-destructive">{error}</p>}
          <Button variant="destructive" disabled={!confirmed || busy} onClick={() => void deleteAccount()}>{busy && <Loader2 className="mr-2 h-4 w-4 animate-spin"/>}Permanently delete my account</Button>
        </section>
      )}

      <p className="text-sm text-muted-foreground">You can also delete your account inside the Pop app under More → Preferences → Your data.</p>
    </article>
  </main>
}
