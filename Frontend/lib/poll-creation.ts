export interface PollDraft {
  question: string
  description: string
  media: MediaAttachment | null
  externalLink: ExternalLink | null
  privacy: "public" | "private"
}

export interface MediaAttachment {
  type: "image" | "video" | "audio" | "document"
  url: string
  name: string
  size?: number
}

export interface ExternalLink {
  url: string
  platform: "youtube" | "instagram" | "twitter" | "news" | "other"
  title?: string
  thumbnail?: string
  description?: string
}

export const STEPS = [
  { id: 1, title: "Question", description: "What do you want to ask?" },
  { id: 2, title: "Description", description: "Add context (optional)" },
  { id: 3, title: "Media", description: "Attach visuals" },
  { id: 4, title: "Link", description: "Add external source" },
  { id: 5, title: "Privacy", description: "Who can see this?" },
  { id: 6, title: "Preview", description: "Review your poll" },
] as const

export const MEDIA_TYPES = [
  { type: "image" as const, label: "Image", icon: "Image", accept: "image/*" },
  { type: "video" as const, label: "Video", icon: "Video", accept: "video/*" },
  { type: "audio" as const, label: "Audio", icon: "Music", accept: "audio/*" },
  { type: "document" as const, label: "Document", icon: "FileText", accept: ".pdf,.doc,.docx" },
] as const

export const LINK_PLATFORMS = [
  { id: "youtube", label: "YouTube", color: "bg-red-500/10 text-red-500 ring-red-500/20" },
  { id: "instagram", label: "Instagram", color: "bg-pink-500/10 text-pink-500 ring-pink-500/20" },
  { id: "twitter", label: "X / Twitter", color: "bg-foreground/10 text-foreground ring-foreground/20" },
  { id: "news", label: "News Article", color: "bg-blue-500/10 text-blue-500 ring-blue-500/20" },
  { id: "other", label: "Other Link", color: "bg-muted text-muted-foreground ring-border" },
] as const

export function detectPlatform(url: string): ExternalLink["platform"] {
  if (url.includes("youtube.com") || url.includes("youtu.be")) return "youtube"
  if (url.includes("instagram.com")) return "instagram"
  if (url.includes("twitter.com") || url.includes("x.com")) return "twitter"
  if (url.includes("news") || url.includes("article") || url.includes("bbc") || url.includes("cnn")) return "news"
  return "other"
}

export function getInitialDraft(): PollDraft {
  return {
    question: "",
    description: "",
    media: null,
    externalLink: null,
    privacy: "public",
  }
}
