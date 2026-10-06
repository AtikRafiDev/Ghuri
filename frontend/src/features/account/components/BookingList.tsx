import { CalendarDaysIcon, ChevronRightIcon, MoonIcon, TimerIcon, UsersIcon } from 'lucide-react'
import { Link } from 'react-router'
import type { MyBookingSummary } from '@/features/booking/api/bookings.api'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { formatMinutesSeconds, useSecondsLeft } from '@/features/booking/lib/useSecondsLeft'
import { cn } from '@/lib/utils'
import { formatTaka } from '@/shared/lib/format'

// "yyyy-MM-dd" read as UTC, like shared/lib/dates.ts, so the computer's time zone never moves a day.
const asDate = (date: string) => new Date(`${date}T00:00:00Z`)
const dayMonth = (date: string) => asDate(date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', timeZone: 'UTC' })
const dayMonthYear = (date: string) => asDate(date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })

/** "9 Oct → 11 Oct 2026" - the year once, unless the trip crosses New Year. */
function dateRange(start: string, end: string): string {
  return start.slice(0, 4) === end.slice(0, 4) ? `${dayMonth(start)} → ${dayMonthYear(end)}` : `${dayMonthYear(start)} → ${dayMonthYear(end)}`
}

/** The customer's bookings as a list of cards, each a link to that booking's page. */
export function BookingList({ bookings }: { bookings: MyBookingSummary[] }) {
  return (
    <ul className="stagger grid gap-3">
      {bookings.map((b) => (
        <li key={b.bookingNo}>
          <BookingRow booking={b} />
        </li>
      ))}
    </ul>
  )
}

/**
 * One booking: a calendar tile with the start day, the trip and its dates,
 * the booking number, and - on the right - its status and total. On a phone
 * the status and total drop to a second line under the title.
 */
function BookingRow({ booking: b }: { booking: MyBookingSummary }) {
  // Only a booking waiting for payment has a hold - the countdown just shows how long is left.
  const secondsLeft = useSecondsLeft(b.status === 1 ? b.holdExpiresAtUtc : null)
  const nights = b.nights === 0 ? 'Day trip' : `${b.nights} night${b.nights === 1 ? '' : 's'}`
  // Over or called off: the tile goes grey so live trips stand out.
  const faded = b.status === 4 || b.status === 5 || b.status === 6

  return (
    <Link
      to={`/account/bookings/${encodeURIComponent(b.bookingNo)}`}
      className="group grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-4 gap-y-3 rounded-2xl bg-card p-4 shadow-card ring-1 ring-ink-200/80 transition-[translate,box-shadow] duration-300 ease-(--ease-out-expo) hover:-translate-y-0.5 hover:shadow-lift hover:ring-forest-200 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none sm:grid-cols-[auto_minmax(0,1fr)_auto_auto] sm:px-5"
    >
      <span
        aria-hidden
        className={cn(
          'grid size-14 shrink-0 content-center justify-items-center gap-0.5 rounded-2xl ring-1 ring-inset',
          faded ? 'bg-ink-50 text-ink-500 ring-ink-200/80' : 'bg-forest-50 text-forest-700 ring-forest-100',
        )}
      >
        <span className="text-[0.625rem] leading-none font-bold tracking-wider uppercase">
          {asDate(b.startDate).toLocaleDateString('en-GB', { month: 'short', timeZone: 'UTC' })}
        </span>
        <span className="text-xl leading-none font-bold">{asDate(b.startDate).getUTCDate()}</span>
      </span>

      <div className="grid min-w-0 gap-1.5">
        <p className="line-clamp-2 font-semibold text-ink-900 transition-colors group-hover:text-forest-800 sm:line-clamp-1">{b.packageTitle ?? 'Custom trip'}</p>
        {/* Each fact keeps to one line; on a narrow phone whole facts wrap, never half a date. */}
        <p className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm whitespace-nowrap text-ink-500">
          <span className="flex items-center gap-1.5">
            <CalendarDaysIcon className="size-4 shrink-0 text-ink-400" />
            {dateRange(b.startDate, b.endDate)}
          </span>
          <span className="flex items-center gap-1.5">
            <MoonIcon className="size-4 shrink-0 text-ink-400" />
            {nights}
          </span>
          <span className="flex items-center gap-1.5">
            <UsersIcon className="size-4 shrink-0 text-ink-400" />
            {b.travellers} traveller{b.travellers === 1 ? '' : 's'}
          </span>
        </p>
        <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
          <span className="font-mono text-ink-400">{b.bookingNo}</span>
          {secondsLeft !== null &&
            (secondsLeft > 0 ? (
              <span className="flex items-center gap-1 font-semibold text-sun-700">
                <TimerIcon className="size-3.5" />
                Seats held · <span className="nums">{formatMinutesSeconds(secondsLeft)}</span> left to pay
              </span>
            ) : (
              <span className="flex items-center gap-1 font-medium text-ink-500">
                <TimerIcon className="size-3.5" />
                Time to pay has run out
              </span>
            ))}
        </p>
      </div>

      <div className="col-span-2 col-start-2 row-start-2 flex items-center justify-between gap-3 sm:col-span-1 sm:col-start-3 sm:row-start-1 sm:grid sm:justify-items-end sm:gap-2">
        <BookingStatusBadge status={b.status} />
        <span className="nums font-bold text-ink-900">{formatTaka(b.totalAmount)}</span>
      </div>

      <ChevronRightIcon className="col-start-3 row-start-1 size-5 text-ink-300 transition-[translate,color] duration-200 group-hover:translate-x-0.5 group-hover:text-forest-600 sm:col-start-4" />
    </Link>
  )
}
