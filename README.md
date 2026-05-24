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

- [Node.js](https://nodejs.org/) v18+ (includes npm)

### Steps

```bash
# 1. Go into the Frontend folder
cd Frontend

# 2. Copy the example environment file and confirm the backend URL
copy .env.local.example .env.local

# 3. Install dependencies
npm install --legacy-peer-deps

# 4. Start the development server
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

By default, the frontend expects the ASP.NET Core backend at `http://localhost:5177`.
Override it with `NEXT_PUBLIC_API_URL` when needed.

### Other commands

```bash
npm run build   # Production build
npm run start   # Start production server (run build first)
npm run lint    # Run ESLint
```
