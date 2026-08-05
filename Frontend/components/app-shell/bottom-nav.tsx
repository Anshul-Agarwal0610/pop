"use client"

import { motion } from "framer-motion"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { mobileNavigationItems } from "@/lib/navigation"
import { cn } from "@/lib/utils"

export function BottomNav() {
  const pathname = usePathname()

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 md:hidden">
      <div className="border-t border-border/50 bg-background/90 glass bottom-nav-safe">
        <div className="mx-auto flex h-[4.5rem] max-w-lg items-center justify-around px-2">
          {mobileNavigationItems.map((item) => {
            const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(`${item.href}/`))
            const Icon = item.icon

            return (
              <Link
                key={item.href}
                href={item.href}
                className="relative flex flex-1 flex-col items-center justify-center py-2"
              >
                <motion.div
                  className={cn(
                    "flex min-w-0 flex-col items-center gap-1 rounded-2xl px-1 py-2 transition-colors sm:px-3",
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
                    {item.label}
                  </span>
                </motion.div>
              </Link>
            )
          })}
        </div>
      </div>
    </nav>
  )
}
