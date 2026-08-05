import { Home, BarChart2, PlusCircle, Trophy, Bell, User, BriefcaseBusiness, HeartPulse } from "lucide-react"

export const navigationItems = [
  {
    label: "Home",
    href: "/",
    icon: Home,
  },
  {
    label: "Polls",
    href: "/polls",
    icon: BarChart2,
  },
  {
    label: "Create Poll",
    href: "/create",
    icon: PlusCircle,
  },
  {
    label: "Leaderboard",
    href: "/leaderboard",
    icon: Trophy,
  },
  {
    label: "Notifications",
    href: "/notifications",
    icon: Bell,
  },
  {
    label: "Business",
    href: "/business",
    icon: BriefcaseBusiness,
  },
  {
    label: "Wellness",
    href: "/wellness",
    icon: HeartPulse,
  },
  {
    label: "Profile",
    href: "/profile",
    icon: User,
  },
] as const

export type NavigationItem = (typeof navigationItems)[number]

export const mobilePrimaryNavigation = navigationItems.slice(0, 4)
export const mobileSecondaryNavigation = navigationItems.slice(4)

export function isNavigationItemActive(pathname: string, href: string) {
  return href === "/" ? pathname === href : pathname === href || pathname.startsWith(`${href}/`)
}
