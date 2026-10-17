"use client"

import { useState } from "react"
import { motion } from "framer-motion"
import { Eye, EyeOff, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useAuth } from "@/contexts/auth-context"
import { apiRegister } from "@/lib/auth"
import Link from "next/link"

export function RegisterForm({ onSuccess }: { onSuccess: () => void }) {
  const { login } = useAuth()
  const [displayName, setDisplayName] = useState("")
  const [username,    setUsername]    = useState("")
  const [password,    setPassword]    = useState("")
  const [confirm,     setConfirm]     = useState("")
  const [showPw,      setShowPw]      = useState(false)
  const [error,       setError]       = useState("")
  const [loading,     setLoading]     = useState(false)
  const [acceptedTerms, setAcceptedTerms] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError("")
    if (password !== confirm) { setError("Passwords do not match."); return }
    if (password.length < 6)  { setError("Password must be at least 6 characters."); return }
    if (!acceptedTerms) { setError("Accept the Terms of Use and Community Guidelines to continue."); return }
    setLoading(true)
    try {
      const data = await apiRegister(username, displayName, password, confirm, true)
      login(data)
      onSuccess()
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Registration failed")
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="displayName">Display Name</Label>
        <Input
          id="displayName"
          placeholder="Jane Doe"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="reg-username">Username</Label>
        <Input
          id="reg-username"
          placeholder="jane_doe"
          value={username}
          onChange={(e) => setUsername(e.target.value.toLowerCase().replace(/\s/g, "_"))}
          required
          autoComplete="username"
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="reg-password">Password</Label>
        <div className="relative">
          <Input
            id="reg-password"
            type={showPw ? "text" : "password"}
            placeholder="min. 6 characters"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="new-password"
            className="pr-10"
          />
          <button
            type="button"
            onClick={() => setShowPw((v) => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          >
            {showPw ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
          </button>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="confirm">Confirm Password</Label>
        <Input
          id="confirm"
          type="password"
          placeholder="••••••••"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          required
          autoComplete="new-password"
        />
      </div>

      <label className="flex items-start gap-3 text-sm leading-5 text-muted-foreground">
        <input type="checkbox" checked={acceptedTerms} onChange={event => setAcceptedTerms(event.target.checked)} className="mt-1 h-4 w-4 accent-primary" />
        <span>I agree to the <Link href="/terms" className="font-semibold text-primary hover:underline">Terms of Use and Community Guidelines</Link> and acknowledge the <Link href="/privacy" className="font-semibold text-primary hover:underline">Privacy Policy</Link>.</span>
      </label>

      {error && (
        <motion.p
          initial={{ opacity: 0, y: -4 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-sm text-destructive"
        >
          {error}
        </motion.p>
      )}

      <Button type="submit" className="w-full" disabled={loading}>
        {loading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
        Create Account
      </Button>
    </form>
  )
}
