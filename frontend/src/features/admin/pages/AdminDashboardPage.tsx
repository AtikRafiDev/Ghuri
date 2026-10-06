import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/features/auth/useAuth'
import { toAppError } from '@/shared/api/problem'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { dashboardQuery } from '../operations/api/operations.api'

/**
 * Admin home (17-day plan, Day 12: "today's bookings, revenue, pending
 * payments, upcoming trips") - the day at a glance, each card a way into
 * the list behind it. Refreshes every minute while open.
 */
export function AdminDashboardPage() {
  useDocumentMeta({ title: 'Dashboard' })
  const { user } = useAuth()
  const { data, isPending, isError, error, refetch } = useQuery({ ...dashboardQuery, refetchInterval: 60_000 })

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-muted-foreground">
          Welcome, {user?.fullName}.{data && ` Today is ${formatDate(data.today)}.`}
        </p>
      </div>

      {isError && (
        <div className="grid justify-items-start gap-2">
          <p className="text-destructive">{toAppError(error).message}</p>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Try again
          </Button>
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard loading={isPending} label="Bookings today" value={data?.bookingsToday} to="/admin/bookings" />
        <StatCard
          loading={isPending}
          label="Revenue today"
          value={data && formatTaka(data.revenueToday)}
          hint={data && `This month ${formatTaka(data.revenueThisMonth)}`}
          to="/admin/payments?status=3"
        />
        <StatCard
          loading={isPending}
          label="Waiting for payment"
          value={data?.pendingPayments}
          hint="Seats held 20 minutes"
          to="/admin/bookings?status=1"
        />
        <StatCard
          loading={isPending}
          label="Refunds to process"
          value={data?.refundsToProcess}
          hint={data && data.refundsToProcess > 0 ? `${formatTaka(data.refundsToProcessAmount)} owed` : 'Nothing owed'}
          to="/admin/refunds"
          urgent={!!data && data.refundsToProcess > 0}
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Upcoming trips</CardTitle>
          <CardDescription>
            Confirmed trips starting in the next 7 days{data && data.upcomingTripCount > 0 ? ` - ${data.upcomingTripCount} in all` : ''}.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isPending && <Skeleton className="h-24 w-full" />}
          {data?.upcomingTrips.length === 0 && <p className="text-sm text-muted-foreground">No trips in the next 7 days.</p>}
          {data && data.upcomingTrips.length > 0 && (
            <ul className="divide-y text-sm">
              {data.upcomingTrips.map((t) => (
                <li key={t.bookingNo} className="flex flex-wrap items-center justify-between gap-2 py-2">
                  <div>
                    <Link to={`/admin/bookings/${encodeURIComponent(t.bookingNo)}`} className="font-mono font-medium hover:underline">
                      {t.bookingNo}
                    </Link>{' '}
                    · {t.packageTitle ?? 'Custom trip'}
                    <div className="text-xs text-muted-foreground">
                      {t.contactName} · {t.contactPhone} · {t.travellers} traveller{t.travellers === 1 ? '' : 's'}
                    </div>
                  </div>
                  <span className="whitespace-nowrap">{formatDate(t.startDate)}</span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function StatCard({
  loading,
  label,
  value,
  hint,
  to,
  urgent,
}: {
  loading: boolean
  label: string
  value: string | number | undefined
  hint?: string
  to: string
  urgent?: boolean
}) {
  return (
    <Link to={to} className="rounded-xl transition-shadow hover:shadow-md focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none">
      <Card className={urgent ? 'h-full border-destructive/50' : 'h-full'}>
        <CardHeader>
          <CardDescription>{label}</CardDescription>
          {loading ? (
            <Skeleton className="h-8 w-24" />
          ) : (
            <CardTitle className={urgent ? 'text-2xl text-destructive tabular-nums' : 'text-2xl tabular-nums'}>{value ?? '-'}</CardTitle>
          )}
          {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
        </CardHeader>
      </Card>
    </Link>
  )
}
