import type { Metadata } from "next"
import Link from "next/link"

export const metadata: Metadata = {
  title: "Terms of Use and Community Guidelines | Pop",
  description: "The rules for using Pop and participating in its polling community.",
}

const Section = ({ title, children }: { title: string; children: React.ReactNode }) => (
  <section className="space-y-3">
    <h2 className="text-xl font-semibold text-foreground">{title}</h2>
    <div className="space-y-3 leading-7 text-muted-foreground">{children}</div>
  </section>
)

export default function TermsPage() {
  return <main className="min-h-dvh bg-background px-5 py-10 sm:py-16">
    <article className="mx-auto max-w-3xl space-y-10 rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-10">
      <header className="space-y-3 border-b border-border pb-8">
        <Link href="/" className="text-sm font-semibold text-primary hover:underline">← Back to Pop</Link>
        <h1 className="text-3xl font-bold tracking-tight text-foreground sm:text-4xl">Terms of Use and Community Guidelines</h1>
        <p className="text-sm text-muted-foreground">Effective 11 October 2026 · Version 2026-10-11</p>
        <p className="leading-7 text-muted-foreground">These terms govern your use of the Pop website and mobile app. Creating an account means you accept these terms and the Community Guidelines below.</p>
      </header>

      <Section title="Using Pop"><p>You must provide accurate account information, protect your sign-in details, and use Pop lawfully. You are responsible for polls, comments, media, votes, and other content submitted through your account. Pop is not directed to children under 13.</p></Section>
      <Section title="Polls and voting"><p>Poll results represent participating users and are not scientific surveys unless explicitly stated. Do not use Pop results as medical, legal, financial, or emergency advice. You may vote only through your own account and must not manipulate results through automation, duplicate accounts, payments, threats, or coordinated abuse.</p></Section>
      <Section title="Your content"><p>You retain ownership of content you submit. You grant Pop a non-exclusive, worldwide, royalty-free licence to host, process, display, distribute, and adapt that content only as needed to operate, promote, secure, and improve the service. You must have permission to upload media and share source material.</p></Section>
      <Section title="Community Guidelines">
        <p>Do not post or promote illegal activity; threats or violence; hatred or discrimination; harassment or bullying; sexual exploitation; content that endangers minors; impersonation; privacy violations; spam; scams; malware; intellectual-property infringement; or deliberately deceptive content intended to cause harm.</p>
        <p>Polls should present choices clearly. Sponsored polls and business content must be identifiable and must not make misleading claims. Wellness polls are for general engagement and do not diagnose or treat health conditions.</p>
      </Section>
      <Section title="Safety and moderation"><p>Users can report content and block accounts. We may review, restrict, remove, or preserve content; limit features; suspend accounts; or terminate access when needed to enforce these terms, protect users, comply with law, or maintain the service. Repeated or severe violations may lead to permanent removal.</p></Section>
      <Section title="Business accounts and sponsorship"><p>Business accounts are separate from personal accounts. Business users are responsible for campaign claims, audience targeting, sponsorship disclosures, permissions, and compliance with advertising, consumer-protection, and industry rules. Subscription features and prices may change with notice.</p></Section>
      <Section title="Availability and liability"><p>Pop is provided on an “as available” basis. Features may change, pause, or end. To the extent permitted by law, Pop is not liable for indirect, incidental, or consequential losses arising from use of the service or reliance on user-generated poll results.</p></Section>
      <Section title="Account closure and changes"><p>You may delete your account from the app. We may update these terms as the service changes; the effective date and version will identify the revision. Continued use after an update takes effect means you accept the revised terms where permitted by law.</p></Section>
      <Section title="Privacy and contact"><p>Our <Link href="/privacy" className="font-semibold text-primary hover:underline">Privacy Policy</Link> explains how information is handled. For questions, reports, or appeals, use the support address published on Pop&apos;s Google Play listing.</p></Section>
    </article>
  </main>
}
