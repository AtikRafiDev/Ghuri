import { ChevronRightIcon, PlaneTakeoffIcon, UsersIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import type { AdminDashboard } from '@/features/admin/operations/api/operations.api'
import { EmptyState } from '@/shared/components/EmptyState'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { addDays, todayInBangladesh } from '@/shared/lib/dates'
import { dayMonth, weekday } from '../lib/chartMath'
import { DashboardCard } from './DashboardCard'

/** "Today" / "Tomorrow" / "In 4 days" - how soon, which is what staff plan around. */
function whenLabel(date: string): string {
  const today = todayInBangladesh()
  if (date === today) return 'Today'
  if (date === addDays(today, 1)) return 'Tomorrow'
  const days = Math.round((Date.parse(date) - Date.parse(today)) / 86_400_000)
  return `In ${days} days`
}

/** Confirmed trips starting in the next 7 days - who's travelling, how many, when. */
export function UpcomingTripsCard({ data }: { data: AdminDashboard }) {
  return (
    <DashboardCard
      title="Upcoming trips"
      subtitle={`Confirmed trips starting in the next 7 days${data.upcomingTripCount > 0 ? ` · ${data.upcomingTripCount} in all` : ''}`}
      aside={
        <Button asChild variant="outline" size="sm">
          <Link to="/admin/bookings?status=2">See all</Link>
        </Button>
      }
    >
      {data.upcomingTrips.length === 0 ? (
        <EmptyState icon={PlaneTakeoffIcon} title="No trips this week" text="Confirmed departures for the next 7 days will show up here." className="border-0 bg-ink-50/60 py-10" />
      ) : (
        <ul className="stagger -mx-2 grid">
          {data.upcomingTrips.map((t) => (
            <li key={t.bookingNo}>
              <Link
                to={`/admin/bookings/${encodeURIComponent(t.bookingNo)}`}
                className="group flex items-center gap-3 rounded-2xl px-2 py-2.5 transition-colors duration-200 hover:bg-forest-50/70"
              >
                <UserAvatar name={t.contactName} />
                <span className="grid min-w-0 flex-1">
                  <span className="truncate text-sm font-semibold text-ink-900">{t.contactName}</span>
                  <span className="truncate text-xs text-ink-500">
                    <span className="font-mono">{t.bookingNo}</span> · {t.packageTitle ?? 'Custom trip'}
                  </span>
                </span>
                <span className="hidden items-center gap-1 rounded-full bg-ink-100 px-2 py-1 text-xs font-medium text-ink-600 sm:flex">
                  <UsersIcon className="size-3.5" />
                  {t.travellers}
                </span>
                <span className="grid w-20 justify-items-end text-right">
                  <span className="text-xs font-semibold text-forest-700">{whenLabel(t.startDate)}</span>
                  <span className="text-xs text-ink-400">
                    {weekday(t.startDate)} {dayMonth(t.startDate)}
                  </span>
                </span>
                <ChevronRightIcon className="size-4 shrink-0 text-ink-300 transition-[translate,color] group-hover:translate-x-0.5 group-hover:text-forest-600" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </DashboardCard>
  )
}
