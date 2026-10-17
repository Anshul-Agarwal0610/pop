import type { Metadata } from "next"
import Link from "next/link"

export const metadata: Metadata = {
  title: "Privacy Policy | Pop",
  description: "How Pop collects, uses, stores, and deletes personal data.",
}

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <section className="space-y-3">
    <h2 className="text-xl font-semibold text-foreground">{title}</h2>
    <div className="space-y-3 leading-7 text-muted-foreground">{children}</div>
  </section>
)

export default function PrivacyPolicyPage() {
  return (
    <main className="min-h-dvh bg-background px-5 py-10 sm:py-16">
      <article className="mx-auto max-w-3xl space-y-10 rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-10">
        <header className="space-y-3 border-b border-border pb-8">
          <Link href="/" className="text-sm font-semibold text-primary hover:underline">← Back to Pop</Link>
          <h1 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">Privacy Policy</h1>
          <p className="text-sm text-muted-foreground">Effective 10 October 2026</p>
          <p className="leading-7 text-muted-foreground">
            This policy explains how Pop (also presented as Pollify in parts of the service) handles information when you use our website and mobile app.
          </p>
        </header>

        <Section title="Information we collect">
          <p>We collect account information such as your username, display name, authentication provider, and, when supplied through Google sign-in, your email address and profile image.</p>
          <p>We store content and activity needed to operate the service, including polls, poll options, votes, source links, uploaded photos or videos, comments or reports, XP, streaks, achievements, challenges, games, groups, and social connections.</p>
          <p>Wellness check-in answers and notes are treated as private wellness information. We also process notification preferences and a device push token when you enable notifications. Optional product analytics are collected only after you give consent.</p>
        </Section>

        <Section title="How we use information">
          <p>We use this information to create and secure your account, show and personalize polls, record votes, calculate results and rewards, provide social and wellness features, deliver requested notifications, prevent abuse, and troubleshoot the service.</p>
          <p>With your consent, we use limited usage analytics to understand feature performance and improve Pop. You can withdraw analytics consent in the app at any time.</p>
        </Section>

        <Section title="Public content and sharing">
          <p>Public polls, profile information, leaderboard entries, and other content you choose to publish may be visible to other users. Private wellness responses and notes are not published. Any future sponsor reporting based on wellness polls will use aggregated or de-identified results and will not include your identity or private notes.</p>
          <p>We use service providers for cloud hosting and storage, Google authentication, and Expo notifications. A YouTube video is loaded from YouTube only when you choose to play it; YouTube may then receive device and network information under its own privacy terms. We do not sell personal information.</p>
        </Section>

        <section id="account-deletion" className="space-y-4 rounded-2xl border border-primary/30 bg-primary/5 p-5 sm:p-6">
          <h2 className="text-xl font-semibold text-foreground">Delete your Pop account</h2>
          <p className="leading-7 text-muted-foreground">You can permanently delete your account and associated personal data from the mobile app under More → Preferences → Your data, or use our web deletion resource.</p>
          <Link href="/delete-account" className="inline-flex rounded-xl bg-primary px-5 py-3 text-sm font-semibold text-primary-foreground hover:opacity-90">Open account deletion</Link>
        </section>

        <Section title="Retention after account deletion">
          <p>You can delete your account in the mobile app under More → Preferences → Your data. Deletion removes your sign-in credentials, profile details, private wellness history, preferences, notifications, device tokens, and social connections.</p>
          <p>Public polls and votes may remain in anonymized form so discussions and aggregate voting results stay accurate. They are no longer connected to your name, email, avatar, or sign-in credentials. Limited security logs and encrypted backups may remain for their normal retention period before deletion.</p>
        </Section>

        <Section title="Your choices">
          <p>You can update your display name and avatar, change categories and notifications, withdraw analytics consent, and delete your account from inside the app. You may also deny camera, photo, video, or notification permissions through Android settings; related features will then be unavailable.</p>
        </Section>

        <Section title="Security, children, and international processing">
          <p>We use access controls and encryption in transit to protect data, but no online service can guarantee absolute security. Pop is not directed to children under 13. Service providers may process information in countries other than your own, subject to their contractual and legal safeguards.</p>
        </Section>

        <Section title="Changes and contact">
          <p>We may update this policy as the product changes. The effective date above will change when we publish a revision. For privacy or deletion questions, contact the support address published on Pop&apos;s Google Play listing.</p>
        </Section>
      </article>
    </main>
  )
}
