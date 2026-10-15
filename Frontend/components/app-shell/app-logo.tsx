"use client"

import { motion } from "framer-motion"
import Link from "next/link"
import Image from "next/image"

export function AppLogo() {
  return (
    <Link href="/" className="flex items-center gap-2.5">
      <motion.div
        className="relative flex h-10 w-10 items-center justify-center rounded-xl bg-[#17151f] p-1.5 shadow-sm"
        whileHover={{ scale: 1.05, rotate: 5 }}
        whileTap={{ scale: 0.95 }}
        transition={{ type: "spring", stiffness: 400, damping: 17 }}
      >
        <Image alt="PoP" height={30} priority src="/brand/pop-mark.svg" width={30} />
      </motion.div>
      <div className="flex flex-col">
        <span className="text-lg font-bold leading-tight tracking-tight text-foreground">
          PoP
        </span>
        <span className="text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
          Your voice matters
        </span>
      </div>
    </Link>
  )
}
