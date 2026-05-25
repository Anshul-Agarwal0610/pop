"use client"

import { Share2 } from "lucide-react"
import type { MouseEvent } from "react"
import { Button } from "@/components/ui/button"
import { toast } from "@/hooks/use-toast"
import { cn } from "@/lib/utils"

interface ShareButtonProps {
  pollId: number | string
  title: string
  className?: string
  variant?: "default" | "secondary" | "ghost" | "outline"
}

function appBaseUrl() {
  const configured = process.env.NEXT_PUBLIC_BASE_URL
  if (configured) return configured.replace(/\/+$/, "")

  if (typeof window !== "undefined") {
    return window.location.origin
  }

  return ""
}

function showShareToast(title: "Link copied!" | "Shared!") {
  const { dismiss } = toast({ title })
  window.setTimeout(dismiss, 2000)
}

export function ShareButton({
  className,
  pollId,
  title,
  variant = "secondary",
}: ShareButtonProps) {
  async function sharePoll(event: MouseEvent<HTMLButtonElement>) {
    event.stopPropagation()

    const url = `${appBaseUrl()}/polls/${pollId}`
    const shareData = {
      title,
      text: title,
      url,
    }

    try {
      if (navigator.share && navigator.canShare?.(shareData) !== false) {
        await navigator.share(shareData)
        showShareToast("Shared!")
        return
      }

      await navigator.clipboard.writeText(url)
      showShareToast("Link copied!")
    } catch (error) {
      if ((error as DOMException).name === "AbortError") return

      await navigator.clipboard.writeText(url)
      showShareToast("Link copied!")
    }
  }

  return (
    <Button
      aria-label="Share poll"
      className={cn("gap-2", className)}
      onClick={sharePoll}
      onPointerDown={(event) => event.stopPropagation()}
      size="sm"
      type="button"
      variant={variant}
    >
      <Share2 className="h-4 w-4" />
      <span className="sr-only sm:not-sr-only">Share</span>
    </Button>
  )
}
