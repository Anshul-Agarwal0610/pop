"use client"

import { Share2 } from "lucide-react"
import type { MouseEvent } from "react"
import { Button } from "@/components/ui/button"
import { toast } from "@/hooks/use-toast"
import { cn } from "@/lib/utils"
import { appBaseUrl, isPollPubliclyShareable } from "@/lib/share"

interface ShareButtonProps {
  pollId: number | string
  title: string
  className?: string
  category?: string | null
  disabledReason?: string
  path?: string
  text?: string
  variant?: "default" | "secondary" | "ghost" | "outline"
}

function showShareToast(title: "Link copied!" | "Shared!") {
  const { dismiss } = toast({ title })
  window.setTimeout(dismiss, 2000)
}

export function ShareButton({
  className,
  category,
  disabledReason,
  path,
  pollId,
  text,
  title,
  variant = "secondary",
}: ShareButtonProps) {
  async function sharePoll(event: MouseEvent<HTMLButtonElement>) {
    event.stopPropagation()

    const url = `${appBaseUrl()}${path ?? `/polls/${pollId}`}`
    const shareData = {
      title,
      text: text ?? title,
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

  if (!isPollPubliclyShareable(category)) {
    return null
  }

  return (
    <Button
      aria-label={disabledReason ?? "Share poll"}
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
