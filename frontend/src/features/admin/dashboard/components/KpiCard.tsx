import { ArrowUpRightIcon, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { cn } from '@/lib/utils'
import { AnimatedNumber } from '@/shared/components/AnimatedNumber'
import { monotonePath } from '../lib/chartMath'

/**
 * One headline number: its name, the value (counting up), a line of
 * context, and an arrow into the list behind it. tone="hero" is the dark
 * green lead card (one per dashboard) with a sparkline of its trend.
 */
export function KpiCard({
  label,
  value,
  format,
  footer,
  to,
  icon: Icon,
  tone = 'default',
  trend,
}: {
  label: string
  value: number
  format?: (n: number) => string
  footer?: ReactNode
  to: string
  icon: LucideIcon
  tone?: 'default' | 'hero' | 'attention'
  trend?: number[]
}) {
  const hero = tone === 'hero'
  return (
    <Link
      to={to}
      className={cn(
        'group relative isolate flex min-h-36 flex-col sm:min-h-44 justify-between gap-4 overflow-hidden rounded-3xl p-5 ring-1 transition-[translate,box-shadow] duration-300 ease-(--ease-out-expo) hover:-translate-y-1 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none sm:p-6',
        hero
          ? 'brand-surface bg-gradient-to-br from-forest-600 via-forest-700 to-forest-900 text-white shadow-lift ring-forest-800'
          : 'bg-card shadow-card ring-ink-200/80 hover:shadow-lift',
      )}
    >
      {hero && <div aria-hidden className="bg-topo absolute inset-0 -z-10 opacity-80" />}
      {hero && <div aria-hidden className="absolute -top-10 -right-10 -z-10 size-36 rounded-full bg-forest-400/30 blur-2xl" />}

      <div className="flex items-start justify-between gap-3">
        <span
          className={cn(
            'flex size-10 items-center justify-center rounded-xl',
            hero ? 'bg-white/12 text-white ring-1 ring-white/15' : tone === 'attention' ? 'bg-sun-50 text-sun-700' : 'bg-forest-50 text-forest-600',
          )}
        >
          <Icon className="size-5" />
        </span>
        {trend && trend.length > 1 && <Sparkline values={trend} />}
        <span
          className={cn(
            'flex size-8 items-center justify-center rounded-full ring-1 transition-[background-color,rotate,color] duration-300 group-hover:rotate-45',
            hero ? 'bg-white/10 text-white ring-white/20 group-hover:bg-white group-hover:text-forest-800' : 'bg-card text-ink-500 ring-ink-200 group-hover:bg-primary group-hover:text-white group-hover:ring-primary',
          )}
        >
          <ArrowUpRightIcon className="size-4" />
        </span>
      </div>

      <div className="flex items-end justify-between gap-3">
        <div className="grid min-w-0 gap-1.5">
          <span className={cn('text-sm font-semibold', hero ? 'text-forest-50' : 'text-ink-500')}>{label}</span>
          <span className={cn('text-[2rem] leading-none font-bold tracking-tight', hero ? 'text-white' : tone === 'attention' ? 'text-sun-700' : 'text-ink-900')}>
            <AnimatedNumber value={value} format={format} />
          </span>
          {footer && <div className="flex h-5 items-center text-xs">{footer}</div>}
        </div>
      </div>
    </Link>
  )
}

/** A tiny trend line for the hero card - shape only, no axis (the full chart is further down). */
function Sparkline({ values }: { values: number[] }) {
  const w = 96
  const h = 40
  const max = Math.max(...values, 1)
  const min = Math.min(...values, 0)
  const points = values.map((v, i) => ({ x: (i / (values.length - 1)) * w, y: h - 4 - ((v - min) / (max - min || 1)) * (h - 8) }))
  const line = monotonePath(points)
  return (
    <svg viewBox={`0 0 ${w} ${h}`} className="mx-2 h-10 min-w-0 flex-1 overflow-visible" aria-hidden>
      <path d={line} fill="none" className="stroke-sun-300" strokeWidth={2} strokeLinecap="round" pathLength={1} strokeDasharray={1} strokeDashoffset={1} style={{ animation: 'draw 1.2s var(--ease-out-expo) 0.3s forwards' }} />
      <circle cx={points.at(-1)!.x} cy={points.at(-1)!.y} r={3.5} className="fill-sun-300 stroke-forest-800" strokeWidth={2} />
    </svg>
  )
}
