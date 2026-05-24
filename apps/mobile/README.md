# Pollify Mobile

Expo React Native app for Pollify's Android-first mobile experience, with iOS support kept in the same codebase.

The app uses the existing Pollify backend auth endpoints:

- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/auth/me`

JWT sessions are stored with Expo SecureStore on device.

Signed-in users can switch between mobile gamification surfaces:

- Home: current XP, streak, and level progress.
- Profile: votes, polls created, XP, streak, level, and joined date.
- Ranks: real backend leaderboard data from `GET /api/users/leaderboard`.

## Requirements

- Node.js 20.19.4 or newer is recommended for the current Expo package set.
- Android Studio and an Android emulator, or a physical Android device with Expo Go.
- The ASP.NET Core backend running locally or on a reachable development URL.

## Run Locally

```bash
cd apps/mobile
npm install
npm run android
```

The app reads the backend URL from `EXPO_PUBLIC_API_URL`.

For the Android emulator, use:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://10.0.2.2:5177"
npm run android
```

For a physical device, use your machine's LAN IP address:

```powershell
$env:EXPO_PUBLIC_API_URL = "http://192.168.1.10:5177"
npm run android
```

The web app remains in `Frontend` and can continue to use `NEXT_PUBLIC_API_URL`.

## Auth Notes

Email/password login and registration are wired for the mobile MVP. Google sign-in still needs native Android/iOS OAuth client IDs before it should be exposed in the app store build.
