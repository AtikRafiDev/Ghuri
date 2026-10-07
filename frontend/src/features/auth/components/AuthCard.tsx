import { LockIcon, ShieldCheckIcon, TimerIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { site } from '@/shared/config/site'
import { Photo } from '@/shared/photos/Photo'
import { sajekPhoto } from '@/shared/photos/photos'

const promises = [
  { icon: LockIcon, text: 'Secure online payment by SSLCommerz' },
  { icon: TimerIcon, text: 'Your seats are held while you pay' },
  { icon: ShieldCheckIcon, text: 'Full refund 30+ days before the trip' },
]

/**
 * The frame every auth page sits in - one look for login, register, forgot
 * and reset, in the home page's style: a full-screen photo (clouds over
 * Sajek Valley) that slides up under the see-through header (full-bleed
 * route), with the form in a card on top. On a wide screen the promises sit
 * beside it on the photo; on a phone it's just the card.
 *
 * Only the photo side is a brand surface (always light text). The card
 * follows the theme like any other card, so the form is dark in dark mode.
 * All motion is CSS, so it stands still with "reduce motion" on (index.css).
 */
export function AuthCard({
  title,
  description,
  footer,
  children,
}: {
  title: string
  description?: string
  footer?: ReactNode
  children: ReactNode
}) {
  return (
    // -mt: slides up under the sticky header (64px + its 1px border).
    <section className="relative isolate -mt-[calc(4rem+1px)] flex min-h-svh flex-col overflow-hidden bg-forest-950">
      <div aria-hidden className="absolute inset-0 -z-10">
        <Photo photo={sajekPhoto} fetchPriority="high" className="animate-ken-burns" />
      </div>
      {/* Shades: darker at the bottom and on the left (behind the promises), and under the header. */}
      <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-t from-forest-950/90 via-forest-950/40 to-forest-950/30" />
      <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-r from-forest-950/80 via-forest-950/30 to-transparent" />

      <div className="mx-auto grid w-full max-w-6xl flex-1 items-center gap-12 px-4 pt-28 pb-10 sm:pt-32 lg:grid-cols-[1fr_29rem] lg:gap-16">
        <Pitch />

        <div className="grid animate-[fade-up_0.7s_var(--ease-out-expo)_0.1s_backwards] gap-7 rounded-[2rem] bg-card px-6 py-9 shadow-pop ring-1 ring-ink-200/80 sm:px-10 sm:py-11">
          <header className="grid gap-2">
            <p className="flex items-center gap-2 text-xs font-bold tracking-[0.18em] text-forest-600 uppercase">
              <span aria-hidden className="h-px w-8 bg-forest-500" />
              {site.name}
            </p>
            <h1 className="text-3xl leading-tight font-extrabold tracking-tight text-ink-900">{title}</h1>
            {description && <p className="text-ink-500">{description}</p>}
          </header>
          <div>{children}</div>
          {footer && <footer className="border-t pt-6 text-center text-sm text-ink-500">{footer}</footer>}
        </div>
      </div>
    </section>
  )
}

/** The left side on a laptop: the headline and the three promises, straight on the photo. */
function Pitch() {
  return (
    <div className="brand-surface hidden content-center gap-8 text-white lg:grid">
      <div className="grid animate-fade-up gap-5">
        <p className="flex items-center gap-2 text-xs font-bold tracking-[0.18em] text-sun-300 uppercase">
          <span aria-hidden className="h-px w-8 bg-sun-300" />
          {site.tagline}
        </p>
        <h2 className="text-5xl leading-[1.04] font-extrabold tracking-tight xl:text-6xl">
          Your next trip is
          <span className="block text-sun-300">one step away.</span>
        </h2>
        <p className="max-w-md text-lg text-forest-50/85">Hand-picked hotels, local guides and every transfer arranged - you just pack the bag.</p>
      </div>
      <ul className="stagger grid max-w-md gap-3">
        {promises.map(({ icon: Icon, text }) => (
          <li key={text} className="flex items-center gap-3 rounded-2xl bg-white/10 p-3 pr-5 text-sm font-semibold ring-1 ring-white/15 backdrop-blur-md">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-sun-500 text-forest-950">
              <Icon className="size-4" />
            </span>
            {text}
          </li>
        ))}
      </ul>
    </div>
  )
}
