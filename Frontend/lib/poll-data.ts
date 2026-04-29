export type MediaSource = "youtube" | "instagram" | "twitter" | "news" | "tiktok"

export interface Poll {
  id: string
  question: string
  category: string
  trending: boolean
  mediaType: "image" | "video" | "embed"
  mediaUrl: string
  mediaThumbnail?: string
  source: MediaSource
  sourceLabel: string
  sourceUrl?: string
  totalVotes: number
  yesPercentage: number
  xpReward: number
  timePosted: string
}

export const MOCK_POLLS: Poll[] = [
  {
    id: "1",
    question: "Should AI-generated art be allowed in art competitions?",
    category: "Technology",
    trending: true,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800&q=80",
    source: "twitter",
    sourceLabel: "@TechCrunch",
    totalVotes: 24567,
    yesPercentage: 42,
    xpReward: 25,
    timePosted: "2h ago",
  },
  {
    id: "2",
    question: "Is remote work better than office work for productivity?",
    category: "Work",
    trending: true,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1587560699334-cc4ff634909a?w=800&q=80",
    source: "news",
    sourceLabel: "Forbes",
    totalVotes: 89234,
    yesPercentage: 67,
    xpReward: 30,
    timePosted: "4h ago",
  },
  {
    id: "3",
    question: "Should social media have age verification?",
    category: "Society",
    trending: false,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=800&q=80",
    source: "instagram",
    sourceLabel: "@socialmediatoday",
    totalVotes: 156789,
    yesPercentage: 78,
    xpReward: 20,
    timePosted: "6h ago",
  },
  {
    id: "4",
    question: "Will electric cars fully replace gas cars by 2035?",
    category: "Environment",
    trending: true,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1593941707882-a5bba14938c7?w=800&q=80",
    source: "youtube",
    sourceLabel: "MKBHD",
    totalVotes: 45123,
    yesPercentage: 54,
    xpReward: 35,
    timePosted: "8h ago",
  },
  {
    id: "5",
    question: "Should tipping culture be abolished?",
    category: "Culture",
    trending: false,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=800&q=80",
    source: "tiktok",
    sourceLabel: "@serverlife",
    totalVotes: 234567,
    yesPercentage: 61,
    xpReward: 25,
    timePosted: "12h ago",
  },
  {
    id: "6",
    question: "Is the 4-day work week the future?",
    category: "Work",
    trending: true,
    mediaType: "image",
    mediaUrl: "https://images.unsplash.com/photo-1497032628192-86f99bcd76bc?w=800&q=80",
    source: "news",
    sourceLabel: "BBC News",
    totalVotes: 178923,
    yesPercentage: 82,
    xpReward: 40,
    timePosted: "1d ago",
  },
]

export const SOURCE_COLORS: Record<MediaSource, { bg: string; text: string; icon: string }> = {
  youtube: { bg: "bg-red-500/10", text: "text-red-500", icon: "text-red-500" },
  instagram: { bg: "bg-pink-500/10", text: "text-pink-500", icon: "text-pink-500" },
  twitter: { bg: "bg-sky-500/10", text: "text-sky-500", icon: "text-sky-500" },
  news: { bg: "bg-emerald-500/10", text: "text-emerald-500", icon: "text-emerald-500" },
  tiktok: { bg: "bg-foreground/10", text: "text-foreground", icon: "text-foreground" },
}
