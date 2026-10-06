import { ArrowRightIcon, CheckCheckIcon, HourglassIcon, RouteIcon, Undo2Icon, type LucideIcon } from 'lucide-react'
import { Link } from 'react-router'
import type { AdminDashboard } from '@/features/admin/operations/api/operations.api'
import { cn } from '@/lib/utils'
import { formatTaka } from '@/shared/lib/format'
import { DashboardCard } from './DashboardCard'

type Task = { key: string; count: number; title: string; detail: string; to: string; icon: LucideIcon; tone: 'sun' | 'clay' | 'forest' }

/**
 * "Needs attention" - the work waiting on staff right now, most urgent
 * first, each a shortcut to the list where it gets done. Shows a calm
 * "all caught up" when there's nothing to do.
 */
export function AttentionCard({ data }: { data: AdminDashboard }) {
  const tasks: Task[] = [
    {
      key: 'quotes',
      count: data.customTripsToQuote,
      title: `${data.customTripsToQuote} custom trip${data.customTripsToQuote === 1 ? '' : 's'} to quote`,
      detail: 'Customers are waiting for a price',
      to: '/admin/custom-trips',
      icon: RouteIcon,
      tone: 'sun' as const,
    },
    {
      key: 'refunds',
      count: data.refundsToProcess,
      title: `${data.refundsToProcess} refund${data.refundsToProcess === 1 ? '' : 's'} to send`,
      detail: `${formatTaka(data.refundsToProcessAmount)} owed to customers`,
      to: '/admin/refunds',
      icon: Undo2Icon,
      tone: 'clay' as const,
    },
    {
      key: 'pending',
      count: data.pendingPayments,
      title: `${data.pendingPayments} booking${data.pendingPayments === 1 ? '' : 's'} waiting to pay`,
      detail: 'Seats are held for 20 minutes',
      to: '/admin/bookings?status=1',
      icon: HourglassIcon,
      tone: 'forest' as const,
    },
  ].filter((t) => t.count > 0)

  return (
    <DashboardCard title="Needs attention" subtitle="Work waiting on the team right now">
      {tasks.length === 0 ? (
        <div className="grid flex-1 content-center justify-items-center gap-2 py-6 text-center">
          <span className="flex size-12 items-center justify-center rounded-2xl bg-forest-50 text-forest-600 ring-8 ring-forest-50/50">
            <CheckCheckIcon className="size-6" />
          </span>
          <p className="mt-2 font-semibold text-ink-900">All caught up</p>
          <p className="text-sm text-ink-500">No quotes, refunds or payments waiting.</p>
        </div>
      ) : (
        <ul className="stagger grid gap-2.5">
          {tasks.map((task) => {
            const Icon = task.icon
            return (
              <li key={task.key}>
                <Link
                  to={task.to}
                  className="group flex items-center gap-3 rounded-2xl border border-ink-200/80 p-3 transition-[border-color,background-color] duration-200 hover:border-forest-200 hover:bg-forest-50/60"
                >
                  <span
                    className={cn(
                      'flex size-10 shrink-0 items-center justify-center rounded-xl',
                      task.tone === 'sun' && 'bg-sun-50 text-sun-700',
                      task.tone === 'clay' && 'bg-clay-50 text-clay-600',
                      task.tone === 'forest' && 'bg-forest-50 text-forest-600',
                    )}
                  >
                    <Icon className="size-[18px]" />
                  </span>
                  <span className="grid min-w-0 flex-1">
                    <span className="truncate text-sm font-semibold text-ink-900">{task.title}</span>
                    <span className="truncate text-xs text-ink-500">{task.detail}</span>
                  </span>
                  <ArrowRightIcon className="size-4 shrink-0 text-ink-300 transition-[translate,color] duration-200 group-hover:translate-x-0.5 group-hover:text-forest-600" />
                </Link>
              </li>
            )
          })}
        </ul>
      )}
    </DashboardCard>
  )
}
