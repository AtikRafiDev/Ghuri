import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'

// Where each confetti piece flies (x, y in px from the centre), its spin, colour and shape.
const confetti = [
  [-92, -58, 220, 'bg-sun-500', 'rounded-sm'],
  [-70, -96, -160, 'bg-forest-400', 'rounded-full'],
  [-24, -112, 300, 'bg-clay-500', 'rounded-sm'],
  [30, -108, -240, 'bg-sun-300', 'rounded-full'],
  [78, -84, 180, 'bg-forest-300', 'rounded-sm'],
  [104, -30, -300, 'bg-clay-300', 'rounded-full'],
  [96, 40, 260, 'bg-sun-500', 'rounded-sm'],
  [58, 92, -200, 'bg-forest-500', 'rounded-full'],
  [-6, 110, 140, 'bg-sun-300', 'rounded-sm'],
  [-62, 88, -280, 'bg-clay-500', 'rounded-full'],
  [-104, 34, 200, 'bg-forest-300', 'rounded-sm'],
  [-112, -14, -120, 'bg-sun-500', 'rounded-full'],
] as const

/**
 * The "it worked!" moment: a ring draws itself, fills green, a tick is
 * drawn, two ripples spread and confetti bursts out. Purely decorative -
 * the words next to it say what happened (screen readers get those).
 */
export function SuccessBurst({ size = 112, className }: { size?: number; className?: string }) {
  return (
    <div aria-hidden className={cn('relative shrink-0', className)} style={{ width: size, height: size }}>
      <span className="absolute inset-0 animate-[ring-ping_1.6s_ease-out_0.7s_both] rounded-full border-2 border-forest-300" />
      <span className="absolute inset-0 animate-[ring-ping_1.6s_ease-out_1s_both] rounded-full border-2 border-forest-200" />
      {confetti.map(([x, y, r, color, shape], i) => (
        <span
          key={i}
          className={cn('absolute top-1/2 left-1/2 size-2.5 opacity-0', color, shape)}
          style={{ '--x': `${x * (size / 112)}px`, '--y': `${y * (size / 112)}px`, '--r': `${r}deg`, animation: `confetti 1.1s var(--ease-out-expo) ${0.75 + (i % 4) * 0.04}s both` } as CSSProperties}
        />
      ))}
      <svg viewBox="0 0 120 120" className="relative size-full">
        <circle cx="60" cy="60" r="54" className="fill-forest-50" />
        <circle
          cx="60"
          cy="60"
          r="54"
          fill="none"
          className="stroke-forest-500"
          strokeWidth="5"
          strokeLinecap="round"
          strokeDasharray="340"
          strokeDashoffset="340"
          transform="rotate(-90 60 60)"
          style={{ animation: 'draw 0.7s var(--ease-out-expo) 0.1s forwards' }}
        />
        <circle cx="60" cy="60" r="46" className="origin-center fill-forest-600 [transform-box:fill-box]" style={{ animation: 'scale-in 0.5s var(--ease-spring) 0.55s both' }} />
        <path
          d="M40 61l14 14 27-29"
          fill="none"
          stroke="white"
          strokeWidth="8"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeDasharray="64"
          strokeDashoffset="64"
          style={{ animation: 'draw 0.45s var(--ease-out-expo) 0.85s forwards' }}
        />
      </svg>
    </div>
  )
}

/**
 * A finished-step screen: the burst, a heading, a line or two, and what to
 * do next. Used after payment, after a trip request is sent, after a
 * password reset... Each part rises in just after the one before.
 */
export function SuccessPanel({ title, children, actions, className }: { title: ReactNode; children?: ReactNode; actions?: ReactNode; className?: string }) {
  return (
    <section className={cn('grid justify-items-center gap-4 py-8 text-center', className)} role="status">
      <SuccessBurst />
      <h1 className="animate-[fade-up_0.6s_var(--ease-out-expo)_0.9s_backwards] text-2xl font-bold text-ink-900 sm:text-3xl">{title}</h1>
      {children && <div className="grid max-w-md animate-[fade-up_0.6s_var(--ease-out-expo)_1s_backwards] gap-2 text-ink-500">{children}</div>}
      {actions && <div className="mt-2 flex animate-[fade-up_0.6s_var(--ease-out-expo)_1.1s_backwards] flex-wrap justify-center gap-2">{actions}</div>}
    </section>
  )
}
