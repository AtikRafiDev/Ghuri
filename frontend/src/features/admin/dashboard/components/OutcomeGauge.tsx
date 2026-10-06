import { CircleCheckIcon, CircleXIcon, HourglassIcon, type LucideIcon } from 'lucide-react'
import type { BookingStatus } from '@/features/booking/api/bookings.api'
import { AnimatedNumber } from '@/shared/components/AnimatedNumber'

type Group = { key: string; label: string; statuses: BookingStatus[]; stroke: string; dot: string; icon: LucideIcon }

// Three outcomes (status colours: good / waiting / lost), each always shown with its word and count - never colour alone.
const groups: Group[] = [
  { key: 'paid', label: 'Paid', statuses: [2, 3, 4], stroke: 'stroke-chart-1', dot: 'bg-chart-1', icon: CircleCheckIcon },
  { key: 'waiting', label: 'Waiting to pay', statuses: [1], stroke: 'stroke-chart-2', dot: 'bg-chart-2', icon: HourglassIcon },
  { key: 'lost', label: 'Cancelled or expired', statuses: [5, 6], stroke: 'stroke-chart-3', dot: 'bg-chart-3', icon: CircleXIcon },
]

const cx = 110
const cy = 104
const r = 84
const gapDeg = 2.2

/** A point on the half circle: 0° = far left, 180° = far right, over the top. */
function point(deg: number) {
  const rad = (deg * Math.PI) / 180
  return { x: cx - r * Math.cos(rad), y: cy - r * Math.sin(rad) }
}

function arc(fromDeg: number, toDeg: number) {
  const a = point(fromDeg)
  const b = point(toDeg)
  return `M${a.x},${a.y}A${r},${r} 0 0 1 ${b.x},${b.y}`
}

/** Each group's arc along 0°-180° (null when it has no bookings), with a hairline gap wherever two meet. */
function layOut(counts: number[], total: number) {
  const visible = counts.filter((c) => c > 0).length
  const usable = 180 - gapDeg * Math.max(0, visible - 1)
  const segments: ((Group & { d: string; delay: number }) | null)[] = []
  let cursor = 0
  groups.forEach((g, i) => {
    if (counts[i] === 0) {
      segments.push(null)
      return
    }
    const span = (counts[i] / total) * usable
    segments.push({ ...g, d: arc(cursor, cursor + span), delay: 0.15 + i * 0.25 })
    cursor += span + gapDeg
  })
  return segments
}

/**
 * What became of the bookings made in the last 30 days, as a half-ring
 * gauge: the share that got paid in the middle, the three outcomes
 * around it with a small gap between, and a legend with the counts.
 */
export function OutcomeGauge({ mix }: { mix: { status: BookingStatus; count: number }[] }) {
  const counts = groups.map((g) => mix.filter((m) => g.statuses.includes(m.status)).reduce((sum, m) => sum + m.count, 0))
  const total = counts.reduce((a, b) => a + b, 0)
  const paidShare = total === 0 ? 0 : Math.round((counts[0] / total) * 100)

  const segments = layOut(counts, total)

  return (
    <div className="grid gap-5">
      <div className="relative mx-auto w-full max-w-64">
        <svg viewBox="0 0 220 116" className="w-full overflow-visible" aria-hidden>
          <path d={arc(0, 180)} fill="none" className="stroke-ink-100" strokeWidth={20} />
          {segments.map(
            (s) =>
              s && (
                <path
                  key={s.key}
                  d={s.d}
                  fill="none"
                  className={s.stroke}
                  strokeWidth={20}
                  pathLength={1}
                  strokeDasharray={1}
                  strokeDashoffset={1}
                  style={{ animation: `draw 0.9s var(--ease-out-expo) ${s.delay}s forwards` }}
                />
              ),
          )}
        </svg>
        <div className="absolute inset-x-0 bottom-0 grid justify-items-center">
          <span className="text-4xl font-bold tracking-tight text-ink-900">
            <AnimatedNumber value={paidShare} format={(n) => `${Math.round(n)}%`} />
          </span>
          <span className="text-xs font-medium text-ink-500">{total === 0 ? 'No bookings yet' : 'of bookings got paid'}</span>
        </div>
      </div>

      <ul className="grid gap-2">
        {groups.map((g, i) => {
          const Icon = g.icon
          return (
            <li key={g.key} className="flex items-center gap-3 rounded-xl px-2 py-1.5 text-sm transition-colors hover:bg-ink-50">
              <span className={`size-2.5 shrink-0 rounded-full ${g.dot}`} aria-hidden />
              <Icon className="size-4 shrink-0 text-ink-400" aria-hidden />
              <span className="flex-1 text-ink-600">{g.label}</span>
              <span className="nums font-semibold text-ink-900">{counts[i]}</span>
              <span className="nums w-10 text-right text-xs text-ink-400">{total === 0 ? '-' : `${Math.round((counts[i] / total) * 100)}%`}</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
