# Pollify

A gamified public opinion polling platform built with Next.js, TypeScript, and Tailwind CSS.

## Features

- Create polls with a multi-step wizard
- Vote on public polls with real-time feedback
- Earn XP and track voting streaks (gamification)
- Leaderboard, notifications, and user profiles
- Dark/light theme support

## Tech Stack

- **Framework:** Next.js 16 (React 19, Turbopack)
- **Language:** TypeScript
- **Styling:** Tailwind CSS 4 + Radix UI
- **Animation:** Framer Motion
- **Forms:** React Hook Form + Zod

## Running Locally

### Prerequisites

- [Node.js](https://nodejs.org/) v18+
- [pnpm](https://pnpm.io/) — install with `npm install -g pnpm`

### Steps

```bash
# 1. Install dependencies
cd Frontend
pnpm install

# 2. Start the development server
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Other commands

```bash
pnpm build   # Production build
pnpm start   # Start production server (run build first)
pnpm lint    # Run ESLint
```
