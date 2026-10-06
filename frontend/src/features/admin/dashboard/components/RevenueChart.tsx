import { useId, useState, type KeyboardEvent, type PointerEvent } from 'react'
import { cn } from '@/lib/utils'
import { formatTaka } from '@/shared/lib/format'
import { compactTaka, dayMonth, monotonePath, niceTicks, weekday } from '../lib/chartMath'
import { useElementWidth } from '../lib/useElementWidth'

type Day = { date: string; bookings: number; revenue: number }

const plotHeight = 210
const pad = { top: 12, right: 12, bottom: 30, left: 52 }

/**
 * Revenue per day as a smooth area: one series, so no legend - the card's
 * title names it. Hover (or focus + arrow keys) snaps a crosshair to the
 * nearest day and shows its numbers; a hidden table carries every value
 * for screen readers.
 */
export function RevenueChart({ days }: { days: Day[] }) {
  const [ref, width] = useElementWidth<HTMLDivElement>()
  const [active, setActive] = useState<number | null>(null)
  const gradientId = useId()

  const values = days.map((d) => d.revenue)
  // Round ticks, at least ৳1,000 tall - a quiet fortnight still gets a readable ৳0 / ৳250 / ৳500… axis.
  // A day where refunds beat income dips below ৳0: the same step continues downwards.
  const upper = niceTicks(Math.max(...values, 1000))
  const step = upper[1]
  const lowest = Math.min(...values, 0)
  const below = lowest < 0 ? Math.ceil(-lowest / step) : 0
  const ticks = [...Array.from({ length: below }, (_, i) => -(below - i) * step), ...upper]
  const yMin = ticks[0]
  const yMax = ticks[ticks.length - 1]
  const innerW = Math.max(0, width - pad.left - pad.right)
  const x = (i: number) => pad.left + (days.length < 2 ? innerW / 2 : (i * innerW) / (days.length - 1))
  const y = (v: number) => pad.top + plotHeight - ((v - yMin) / (yMax - yMin)) * plotHeight
  const baseline = y(Math.max(0, yMin))
  const points = days.map((d, i) => ({ x: x(i), y: y(d.revenue) }))
  const line = monotonePath(points)
  const area = `${line}L${x(days.length - 1)},${baseline}L${x(0)},${baseline}Z`
  const last = days.length - 1
  // Room for a "28 Sept" label is ~56px: skip days until that fits.
  const labelEvery = Math.max(2, Math.ceil(56 / (innerW / Math.max(1, days.length - 1))))

  const nearest = (clientX: number, rect: DOMRect) => {
    const i = Math.round(((clientX - rect.left - pad.left) / innerW) * (days.length - 1))
    return Math.min(last, Math.max(0, i))
  }
  const onPointerMove = (e: PointerEvent<SVGRectElement>) => setActive(nearest(e.clientX, e.currentTarget.ownerSVGElement!.getBoundingClientRect()))
  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'ArrowLeft') setActive((i) => Math.max(0, (i ?? last) - 1))
    else if (e.key === 'ArrowRight') setActive((i) => Math.min(last, (i ?? last) + 1))
    else return
    e.preventDefault()
  }

  const shown = active ?? null
  const tooltipLeft = shown === null ? 0 : x(shown)
  const flip = tooltipLeft > width - 170

  return (
    <div
      ref={ref}
      tabIndex={0}
      role="group"
      aria-label="Revenue per day, last 14 days. Use the arrow keys to read each day."
      onKeyDown={onKeyDown}
      onFocus={() => setActive((i) => i ?? last)}
      onBlur={() => setActive(null)}
      className="relative -mx-1 rounded-2xl outline-none focus-visible:ring-4 focus-visible:ring-ring/20"
      style={{ height: plotHeight + pad.top + pad.bottom }}
    >
      {width > 0 && (
        <svg width={width} height={plotHeight + pad.top + pad.bottom} className="block overflow-visible" aria-hidden>
          <defs>
            <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" className="[stop-color:var(--chart-1)] [stop-opacity:0.22]" />
              <stop offset="100%" className="[stop-color:var(--chart-1)] [stop-opacity:0]" />
            </linearGradient>
          </defs>

          {ticks.map((t) => (
            <g key={t}>
              <line x1={pad.left} x2={width - pad.right} y1={y(t)} y2={y(t)} className={t === 0 ? 'stroke-ink-300' : 'stroke-ink-100'} strokeWidth={1} />
              <text x={pad.left - 10} y={y(t)} dy="0.32em" textAnchor="end" className="nums fill-ink-400 text-[11px]">
                {compactTaka(t)}
              </text>
            </g>
          ))}

          {days.map((d, i) =>
            // Every 2nd day (3rd/4th on narrow screens), counted back from today, so "Today" is always labelled and labels never collide.
            (last - i) % labelEvery === 0 ? (
              <text key={d.date} x={x(i)} y={pad.top + plotHeight + 20} textAnchor="middle" className={cn('text-[11px]', i === last ? 'fill-forest-700 font-semibold' : 'fill-ink-400')}>
                {i === last ? 'Today' : dayMonth(d.date)}
              </text>
            ) : null,
          )}

          <path d={area} fill={`url(#${gradientId})`} className="animate-[fade-in_0.8s_ease-out_0.4s_backwards]" />
          <path
            d={line}
            fill="none"
            className="stroke-chart-1"
            strokeWidth={2.5}
            strokeLinecap="round"
            strokeLinejoin="round"
            pathLength={1}
            strokeDasharray={1}
            strokeDashoffset={1}
            style={{ animation: 'draw 1.4s var(--ease-out-expo) 0.1s forwards' }}
          />

          {/* Today's point, with a slow ripple: "this is now". */}
          <circle cx={x(last)} cy={y(days[last].revenue)} r={10} className="origin-center animate-[ring-ping_2s_ease-out_1.2s_infinite] fill-chart-1/30 [transform-box:fill-box]" />
          <circle cx={x(last)} cy={y(days[last].revenue)} r={5} className="fill-chart-1 stroke-white" strokeWidth={2.5} />

          {shown !== null && (
            <g className="pointer-events-none">
              <line x1={x(shown)} x2={x(shown)} y1={pad.top} y2={pad.top + plotHeight} className="stroke-ink-300" strokeWidth={1} />
              <circle cx={x(shown)} cy={y(days[shown].revenue)} r={6} className="fill-chart-1 stroke-white" strokeWidth={3} />
            </g>
          )}

          {/* The hover target: the whole plot, so the pointer only has to be near a day, not on the line. */}
          <rect
            x={pad.left}
            y={pad.top}
            width={innerW}
            height={plotHeight}
            fill="transparent"
            onPointerMove={onPointerMove}
            onPointerDown={onPointerMove}
            onPointerLeave={() => setActive(null)}
          />
        </svg>
      )}

      {shown !== null && (
        <div
          className="brand-surface pointer-events-none absolute top-0 z-10 grid min-w-36 animate-[fade-in_0.15s_ease-out] gap-0.5 rounded-xl bg-forest-950 px-3 py-2 text-white shadow-pop"
          style={{ left: tooltipLeft, transform: `translateX(${flip ? 'calc(-100% - 14px)' : '14px'})` }}
        >
          <span className="text-base font-bold">{formatTaka(days[shown].revenue)}</span>
          <span className="text-xs text-forest-100/80">
            {weekday(days[shown].date)}, {dayMonth(days[shown].date)} · {days[shown].bookings} booking{days[shown].bookings === 1 ? '' : 's'}
          </span>
        </div>
      )}

      <table className="sr-only">
        <caption>Revenue per day</caption>
        <thead>
          <tr>
            <th>Day</th>
            <th>Revenue</th>
            <th>Bookings</th>
          </tr>
        </thead>
        <tbody>
          {days.map((d) => (
            <tr key={d.date}>
              <td>{dayMonth(d.date)}</td>
              <td>{formatTaka(d.revenue)}</td>
              <td>{d.bookings}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
