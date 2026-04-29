"use client"

import { motion } from "framer-motion"
import Link from "next/link"

export function AppLogo() {
  return (
    <Link href="/" className="flex items-center gap-2.5">
      <motion.div
        className="relative flex h-9 w-9 items-center justify-center rounded-xl bg-primary"
        whileHover={{ scale: 1.05, rotate: 5 }}
        whileTap={{ scale: 0.95 }}
        transition={{ type: "spring", stiffness: 400, damping: 17 }}
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          className="h-5 w-5 text-primary-foreground"
          stroke="currentColor"
          strokeWidth="2.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M3 3v18h18" />
          <path d="M7 16l4-8 4 5 5-9" />
        </svg>
        <motion.div
          className="absolute -right-0.5 -top-0.5 h-2.5 w-2.5 rounded-full bg-accent"
          animate={{ scale: [1, 1.2, 1] }}
          transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
        />
      </motion.div>
      <div className="flex flex-col">
        <span className="text-lg font-bold leading-tight tracking-tight text-foreground">
          Pollify
        </span>
        <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
          Your voice matters
        </span>
      </div>
    </Link>
  )
}
