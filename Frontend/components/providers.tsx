"use client"

import { Suspense } from "react"
import { GoogleOAuthProvider } from "@react-oauth/google"
import { AuthProvider } from "@/contexts/auth-context"
import { ThemeProvider } from "@/components/theme-provider"
import { Toaster } from "@/components/ui/toaster"
import { AchievementCelebrationProvider } from "@/components/achievements/celebration-provider"
import { AuthRouteGuard } from "@/components/auth/auth-route-guard"
import { AnalyticsProvider } from "@/lib/analytics/provider"

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <GoogleOAuthProvider
      clientId={process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? ""}
    >
      <AuthProvider>
        <AnalyticsProvider>
        <ThemeProvider
          attribute="class"
          defaultTheme="system"
          enableSystem
          disableTransitionOnChange
        >
          <Suspense fallback={null}>
            <AuthRouteGuard><AchievementCelebrationProvider>{children}</AchievementCelebrationProvider></AuthRouteGuard>
          </Suspense>
          <Toaster />
        </ThemeProvider>
        </AnalyticsProvider>
      </AuthProvider>
    </GoogleOAuthProvider>
  )
}
