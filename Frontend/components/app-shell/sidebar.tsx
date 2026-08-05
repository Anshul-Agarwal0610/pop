"use client"

import { useEffect, useState } from "react"
import { motion } from "framer-motion"
import Link from "next/link"
import { usePathname } from "next/navigation"
import { Moon, Sun } from "lucide-react"
import { useTheme } from "next-themes"
import { navigationItems } from "@/lib/navigation"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"

export function Sidebar() {
  const pathname = usePathname()
  const { theme, setTheme } = useTheme()
  const [mounted, setMounted] = useState(false)
  useEffect(() => setMounted(true), [])

  return (
    <aside className="fixed left-0 top-16 z-30 hidden h-[calc(100vh-4rem)] w-64 border-r border-border/50 bg-sidebar md:block">
      <div className="flex h-full flex-col p-4">
        {/* Navigation */}
        <nav className="flex-1 space-y-1">
          {navigationItems.map((item, index) => {
            const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(`${item.href}/`))
            const Icon = item.icon

            return (
              <motion.div
                key={item.href}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: index * 0.05 }}
              >
                <Link
                  href={item.href}
                  className={cn(
                    "group relative flex items-center gap-3 rounded-xl px-4 py-3 font-medium transition-all",
                    isActive
                      ? "bg-primary text-primary-foreground shadow-lg shadow-primary/25"
                      : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                  )}
                >
                  {/* Hover effect */}
                  {!isActive && (
                    <motion.div
                      className="absolute inset-0 rounded-xl bg-secondary opacity-0 group-hover:opacity-100"
                      layoutId={`sidebar-hover-${item.href}`}
                      transition={{ duration: 0.2 }}
                    />
                  )}

                  <Icon
                    className={cn(
                      "relative z-10 h-5 w-5 transition-transform group-hover:scale-110",
                      isActive && "stroke-[2.5px]"
                    )}
                  />
                  <span className="relative z-10">{item.label}</span>

                  {/* Active indicator dot */}
                  {isActive && (
                    <motion.div
                      layoutId="sidebarActiveIndicator"
                      className="absolute right-3 h-2 w-2 rounded-full bg-primary-foreground"
                      transition={{
                        type: "spring",
                        stiffness: 500,
                        damping: 30,
                      }}
                    />
                  )}
                </Link>
              </motion.div>
            )
          })}
        </nav>

        {/* Bottom section */}
        <div className="space-y-3 border-t border-sidebar-border pt-4">
          {/* Theme toggle */}
          <div className="flex items-center justify-between rounded-xl bg-secondary/50 p-2">
            <span className="pl-2 text-sm font-medium text-muted-foreground">
              Theme
            </span>
            <div className="flex gap-1">
              <Button
                variant={mounted && theme === "light" ? "default" : "ghost"}
                size="icon"
                className="h-8 w-8 rounded-lg"
                onClick={() => setTheme("light")}
              >
                <Sun className="h-4 w-4" />
                <span className="sr-only">Light mode</span>
              </Button>
              <Button
                variant={mounted && theme === "dark" ? "default" : "ghost"}
                size="icon"
                className="h-8 w-8 rounded-lg"
                onClick={() => setTheme("dark")}
              >
                <Moon className="h-4 w-4" />
                <span className="sr-only">Dark mode</span>
              </Button>
            </div>
          </div>
        </div>
      </div>
    </aside>
  )
}
