import { TrophyIcon } from 'lucide-react'
import { Link } from 'react-router'
import type { AdminDashboard } from '@/features/admin/operations/api/operations.api'
import { cn } from '@/lib/utils'
import { EmptyState } from '@/shared/components/EmptyState'
import { formatTaka } from '@/shared/lib/format'
import { DashboardCard } from './DashboardCard'

/**
 * Best sellers of the last 30 days. One series (bookings per package), so
 * every bar is the same green - the length is the comparison; the number
 * sits beside it in text.
 */
export function TopPackagesCard({ data }: { data: AdminDashboard }) {
  const max = Math.max(...data.topPackages.map((p) => p.bookings), 1)
  return (
    <DashboardCard title="Top packages" subtitle="Most booked in the last 30 days">
      {data.topPackages.length === 0 ? (
        <EmptyState icon={TrophyIcon} title="No sales yet" text="Paid bookings from the last 30 days will rank here." className="border-0 bg-ink-50/60 py-10" />
      ) : (
        <ol className="grid gap-4">
          {data.topPackages.map((p, i) => (
            <li key={p.slug} className="grid gap-1.5">
              <div className="flex items-baseline justify-between gap-3 text-sm">
                <Link to={`/packages/${encodeURIComponent(p.slug)}`} target="_blank" className="flex min-w-0 items-center gap-2 font-semibold text-ink-900 hover:text-forest-700">
                  <span
                    className={cn(
                      'flex size-5 shrink-0 items-center justify-center rounded-md text-[0.6875rem] font-bold',
                      i === 0 ? 'bg-sun-500 text-forest-950' : 'bg-ink-100 text-ink-500',
                    )}
                  >
                    {i + 1}
                  </span>
                  <span className="truncate">{p.title}</span>
                </Link>
                <span className="nums shrink-0 text-xs text-ink-500">
                  <span className="font-semibold text-ink-900">{p.bookings}</span> · {formatTaka(p.amount)}
                </span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-forest-50">
                <div
                  className="h-full origin-left rounded-full bg-chart-1"
                  style={{ width: `${(p.bookings / max) * 100}%`, animation: `grow-x 1s var(--ease-out-expo) ${0.15 + i * 0.08}s both` }}
                />
              </div>
            </li>
          ))}
        </ol>
      )}
    </DashboardCard>
  )
}
