import { Home, BarChart2, PlusCircle, Trophy, Bell, User, BriefcaseBusiness, HeartPulse, Gamepad2, Target, Users } from "lucide-react"

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
    label: "Challenges",
    href: "/challenges",
    icon: Target,
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
  { label: "Social", href: "/social", icon: Users },
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
  {
    label: "Play",
    href: "/play",
    icon: Gamepad2,
  },
  {
    label: "Games",
    href: "/games",
    icon: Gamepad2,
  },
] as const

export const mobileNavigationItems = navigationItems.filter(item =>
  ["/", "/play", "/polls", "/create", "/profile"].includes(item.href)
)

export type NavigationItem = (typeof navigationItems)[number]

const mobilePrimaryHrefs = ["/", "/play", "/games", "/polls"]
const sidebarBottomHrefs = ["/play", "/games"]

export const mobilePrimaryNavigation = mobilePrimaryHrefs.map(
  href => navigationItems.find(item => item.href === href)!
)
export const mobileSecondaryNavigation = navigationItems.filter(
  item => !mobilePrimaryHrefs.includes(item.href)
)
export const sidebarPrimaryNavigation = navigationItems.filter(
  item => !sidebarBottomHrefs.includes(item.href)
)
export const sidebarBottomNavigation = sidebarBottomHrefs.map(
  href => navigationItems.find(item => item.href === href)!
)

export function isNavigationItemActive(pathname: string, href: string) {
  return href === "/" ? pathname === href : pathname === href || pathname.startsWith(`${href}/`)
}
