import { useQuery } from '@tanstack/react-query'
import {
  ArrowRightIcon,
  ArrowUpRightIcon,
  CalendarCheckIcon,
  CompassIcon,
  HourglassIcon,
  LuggageIcon,
  MailIcon,
  PencilIcon,
  PhoneIcon,
  PlaneTakeoffIcon,
  UserRoundIcon,
  type LucideIcon,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuth } from '@/features/auth/useAuth'
import { myBookingsQuery, type MyBookingSummary } from '@/features/booking/api/bookings.api'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { AnimatedNumber } from '@/shared/components/AnimatedNumber'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { formatDate, todayInBangladesh } from '@/shared/lib/dates'
import { BookingList } from '../components/BookingList'

/** How many bookings the overview shows; "See all" leads to the rest. */
const recentCount = 3

/** /account - the customer's home: their trips at a glance, their latest bookings and their details. */
export function AccountPage() {
  const { user } = useAuth()
  const bookings = useQuery(myBookingsQuery)
  if (!user) return null // RequireAuth guarantees a user; this only satisfies TypeScript

  return (
    <div className="grid gap-6">
      <PageHeader
        title="My account"
        description="Your trips and details at a glance."
        actions={
          <Button asChild>
            <Link to="/packages">
              <CompassIcon />
              Find a trip
            </Link>
          </Button>
        }
      />

      {/* The tiles are worked out from the bookings list already loaded below - no extra request. */}
      {bookings.isPending && (
        <div className="grid grid-cols-2 gap-4 sm:gap-5 lg:grid-cols-[1.5fr_1fr_1fr]" role="status" aria-label="Loading your trips">
          <Skeleton className="col-span-2 h-44 rounded-3xl lg:col-span-1" />
          <Skeleton className="h-44 rounded-3xl" />
          <Skeleton className="h-44 rounded-3xl" />
        </div>
      )}
      {bookings.data && <TripTiles bookings={bookings.data} />}

      <section className="grid gap-4">
        <div className="flex items-center justify-between gap-4">
          <div className="grid gap-0.5">
            <h2 className="text-lg font-bold text-ink-900">Latest bookings</h2>
            {bookings.data && bookings.data.length > 0 && (
              <p className="text-sm text-ink-500">
                {bookings.data.length} booking{bookings.data.length === 1 ? '' : 's'} in all · newest first
              </p>
            )}
          </div>
          {bookings.data && bookings.data.length > recentCount && (
            <Button asChild variant="outline" size="sm">
              <Link to="/account/bookings">
                See all {bookings.data.length}
                <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
              </Link>
            </Button>
          )}
        </div>

        {bookings.isPending && (
          <div className="grid gap-3" role="status" aria-label="Loading your bookings">
            {Array.from({ length: recentCount }, (_, i) => (
              <Skeleton key={i} className="h-[5.5rem] rounded-2xl" />
            ))}
          </div>
        )}
        {bookings.isError && (
          <div className="grid justify-items-start gap-3">
            <FormAlert kind="error">{toAppError(bookings.error).message}</FormAlert>
            <Button variant="outline" size="sm" onClick={() => bookings.refetch()}>
              Try again
            </Button>
          </div>
        )}
        {bookings.data?.length === 0 && (
          <EmptyState icon={LuggageIcon} title="No bookings yet" text="Your trips will show up here once you book one.">
            <Button asChild>
              <Link to="/packages">
                Browse packages
                <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
              </Link>
            </Button>
          </EmptyState>
        )}
        {bookings.data && bookings.data.length > 0 && <BookingList bookings={bookings.data.slice(0, recentCount)} />}
      </section>

      <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
        <header className="flex items-center justify-between gap-4">
          <div className="grid min-w-0 gap-0.5">
            <h2 className="text-base font-bold text-ink-900">Your details</h2>
            <p className="text-xs text-ink-500">Your name, login number and email.</p>
          </div>
          <Button asChild variant="outline" size="sm">
            <Link to="/account/profile">
              <PencilIcon />
              Edit
            </Link>
          </Button>
        </header>
        {/* Each row spans both columns of the list (subgrid), so labels and values form two clean columns with a hairline between rows. */}
        <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-8 text-sm">
          <DetailRow icon={UserRoundIcon} label="Name">
            {user.fullName}
          </DetailRow>
          <DetailRow icon={PhoneIcon} label="Mobile">
            {user.phone}
          </DetailRow>
          <DetailRow icon={MailIcon} label="Email">
            {user.email ?? <span className="font-normal text-ink-400">Not added yet</span>}
          </DetailRow>
        </dl>
      </section>
    </div>
  )
}

function DetailRow({ icon: Icon, label, children }: { icon: LucideIcon; label: string; children: ReactNode }) {
  return (
    <div className="col-span-2 grid grid-cols-subgrid items-center border-t border-ink-100 py-3 first:border-t-0 first:pt-0 last:pb-0">
      <dt className="flex items-center gap-2 text-ink-500">
        <Icon className="size-4 text-ink-400" />
        {label}
      </dt>
      <dd className="truncate font-medium text-ink-900">{children}</dd>
    </div>
  )
}

/** "Today" / "Tomorrow" / "In 4 days" - counted in Bangladesh days, like the API. */
function whenLabel(date: string, today: string): string {
  const days = Math.round((Date.parse(date) - Date.parse(today)) / 86_400_000)
  return days <= 0 ? 'Today' : days === 1 ? 'Tomorrow' : `In ${days} days`
}

/**
 * Three tiles: the next confirmed trip (the dark green lead tile), how many
 * trips are coming up, and how many bookings still wait for payment.
 */
function TripTiles({ bookings }: { bookings: MyBookingSummary[] }) {
  const today = todayInBangladesh()
  // Confirmed (or partly paid) and not started yet - soonest first.
  const upcoming = bookings
    .filter((b) => (b.status === 2 || b.status === 3) && b.startDate >= today)
    .sort((a, b) => a.startDate.localeCompare(b.startDate))
  const next = upcoming[0]
  const waiting = bookings.filter((b) => b.status === 1).length

  return (
    // Phone and tablet: the lead tile across the top, the two counts side by side under it. Desktop: all three in a row.
    <div className="stagger grid grid-cols-2 gap-4 sm:gap-5 lg:grid-cols-[1.5fr_1fr_1fr]">
      <Link
        to={next ? `/account/bookings/${encodeURIComponent(next.bookingNo)}` : '/packages'}
        className="group brand-surface relative isolate col-span-2 flex min-h-44 flex-col justify-between gap-5 overflow-hidden rounded-3xl bg-gradient-to-br from-forest-600 via-forest-700 to-forest-900 p-5 text-white shadow-lift ring-1 ring-forest-800 transition-[translate,box-shadow] duration-300 ease-(--ease-out-expo) hover:-translate-y-1 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none sm:p-6 lg:col-span-1"
      >
        <div aria-hidden className="bg-topo absolute inset-0 -z-10 opacity-80" />
        <div aria-hidden className="absolute -top-12 -right-12 -z-10 size-40 rounded-full bg-sun-500/20 blur-2xl" />
        <div className="flex items-center justify-between gap-3">
          <span className="flex items-center gap-2 text-sm font-semibold text-forest-100">
            <PlaneTakeoffIcon className="size-4 text-sun-300" />
            Next trip
          </span>
          <TileArrow hero />
        </div>
        {next ? (
          <div className="grid gap-1.5">
            <span className="line-clamp-2 text-xl leading-snug font-bold">{next.packageTitle ?? 'Custom trip'}</span>
            <span className="flex flex-wrap items-center gap-x-2 text-sm text-forest-100/80">
              <span className="font-semibold text-sun-300">{whenLabel(next.startDate, today)}</span>
              <span aria-hidden>·</span>
              {formatDate(next.startDate)}
            </span>
          </div>
        ) : (
          <div className="grid gap-1.5">
            <span className="text-xl leading-snug font-bold">No trips planned yet</span>
            <span className="text-sm text-forest-100/80">Find your next escape - beaches, hills and more.</span>
          </div>
        )}
      </Link>

      <StatTile icon={CalendarCheckIcon} label="Upcoming trips" value={upcoming.length} note="Confirmed and on the calendar" />
      <StatTile
        icon={HourglassIcon}
        label="Waiting for payment"
        value={waiting}
        note={waiting > 0 ? 'Pay to keep your seats' : 'Nothing to pay right now'}
        attention={waiting > 0}
      />
    </div>
  )
}

/** A white tile: icon, name, the number (counting up) and a line of context - a shortcut into "My bookings". */
function StatTile({ icon: Icon, label, value, note, attention = false }: { icon: LucideIcon; label: string; value: number; note: string; attention?: boolean }) {
  return (
    <Link
      to="/account/bookings"
      className="group flex min-h-44 flex-col justify-between gap-5 rounded-3xl bg-card p-4 shadow-card ring-1 ring-ink-200/80 transition-[translate,box-shadow] duration-300 ease-(--ease-out-expo) hover:-translate-y-1 hover:shadow-lift focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none sm:p-6"
    >
      <div className="flex items-start justify-between gap-3">
        <span className={cn('flex size-10 items-center justify-center rounded-xl', attention ? 'bg-sun-50 text-sun-700' : 'bg-forest-50 text-forest-600')}>
          <Icon className="size-5" />
        </span>
        <TileArrow />
      </div>
      <div className="grid gap-1.5">
        <span className="text-sm font-semibold text-ink-500">{label}</span>
        <span className={cn('text-[2rem] leading-none font-bold tracking-tight', attention ? 'text-sun-700' : 'text-ink-900')}>
          <AnimatedNumber value={value} />
        </span>
        {/* Room for two lines on every tile, so the numbers line up even when one note wraps on a phone. */}
        <span className="line-clamp-2 min-h-8 text-xs text-ink-500">{note}</span>
      </div>
    </Link>
  )
}

/** The round arrow in a tile's corner; it turns and fills in when the tile is hovered. */
function TileArrow({ hero = false }: { hero?: boolean }) {
  return (
    <span
      aria-hidden
      className={cn(
        'flex size-8 shrink-0 items-center justify-center rounded-full ring-1 transition-[background-color,rotate,color] duration-300 group-hover:rotate-45',
        hero
          ? 'bg-white/10 text-white ring-white/20 group-hover:bg-white group-hover:text-forest-800'
          : 'bg-card text-ink-500 ring-ink-200 group-hover:bg-primary group-hover:text-white group-hover:ring-primary',
      )}
    >
      <ArrowUpRightIcon className="size-4" />
    </span>
  )
}
