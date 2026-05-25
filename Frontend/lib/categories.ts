export type PollCategory = {
  id: number
  name: string
  slug: string
  icon: string
  color: string
  sortOrder: number
  isActive: boolean
  chipClassName: string
  badgeClassName: string
}

export const DEFAULT_CATEGORY = "General"

export const POLL_CATEGORIES: PollCategory[] = [
  {
    id: 1,
    name: DEFAULT_CATEGORY,
    slug: "general",
    icon: "sparkles",
    color: "slate",
    sortOrder: 10,
    isActive: true,
    chipClassName: "bg-slate-500/10 text-slate-700 hover:bg-slate-500/15 dark:text-slate-300",
    badgeClassName: "bg-slate-500/10 text-slate-700 ring-slate-500/20 dark:text-slate-300",
  },
  {
    id: 2,
    name: "Technology",
    slug: "technology",
    icon: "cpu",
    color: "blue",
    sortOrder: 20,
    isActive: true,
    chipClassName: "bg-blue-500/10 text-blue-700 hover:bg-blue-500/15 dark:text-blue-300",
    badgeClassName: "bg-blue-500/10 text-blue-700 ring-blue-500/20 dark:text-blue-300",
  },
  {
    id: 3,
    name: "Society",
    slug: "society",
    icon: "users",
    color: "rose",
    sortOrder: 30,
    isActive: true,
    chipClassName: "bg-rose-500/10 text-rose-700 hover:bg-rose-500/15 dark:text-rose-300",
    badgeClassName: "bg-rose-500/10 text-rose-700 ring-rose-500/20 dark:text-rose-300",
  },
  {
    id: 4,
    name: "Work",
    slug: "work",
    icon: "briefcase",
    color: "amber",
    sortOrder: 40,
    isActive: true,
    chipClassName: "bg-amber-500/10 text-amber-700 hover:bg-amber-500/15 dark:text-amber-300",
    badgeClassName: "bg-amber-500/10 text-amber-700 ring-amber-500/20 dark:text-amber-300",
  },
  {
    id: 5,
    name: "Environment",
    slug: "environment",
    icon: "leaf",
    color: "emerald",
    sortOrder: 50,
    isActive: true,
    chipClassName: "bg-emerald-500/10 text-emerald-700 hover:bg-emerald-500/15 dark:text-emerald-300",
    badgeClassName: "bg-emerald-500/10 text-emerald-700 ring-emerald-500/20 dark:text-emerald-300",
  },
  {
    id: 6,
    name: "Culture",
    slug: "culture",
    icon: "palette",
    color: "violet",
    sortOrder: 60,
    isActive: true,
    chipClassName: "bg-violet-500/10 text-violet-700 hover:bg-violet-500/15 dark:text-violet-300",
    badgeClassName: "bg-violet-500/10 text-violet-700 ring-violet-500/20 dark:text-violet-300",
  },
  {
    id: 7,
    name: "Sports",
    slug: "sports",
    icon: "trophy",
    color: "orange",
    sortOrder: 70,
    isActive: true,
    chipClassName: "bg-orange-500/10 text-orange-700 hover:bg-orange-500/15 dark:text-orange-300",
    badgeClassName: "bg-orange-500/10 text-orange-700 ring-orange-500/20 dark:text-orange-300",
  },
  {
    id: 8,
    name: "Health",
    slug: "health",
    icon: "heart-pulse",
    color: "teal",
    sortOrder: 80,
    isActive: true,
    chipClassName: "bg-teal-500/10 text-teal-700 hover:bg-teal-500/15 dark:text-teal-300",
    badgeClassName: "bg-teal-500/10 text-teal-700 ring-teal-500/20 dark:text-teal-300",
  },
  {
    id: 9,
    name: "Politics",
    slug: "politics",
    icon: "landmark",
    color: "indigo",
    sortOrder: 90,
    isActive: true,
    chipClassName: "bg-indigo-500/10 text-indigo-700 hover:bg-indigo-500/15 dark:text-indigo-300",
    badgeClassName: "bg-indigo-500/10 text-indigo-700 ring-indigo-500/20 dark:text-indigo-300",
  },
]

export const FEED_CATEGORIES = [
  "All",
  ...POLL_CATEGORIES.filter((category) => category.isActive).map((category) => category.name),
] as const

const CATEGORY_ALIASES: Record<string, string> = {
  tech: "Technology",
  business: "Work",
  career: "Work",
  jobs: "Work",
  climate: "Environment",
  entertainment: "Culture",
  arts: "Culture",
  movies: "Culture",
  wellness: "Health",
  medical: "Health",
  fitness: "Health",
  news: "Politics",
  government: "Politics",
}

function slugify(value: string) {
  return value.trim().toLowerCase().replace(/\s+/g, "-").replace(/_/g, "-")
}

export function categoryMeta(category: string | null | undefined): PollCategory {
  if (!category?.trim()) {
    return POLL_CATEGORIES[0]
  }

  const value = category.trim()
  const slug = slugify(value)
  const alias = CATEGORY_ALIASES[value.toLowerCase()] ?? CATEGORY_ALIASES[slug]

  return (
    POLL_CATEGORIES.find(
      (item) => item.name.toLowerCase() === value.toLowerCase() || item.slug === slug
    ) ??
    POLL_CATEGORIES.find((item) => item.name === alias) ??
    POLL_CATEGORIES[0]
  )
}

export function normalizeCategoryName(category: string | null | undefined) {
  return categoryMeta(category).name
}
