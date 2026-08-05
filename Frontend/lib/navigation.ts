import { Home, BarChart2, PlusCircle, Trophy, Bell, User, BriefcaseBusiness, HeartPulse, Gamepad2 } from "lucide-react"

export const navigationItems = [
  {
    label: "Play",
    href: "/play",
    icon: Gamepad2,
  },
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

export const mobileNavigationItems = navigationItems.filter(item =>
  ["/", "/play", "/polls", "/create", "/profile"].includes(item.href)
)

export type NavigationItem = (typeof navigationItems)[number]
