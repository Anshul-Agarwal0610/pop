"use client"
import { useReducedMotion } from "framer-motion"
import { useCallback, useState } from "react"

export function useOptionalHaptics() {
  const reducedMotion = Boolean(useReducedMotion())
  const [enabled, setEnabled] = useState(true)
  const pulse = useCallback(() => {
    if (enabled && !reducedMotion && typeof navigator !== "undefined" && "vibrate" in navigator) return navigator.vibrate(18)
    return false
  }, [enabled, reducedMotion])
  return { enabled, setEnabled, pulse, supported: typeof navigator !== "undefined" && "vibrate" in navigator, reducedMotion }
}
