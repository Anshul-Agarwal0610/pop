"use client"

import { useState } from "react"
import { useGoogleLogin } from "@react-oauth/google"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useAuth } from "@/contexts/auth-context"
import { apiGoogleLogin } from "@/lib/auth"

// Inline Google SVG icon
function GoogleIcon() {
  return (
    <svg className="h-4 w-4" viewBox="0 0 24 24" aria-hidden="true">
      <path
        d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
        fill="#4285F4"
      />
      <path
        d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
        fill="#34A853"
      />
      <path
        d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z"
        fill="#FBBC05"
      />
      <path
        d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
        fill="#EA4335"
      />
    </svg>
  )
}

export function GoogleButton({ onSuccess }: { onSuccess: () => void }) {
  const { login } = useAuth()
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState("")

  const googleLogin = useGoogleLogin({
    onSuccess: async (tokenResponse) => {
      setLoading(true)
      setError("")
      try {
        // Exchange Google access token for user info, then send to backend
        const userInfo = await fetch("https://www.googleapis.com/oauth2/v3/userinfo", {
          headers: { Authorization: `Bearer ${tokenResponse.access_token}` },
        }).then((r) => r.json())

        // For the backend we need an ID token — use the credential flow instead
        // This path uses access_token to fetch the user info directly
        const data = await apiGoogleLogin(tokenResponse.access_token)
        login(data)
        onSuccess()
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Google sign-in failed")
      } finally {
        setLoading(false)
      }
    },
    onError: () => setError("Google sign-in was cancelled or failed."),
    flow: "implicit",
  })

  return (
    <div className="space-y-2">
      <Button
        type="button"
        variant="outline"
        className="w-full gap-2"
        onClick={() => googleLogin()}
        disabled={loading}
      >
        {loading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <GoogleIcon />
        )}
        Continue with Google
      </Button>
      {error && <p className="text-center text-sm text-destructive">{error}</p>}
    </div>
  )
}
