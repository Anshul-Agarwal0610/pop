"use client"

import { FormEvent, useEffect, useState } from "react"
import Link from "next/link"
import { useRouter } from "next/navigation"
import { BriefcaseBusiness, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/contexts/auth-context"
import { apiBusinessLogin, apiBusinessRegister } from "@/lib/auth"
import { cn } from "@/lib/utils"

type Mode = "login" | "register"
type Plan = "Starter" | "Growth" | "Pro"

export default function BusinessLoginPage() {
  const router = useRouter(); const { user, isLoading, login } = useAuth()
  const [mode, setMode] = useState<Mode>("login"); const [busy, setBusy] = useState(false); const [error, setError] = useState("")
  const [username, setUsername] = useState(""); const [displayName, setDisplayName] = useState(""); const [password, setPassword] = useState(""); const [confirmPassword, setConfirmPassword] = useState("")
  const [businessName, setBusinessName] = useState(""); const [websiteUrl, setWebsiteUrl] = useState(""); const [plan, setPlan] = useState<Plan>("Starter")
  const [acceptedTerms, setAcceptedTerms] = useState(false)
  useEffect(() => { if (!isLoading && user?.accountType === "Business") router.replace("/business") }, [isLoading, router, user?.accountType])
  async function submit(event: FormEvent) {
    event.preventDefault(); setError("")
    if (mode === "register" && password !== confirmPassword) { setError("Passwords do not match."); return }
    if (mode === "register" && !acceptedTerms) { setError("Accept the Terms of Use and Community Guidelines to continue."); return }
    setBusy(true)
    try {
      const result = mode === "login"
        ? await apiBusinessLogin(username.trim().toLowerCase(), password)
        : await apiBusinessRegister({ username: username.trim().toLowerCase(), displayName: displayName.trim(), password, confirmPassword, businessName: businessName.trim(), websiteUrl: websiteUrl.trim() || undefined, subscriptionPlan: plan, acceptTerms: true })
      login(result); router.replace("/business")
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Business authentication failed") }
    finally { setBusy(false) }
  }
  return <main className="min-h-screen bg-background px-4 py-12">
    <div className="mx-auto grid max-w-5xl overflow-hidden rounded-[2rem] border border-border bg-card shadow-xl lg:grid-cols-[1.05fr_.95fr]">
      <section className="hidden bg-foreground p-12 text-background lg:flex lg:flex-col lg:justify-between">
        <div className="flex items-center gap-3 text-xl font-bold"><span className="rounded-xl bg-primary p-2 text-primary-foreground"><BriefcaseBusiness /></span>Pop Business</div>
        <div><p className="mb-4 text-xs font-bold uppercase tracking-[.22em] text-primary">Business workspace</p><h1 className="text-5xl font-black leading-[1.04]">Turn questions into decisions.</h1><p className="mt-5 max-w-md text-lg opacity-70">Create branded campaigns, sponsor polls and understand how audiences respond.</p></div>
        <p className="text-sm opacity-60">Business identities remain separate from personal Pop accounts.</p>
      </section>
      <section className="p-6 sm:p-10 lg:p-12">
        <div className="mb-8 lg:hidden"><p className="flex items-center gap-2 text-xl font-bold"><BriefcaseBusiness className="text-primary" /> Pop Business</p></div>
        <div className="mb-7 flex rounded-xl bg-secondary p-1">{(["login","register"] as Mode[]).map(item => <button key={item} type="button" onClick={() => { setMode(item); setError("") }} className={cn("flex-1 rounded-lg px-3 py-2 text-sm font-semibold capitalize", mode === item && "bg-background shadow-sm")}>{item === "login" ? "Sign in" : "Create account"}</button>)}</div>
        <h2 className="text-2xl font-bold">{mode === "login" ? "Welcome back" : "Set up your business"}</h2><p className="mt-1 text-sm text-muted-foreground">{mode === "login" ? "Sign in with your business credentials." : "Your 14-day trial starts after registration."}</p>
        <form onSubmit={submit} className="mt-7 space-y-4">
          {mode === "register" && <><Field label="Your name"><Input required value={displayName} onChange={e=>setDisplayName(e.target.value)} /></Field><Field label="Business name"><Input required value={businessName} onChange={e=>setBusinessName(e.target.value)} /></Field><Field label="Website (optional)"><Input type="url" placeholder="https://" value={websiteUrl} onChange={e=>setWebsiteUrl(e.target.value)} /></Field></>}
          <Field label="Username"><Input required autoComplete="username" value={username} onChange={e=>setUsername(e.target.value.replace(/\s/g,"_"))} /></Field>
          <Field label="Password"><Input required minLength={6} type="password" autoComplete={mode === "login" ? "current-password" : "new-password"} value={password} onChange={e=>setPassword(e.target.value)} /></Field>
          {mode === "register" && <><Field label="Confirm password"><Input required minLength={6} type="password" value={confirmPassword} onChange={e=>setConfirmPassword(e.target.value)} /></Field><div><Label>Subscription plan</Label><div className="mt-2 grid grid-cols-3 gap-2">{(["Starter","Growth","Pro"] as Plan[]).map(item=><button type="button" key={item} onClick={()=>setPlan(item)} className={cn("rounded-xl border border-border p-3 text-sm font-semibold", plan===item&&"border-primary bg-primary/10 text-primary")}>{item}</button>)}</div></div><label className="flex items-start gap-3 text-sm leading-5 text-muted-foreground"><input type="checkbox" checked={acceptedTerms} onChange={event=>setAcceptedTerms(event.target.checked)} className="mt-1 h-4 w-4 accent-primary"/><span>I agree to the <Link href="/terms" className="font-semibold text-primary hover:underline">Terms of Use and Community Guidelines</Link> and acknowledge the <Link href="/privacy" className="font-semibold text-primary hover:underline">Privacy Policy</Link>.</span></label></>}
          {error && <p className="rounded-xl bg-destructive/10 p-3 text-sm text-destructive">{error}</p>}
          <Button disabled={busy} className="w-full">{busy && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}{mode === "login" ? "Sign in to business" : "Create business account"}</Button>
        </form>
        <p className="mt-6 text-center text-sm text-muted-foreground">Looking for personal Pop? <Link className="font-semibold text-primary hover:underline" href="/login">Personal sign in</Link></p>
      </section>
    </div>
  </main>
}

function Field({ label, children }: { label: string; children: React.ReactNode }) { return <div className="space-y-2"><Label>{label}</Label>{children}</div> }
