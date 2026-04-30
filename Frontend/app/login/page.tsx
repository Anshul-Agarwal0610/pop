"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { motion } from "framer-motion"
import { AppLogo } from "@/components/app-shell/app-logo"
import { LoginForm } from "@/components/auth/login-form"
import { RegisterForm } from "@/components/auth/register-form"
import { GoogleButton } from "@/components/auth/google-button"
import { cn } from "@/lib/utils"

type Tab = "login" | "register"

export default function LoginPage() {
  const router = useRouter()
  const [tab, setTab] = useState<Tab>("login")

  function handleSuccess() {
    router.push("/")
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-12">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35 }}
        className="w-full max-w-md"
      >
        {/* Logo */}
        <div className="mb-8 flex justify-center">
          <AppLogo />
        </div>

        {/* Card */}
        <div className="rounded-2xl bg-card p-8 shadow-sm ring-1 ring-border/50">
          {/* Tab switcher */}
          <div className="mb-6 flex rounded-xl bg-secondary p-1">
            {(["login", "register"] as Tab[]).map((t) => (
              <button
                key={t}
                onClick={() => setTab(t)}
                className={cn(
                  "relative flex-1 rounded-lg py-2 text-sm font-medium capitalize transition-colors",
                  tab === t
                    ? "bg-background text-foreground shadow-sm"
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                {t === "login" ? "Sign In" : "Create Account"}
              </button>
            ))}
          </div>

          {/* Forms */}
          <motion.div
            key={tab}
            initial={{ opacity: 0, x: tab === "login" ? -10 : 10 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.2 }}
          >
            {tab === "login" ? (
              <LoginForm onSuccess={handleSuccess} />
            ) : (
              <RegisterForm onSuccess={handleSuccess} />
            )}
          </motion.div>

          {/* Divider */}
          <div className="my-6 flex items-center gap-3">
            <div className="h-px flex-1 bg-border" />
            <span className="text-xs text-muted-foreground">or</span>
            <div className="h-px flex-1 bg-border" />
          </div>

          {/* Google */}
          <GoogleButton onSuccess={handleSuccess} />
        </div>

        <p className="mt-6 text-center text-xs text-muted-foreground">
          By continuing you agree to Pollify&apos;s Terms of Service.
        </p>
      </motion.div>
    </div>
  )
}
