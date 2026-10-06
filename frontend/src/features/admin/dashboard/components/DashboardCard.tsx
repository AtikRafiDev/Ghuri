import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/** The white panel every dashboard block sits in: title + subtitle on the left, an optional extra on the right. */
export function DashboardCard({
  title,
  subtitle,
  aside,
  children,
  className,
}: {
  title: ReactNode
  subtitle?: ReactNode
  aside?: ReactNode
  children: ReactNode
  className?: string
}) {
  return (
    <section className={cn('flex min-w-0 flex-col gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6', className)}>
      <header className="flex items-start justify-between gap-4">
        <div className="grid min-w-0 gap-0.5">
          <h2 className="text-base font-bold text-ink-900">{title}</h2>
          {subtitle && <p className="text-xs text-ink-500">{subtitle}</p>}
        </div>
        {aside && <div className="shrink-0">{aside}</div>}
      </header>
      {children}
    </section>
  )
}

/** "+18%" / "-6%" / "New" pill - green when the change is good news, terracotta when it isn't. Says what it compares against. */
export function DeltaPill({ value, against, upIsGood = true, inverted = false }: { value: number | null; against: string; upIsGood?: boolean; inverted?: boolean }) {
  // null = up from nothing: no percent to show, but it IS growth.
  const good = value === 0 ? null : (value === null || value > 0) === upIsGood
  const arrow = value === null || value > 0 ? '↑' : value < 0 ? '↓' : '→'
  return (
    <span className="inline-flex items-center gap-1.5 text-xs">
      <span
        className={cn(
          'inline-flex h-5 items-center rounded-full px-1.5 font-semibold',
          inverted
            ? 'bg-white/15 text-white'
            : good === null
              ? 'bg-ink-100 text-ink-600'
              : good
                ? 'bg-forest-50 text-forest-700'
                : 'bg-clay-50 text-clay-700',
        )}
      >
        {arrow} {value === null ? 'New' : `${Math.abs(value)}%`}
      </span>
      <span className={inverted ? 'text-forest-100/75' : 'text-ink-500'}>{against}</span>
    </span>
  )
}
