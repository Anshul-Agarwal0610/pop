# Pollify

Generated polls follow the documented [canonical Up/Against API and persistence contract](docs/polls/generated-binary-contract.md).

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
- For mobile development, Node.js 20.19.4+ is recommended by the current Expo dependency set.

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

## Mobile App

Pollify now includes an Expo React Native foundation in `apps/mobile` for Android-first delivery while keeping iOS support in the same codebase.

```bash
cd apps/mobile
npm install
```

```powershell
$env:EXPO_PUBLIC_API_URL = "http://10.0.2.2:5177"
npm run android
```

Use `http://10.0.2.2:5177` for the Android emulator, or your machine's LAN IP address when testing from a physical device.

### Other commands

```bash
npm run build   # Production build
npm run start   # Start production server (run build first)
npm run lint    # Run ESLint
```
