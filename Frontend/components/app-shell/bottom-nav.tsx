"use client"

import { AnimatePresence, motion } from "framer-motion"
import { useState } from "react"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { Menu, X } from "lucide-react"
import { isNavigationItemActive, mobilePrimaryNavigation, mobileSecondaryNavigation } from "@/lib/navigation"
import { cn } from "@/lib/utils"

export function BottomNav() {
  const pathname = usePathname()
  const [moreOpen, setMoreOpen] = useState(false)
  const secondaryActive = mobileSecondaryNavigation.some((item) =>
    isNavigationItemActive(pathname, item.href)
  )

  return (
    <nav aria-label="Mobile navigation" className="fixed bottom-0 left-0 right-0 z-40 lg:hidden">
      <AnimatePresence>
        {moreOpen && (
          <>
            <motion.button
              aria-label="Close more navigation"
              className="fixed inset-0 bottom-[calc(var(--bottom-nav-height)+env(safe-area-inset-bottom))] bg-black/40"
              initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              onClick={() => setMoreOpen(false)}
            />
            <motion.div
              aria-label="More destinations"
              className="fixed inset-x-3 bottom-[calc(var(--bottom-nav-height)+env(safe-area-inset-bottom)+0.75rem)] max-h-[calc(100dvh-7rem)] overflow-y-auto rounded-2xl border bg-background p-3 shadow-2xl sm:left-auto sm:right-4 sm:w-80"
              initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: 24 }}
            >
              <div className="mb-2 flex items-center justify-between px-2">
                <span className="font-semibold">More</span>
                <button aria-label="Close more navigation" className="flex size-11 items-center justify-center rounded-xl hover:bg-secondary" onClick={() => setMoreOpen(false)}><X className="size-5" /></button>
              </div>
              <div className="grid grid-cols-2 gap-2">
                {mobileSecondaryNavigation.map((item) => {
                  const Icon = item.icon
                  const active = isNavigationItemActive(pathname, item.href)
                  return <Link key={item.href} href={item.href} onClick={() => setMoreOpen(false)} className={cn("flex min-h-12 min-w-0 items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium", active ? "bg-primary text-primary-foreground" : "bg-secondary text-foreground")}><Icon className="size-5 shrink-0" /><span className="min-w-0 break-words">{item.label}</span></Link>
                })}
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
      <div className="border-t border-border/50 bg-background/90 glass bottom-nav-safe">
        <div className="mx-auto flex h-[4.5rem] max-w-lg items-stretch justify-around px-1">
          {mobilePrimaryNavigation.map((item) => {
            const isActive = isNavigationItemActive(pathname, item.href)
            const Icon = item.icon

            return (
              <Link
                key={item.href}
                href={item.href}
                className="relative flex min-h-11 min-w-0 flex-1 flex-col items-center justify-center py-1"
              >
                <motion.div
                  className={cn(
                    "flex min-w-0 flex-col items-center gap-1 rounded-2xl px-1 py-2 transition-colors",
                    isActive
                      ? "text-primary"
                      : "text-muted-foreground hover:text-foreground"
                  )}
                  whileTap={{ scale: 0.9 }}
                >
                  {/* Active indicator */}
                  {isActive && (
                    <motion.div
                      layoutId="bottomNavIndicator"
                      className="absolute -top-0.5 h-1 w-8 rounded-full bg-primary"
                      transition={{
                        type: "spring",
                        stiffness: 500,
                        damping: 30,
                      }}
                    />
                  )}

                  <motion.div
                    animate={isActive ? { y: -2 } : { y: 0 }}
                    transition={{ type: "spring", stiffness: 400, damping: 17 }}
                  >
                    <Icon
                      className={cn(
                        "h-6 w-6 transition-all",
                        isActive && "stroke-[2.5px]"
                      )}
                    />
                  </motion.div>

                  <span
                    className={cn(
                      "text-[10px] font-medium transition-all",
                      isActive && "font-semibold"
                    )}
                  >
                    {item.label === "Create Poll" ? "Create" : item.label}
                  </span>
                </motion.div>
              </Link>
            )
          })}
          <button aria-expanded={moreOpen} aria-label="More destinations" onClick={() => setMoreOpen((open) => !open)} className={cn("relative flex min-h-11 min-w-0 flex-1 flex-col items-center justify-center gap-1 rounded-2xl px-1 py-2 text-[10px] font-medium", secondaryActive || moreOpen ? "text-primary" : "text-muted-foreground")}>
            <Menu className="size-6" /><span>More</span>
          </button>
        </div>
      </div>
    </nav>
  )
}
