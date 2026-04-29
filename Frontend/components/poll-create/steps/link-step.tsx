"use client"

import { useState, useEffect } from "react"
import { motion, AnimatePresence } from "framer-motion"
import { Link2, Youtube, Instagram, Newspaper, ExternalLink, X, SkipForward, Check, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import type { ExternalLink as ExternalLinkType } from "@/lib/poll-creation"
import { detectPlatform } from "@/lib/poll-creation"

interface LinkStepProps {
  value: ExternalLinkType | null
  onChange: (value: ExternalLinkType | null) => void
  onSkip: () => void
}

// X/Twitter icon
function XIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor">
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
    </svg>
  )
}

const PLATFORM_ICONS = {
  youtube: Youtube,
  instagram: Instagram,
  twitter: XIcon,
  news: Newspaper,
  other: ExternalLink,
}

const PLATFORM_STYLES = {
  youtube: "text-red-500 bg-red-500/10 ring-red-500/20",
  instagram: "text-pink-500 bg-pink-500/10 ring-pink-500/20",
  twitter: "text-foreground bg-foreground/10 ring-foreground/20",
  news: "text-blue-500 bg-blue-500/10 ring-blue-500/20",
  other: "text-muted-foreground bg-muted ring-border",
}

export function LinkStep({ value, onChange, onSkip }: LinkStepProps) {
  const [url, setUrl] = useState(value?.url || "")
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (value?.url) {
      setUrl(value.url)
    }
  }, [value])

  const handleUrlSubmit = async () => {
    if (!url.trim()) {
      setError("Please enter a URL")
      return
    }

    // Basic URL validation
    try {
      new URL(url)
    } catch {
      setError("Please enter a valid URL")
      return
    }

    setIsLoading(true)
    setError(null)

    // Simulate fetching link preview
    await new Promise((resolve) => setTimeout(resolve, 800))

    const platform = detectPlatform(url)
    onChange({
      url,
      platform,
      title: "Link preview title would appear here",
      description: "This is where the link description would be shown after fetching metadata.",
      thumbnail: "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=400&h=300&fit=crop",
    })

    setIsLoading(false)
  }

  const handleRemove = () => {
    onChange(null)
    setUrl("")
  }

  const Icon = value ? PLATFORM_ICONS[value.platform] : Link2
  const platformStyle = value ? PLATFORM_STYLES[value.platform] : ""

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      className="flex flex-col gap-6"
    >
      {/* Header */}
      <div className="text-center">
        <motion.div
          className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-blue-500/10"
          initial={{ scale: 0, rotate: -180 }}
          animate={{ scale: 1, rotate: 0 }}
          transition={{ type: "spring", stiffness: 200, damping: 15 }}
        >
          <Link2 className="h-8 w-8 text-blue-500" />
        </motion.div>
        <h2 className="text-2xl font-bold text-foreground">
          Add external source
        </h2>
        <p className="mt-2 text-muted-foreground">
          Link to YouTube, news article, or social post (optional)
        </p>
      </div>

      <AnimatePresence mode="wait">
        {value ? (
          /* Link Preview */
          <motion.div
            key="preview"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="overflow-hidden rounded-2xl bg-card ring-2 ring-primary/20"
          >
            {value.thumbnail && (
              <img
                src={value.thumbnail}
                alt=""
                className="h-40 w-full object-cover"
              />
            )}
            <div className="p-4">
              <div className="mb-3 flex items-center gap-2">
                <div className={cn("rounded-lg p-1.5 ring-1", platformStyle)}>
                  <Icon className="h-4 w-4" />
                </div>
                <span className="text-sm font-medium capitalize text-muted-foreground">
                  {value.platform === "twitter" ? "X / Twitter" : value.platform}
                </span>
              </div>
              <h3 className="font-semibold text-foreground">{value.title}</h3>
              <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                {value.description}
              </p>
              <p className="mt-2 truncate text-xs text-primary">{value.url}</p>
            </div>
            <div className="flex items-center justify-between border-t border-border bg-muted/30 px-4 py-3">
              <div className="flex items-center gap-2 text-emerald-500">
                <Check className="h-5 w-5" />
                <span className="text-sm font-medium">Link attached</span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleRemove}
                className="text-destructive hover:bg-destructive/10 hover:text-destructive"
              >
                <X className="h-4 w-4 mr-1" />
                Remove
              </Button>
            </div>
          </motion.div>
        ) : (
          /* URL Input */
          <motion.div
            key="input"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="space-y-4"
          >
            <div className="relative">
              <div className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2">
                <Link2 className="h-5 w-5 text-muted-foreground" />
              </div>
              <input
                type="url"
                value={url}
                onChange={(e) => { setUrl(e.target.value); setError(null) }}
                onKeyDown={(e) => e.key === "Enter" && handleUrlSubmit()}
                placeholder="Paste a link here..."
                className={cn(
                  "w-full rounded-2xl border-2 bg-card py-4 pl-12 pr-4 text-foreground placeholder:text-muted-foreground/60 focus:outline-none transition-colors",
                  error
                    ? "border-destructive focus:border-destructive"
                    : "border-border focus:border-primary"
                )}
              />
            </div>

            {error && (
              <motion.p
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                className="text-sm text-destructive"
              >
                {error}
              </motion.p>
            )}

            <Button
              onClick={handleUrlSubmit}
              disabled={isLoading || !url.trim()}
              className="w-full rounded-xl py-6 text-base font-semibold"
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                  Fetching preview...
                </>
              ) : (
                <>
                  <ExternalLink className="mr-2 h-5 w-5" />
                  Add Link
                </>
              )}
            </Button>

            {/* Platform Hints */}
            <div className="flex flex-wrap justify-center gap-3">
              {Object.entries(PLATFORM_ICONS).slice(0, 4).map(([platform, PlatformIcon]) => (
                <div
                  key={platform}
                  className={cn(
                    "flex items-center gap-1.5 rounded-full px-3 py-1.5 ring-1",
                    PLATFORM_STYLES[platform as keyof typeof PLATFORM_STYLES]
                  )}
                >
                  <PlatformIcon className="h-3.5 w-3.5" />
                  <span className="text-xs font-medium capitalize">
                    {platform === "twitter" ? "X" : platform}
                  </span>
                </div>
              ))}
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Skip Button */}
      {!value && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3 }}
          className="flex justify-center"
        >
          <Button
            variant="ghost"
            onClick={onSkip}
            className="text-muted-foreground hover:text-foreground"
          >
            <SkipForward className="mr-2 h-4 w-4" />
            Skip this step
          </Button>
        </motion.div>
      )}
    </motion.div>
  )
}
