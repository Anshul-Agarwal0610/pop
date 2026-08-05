"use client"

import { useEffect, useRef } from "react"
import { motion } from "framer-motion"
import { categoryMeta, FEED_CATEGORIES } from "@/lib/categories"
import { cn } from "@/lib/utils"

interface CategoryChipsProps {
  selected: string
  onSelect: (category: string) => void
}

export function CategoryChips({ selected, onSelect }: CategoryChipsProps) {
  const chipRefs = useRef(new Map<string, HTMLButtonElement>())

  useEffect(() => {
    chipRefs.current
      .get(selected)
      ?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" })
  }, [selected])

  return (
    <div className="border-y border-border/60 bg-background/80 py-2 backdrop-blur-sm">
      <div className="flex gap-2 overflow-x-auto scroll-smooth px-4 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {FEED_CATEGORIES.map((category) => {
          const active = selected === category
          const meta = category === "All" ? null : categoryMeta(category)

          return (
            <motion.button
              animate={{ scale: active ? 1.03 : 1 }}
              className={cn(
                "relative h-9 flex-shrink-0 rounded-full px-4 text-sm font-semibold transition-colors",
                active
                  ? "bg-primary text-primary-foreground shadow-sm"
                  : meta?.chipClassName ?? "bg-secondary text-secondary-foreground hover:bg-secondary/80"
              )}
              key={category}
              ref={(node) => {
                if (node) chipRefs.current.set(category, node)
                else chipRefs.current.delete(category)
              }}
              onClick={() => onSelect(category)}
              type="button"
              whileTap={{ scale: 0.97 }}
            >
              {category}
            </motion.button>
          )
        })}
      </div>
    </div>
  )
}
