import {
  Briefcase,
  Cpu,
  HeartPulse,
  Landmark,
  Leaf,
  Palette,
  Sparkles,
  Trophy,
  Users,
} from "lucide-react"
import { categoryMeta } from "@/lib/categories"
import { cn } from "@/lib/utils"

const CATEGORY_ICONS = {
  briefcase: Briefcase,
  cpu: Cpu,
  "heart-pulse": HeartPulse,
  landmark: Landmark,
  leaf: Leaf,
  palette: Palette,
  sparkles: Sparkles,
  trophy: Trophy,
  users: Users,
}

interface CategoryBadgeProps {
  category: string | null | undefined
  className?: string
}

export function CategoryBadge({ category, className }: CategoryBadgeProps) {
  const meta = categoryMeta(category)
  const Icon = CATEGORY_ICONS[meta.icon as keyof typeof CATEGORY_ICONS] ?? Sparkles

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-semibold ring-1",
        meta.badgeClassName,
        className
      )}
    >
      <Icon className="h-3.5 w-3.5" />
      {meta.name}
    </span>
  )
}
