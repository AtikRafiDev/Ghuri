import { useQuery } from '@tanstack/react-query'
import { CalendarCheckIcon, HourglassIcon, PlusIcon, RefreshCwIcon, Undo2Icon, WalletIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { AttentionCard } from '../dashboard/components/AttentionCard'
import { BookingsWeekChart } from '../dashboard/components/BookingsWeekChart'
import { DashboardCard, DeltaPill } from '../dashboard/components/DashboardCard'
import { KpiCard } from '../dashboard/components/KpiCard'
import { NextDepartureCard } from '../dashboard/components/NextDepartureCard'
import { OutcomeGauge } from '../dashboard/components/OutcomeGauge'
import { RevenueChart } from '../dashboard/components/RevenueChart'
import { TopPackagesCard } from '../dashboard/components/TopPackagesCard'
import { UpcomingTripsCard } from '../dashboard/components/UpcomingTripsCard'
import { percentChange } from '../dashboard/lib/chartMath'
import { dashboardQuery } from '../operations/api/operations.api'

/** "Good morning" by the clock in Dhaka, where the team works. */
function greeting(): string {
  const hour = Number(new Date().toLocaleString('en-GB', { hour: 'numeric', hourCycle: 'h23', timeZone: 'Asia/Dhaka' }))
  return hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening'
}

const sum = (values: number[]) => values.reduce((a, b) => a + b, 0)

/**
 * Admin home (17-day plan, Day 12: "today's bookings, revenue, pending
 * payments, upcoming trips") - the day at a glance: four headline numbers,
 * the revenue trend, this week's bookings, how bookings turned out, what
 * needs doing, the next departure, and the best sellers. Refreshes every
 * minute; while it does, the last numbers stay on screen.
 */
export function AdminDashboardPage() {
  useDocumentMeta({ title: 'Dashboard' })
  const { user } = useAuth()
  const { data, isPending, isError, error, refetch, isFetching } = useQuery({ ...dashboardQuery, refetchInterval: 60_000 })

  const days = data?.last14Days ?? []
  const lastWeek = days.slice(-7)
  const weekBefore = days.slice(0, 7)
  const yesterday = days.at(-2)

  return (
    <div className="grid gap-6">
      <PageHeader
        title={`${greeting()}, ${user?.fullName.split(' ')[0] ?? 'there'}`}
        description={data ? `Here's how Ghuri is doing today, ${formatDate(data.today)}.` : 'Here’s how Ghuri is doing today.'}
        actions={
          <>
            <Button variant="outline" onClick={() => refetch()} disabled={isFetching} aria-label="Refresh the numbers">
              <RefreshCwIcon className={cn(isFetching && 'animate-spin')} />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            <Button asChild>
              <Link to="/admin/packages/new">
                <PlusIcon />
                Add package
              </Link>
            </Button>
          </>
        }
      />

      {isError && (
        <div className="grid justify-items-start gap-3">
          <FormAlert kind="error">{toAppError(error).message}</FormAlert>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Try again
          </Button>
        </div>
      )}

      {isPending && <DashboardSkeleton />}

      {data && (
        <div className="grid gap-5">
          <div className="stagger grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
            <KpiCard
              tone="hero"
              icon={WalletIcon}
              label="Revenue this month"
              value={data.revenueThisMonth}
              format={(n) => formatTaka(Math.round(n))}
              footer={<span className="text-forest-100/80">Today {formatTaka(data.revenueToday)}</span>}
              trend={days.map((d) => d.revenue)}
              to="/admin/payments?status=3"
            />
            <KpiCard
              icon={CalendarCheckIcon}
              label="Bookings today"
              value={data.bookingsToday}
              footer={<DeltaPill value={yesterday ? percentChange(data.bookingsToday, yesterday.bookings) : null} against="vs yesterday" />}
              to="/admin/bookings"
            />
            <KpiCard
              icon={HourglassIcon}
              label="Waiting for payment"
              value={data.pendingPayments}
              footer={<span className="text-ink-500">Seats held for 20 minutes</span>}
              to="/admin/bookings?status=1"
            />
            <KpiCard
              tone={data.refundsToProcess > 0 ? 'attention' : 'default'}
              icon={Undo2Icon}
              label="Refunds to send"
              value={data.refundsToProcess}
              footer={<span className="text-ink-500">{data.refundsToProcess > 0 ? `${formatTaka(data.refundsToProcessAmount)} owed` : 'Nothing owed'}</span>}
              to="/admin/refunds"
            />
          </div>

          {/* Refetch keeps the frame: the old numbers stay, dimmed a touch, until the new ones land. */}
          <div className={cn('grid gap-5 transition-opacity duration-300', isFetching && !isPending && 'opacity-80')}>
            <div className="grid gap-5 lg:grid-cols-3">
              <DashboardCard
                className="lg:col-span-2"
                title="Revenue"
                subtitle="Last 14 days · money received minus refunds sent"
                aside={
                  <div className="grid justify-items-end gap-1">
                    <span className="text-xl font-bold tracking-tight text-ink-900">{formatTaka(sum(lastWeek.map((d) => d.revenue)))}</span>
                    <DeltaPill value={percentChange(sum(lastWeek.map((d) => d.revenue)), sum(weekBefore.map((d) => d.revenue)))} against="vs week before" />
                  </div>
                }
              >
                <RevenueChart days={days} />
              </DashboardCard>
              <AttentionCard data={data} />
            </div>

            <div className="grid gap-5 md:grid-cols-2 lg:grid-cols-3">
              <DashboardCard
                title="Bookings this week"
                subtitle="New bookings per day"
                aside={
                  <div className="grid justify-items-end gap-1">
                    <span className="text-xl font-bold tracking-tight text-ink-900">{sum(lastWeek.map((d) => d.bookings))}</span>
                    <DeltaPill value={percentChange(sum(lastWeek.map((d) => d.bookings)), sum(weekBefore.map((d) => d.bookings)))} against="vs last week" />
                  </div>
                }
              >
                <BookingsWeekChart days={lastWeek} />
              </DashboardCard>
              <DashboardCard title="Booking outcomes" subtitle="Bookings made in the last 30 days">
                <OutcomeGauge mix={data.statusMix} />
              </DashboardCard>
              <div className="md:col-span-2 lg:col-span-1">
                <NextDepartureCard trip={data.upcomingTrips[0]} />
              </div>
            </div>

            <div className="grid gap-5 lg:grid-cols-[2fr_1fr]">
              <UpcomingTripsCard data={data} />
              <TopPackagesCard data={data} />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

/** First load only: the dashboard's shape in soft shimmering blocks, so nothing jumps when the numbers arrive. */
function DashboardSkeleton() {
  return (
    <div className="grid gap-5" aria-label="Loading the dashboard" role="status">
      <div className="grid gap-5 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-40 rounded-3xl" />
        ))}
      </div>
      <div className="grid gap-5 lg:grid-cols-3">
        <Skeleton className="h-80 rounded-3xl lg:col-span-2" />
        <Skeleton className="h-80 rounded-3xl" />
      </div>
      <div className="grid gap-5 lg:grid-cols-3">
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} className="h-72 rounded-3xl" />
        ))}
      </div>
    </div>
  )
}
