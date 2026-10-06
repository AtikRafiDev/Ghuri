import { CompassIcon, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

type Tone = 'forest' | 'sun' | 'clay'

// The icon's soft halo says the mood: green all good / neutral, amber waiting, terracotta something failed.
const halo: Record<Tone, string> = {
  forest: 'bg-forest-50 text-forest-600 ring-forest-50/60',
  sun: 'bg-sun-50 text-sun-700 ring-sun-50/70',
  clay: 'bg-clay-50 text-clay-600 ring-clay-50/70',
}

/**
 * A whole-page message with an action under it: "Nothing to book yet", "Your
 * seats were released"... A white card in the middle of the page: an icon in
 * a soft halo, the heading, a line or two, then the buttons. `visual`
 * replaces the icon when the moment needs something livelier (the "we're
 * confirming your payment" animation).
 */
export function PageMessage({
  title,
  text,
  children,
  icon: Icon = CompassIcon,
  tone = 'forest',
  visual,
}: {
  title: string
  text: ReactNode
  children?: ReactNode
  icon?: LucideIcon
  tone?: Tone
  visual?: ReactNode
}) {
  return (
    <section className="mx-auto my-2 grid w-full max-w-xl animate-fade-up justify-items-center gap-3 rounded-3xl bg-card px-6 py-12 text-center shadow-card ring-1 ring-ink-200/80 sm:my-6 sm:px-12 sm:py-14">
      {visual ?? (
        <span className={cn('flex size-16 items-center justify-center rounded-2xl ring-8', halo[tone])}>
          <Icon className="size-7" />
        </span>
      )}
      <h1 className="mt-4 text-2xl font-bold text-ink-900">{title}</h1>
      <p className="max-w-md text-ink-500">{text}</p>
      {children && <div className="mt-3 flex flex-wrap justify-center gap-2">{children}</div>}
    </section>
  )
}
