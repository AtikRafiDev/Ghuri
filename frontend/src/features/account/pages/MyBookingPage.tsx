import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon, ArrowRightIcon, PlaneIcon, TimerIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { myBookingQuery, type MyBooking, type RefundStatus, type TravellerType } from '@/features/booking/api/bookings.api'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { formatMinutesSeconds, useSecondsLeft } from '@/features/booking/lib/useSecondsLeft'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { BookingDocuments } from '../components/BookingDocuments'
import { CancelBookingDialog } from '../components/CancelBookingDialog'

const refundLabels: Record<RefundStatus, string> = {
  1: "requested - our team will send it to you within a few working days",
  2: 'approved - on its way',
  3: 'not approved - please contact us',
  4: 'being sent',
  5: 'refunded',
  6: 'failed - our team will contact you',
}

const travellerTypes: Record<TravellerType, string> = { 1: 'Adult', 2: 'Child', 3: 'Infant' }
const bookingTypes: Record<MyBooking['bookingType'], string> = { 1: 'Fixed departure', 2: 'Flexible stay', 3: 'Custom trip' }

/** The white panel every block on this page sits in. */
const panel = 'grid content-start gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6'

/** /account/bookings/:bookingNo - one booking: the trip, who's going, what was paid, cancel (17-day plan, Day 11). */
export function MyBookingPage() {
  const { bookingNo = '' } = useParams()
  useDocumentMeta({ title: `Booking ${bookingNo}` })
  const booking = useQuery(myBookingQuery(bookingNo))

  if (booking.isPending) return <PageSpinner />

  if (booking.isError) {
    const error = toAppError(booking.error)
    return (
      <PageMessage
        title={error.status === 404 ? 'Booking not found' : "This booking couldn't be loaded"}
        text={error.status === 404 ? "This booking doesn't exist, or it belongs to another account." : error.message}
      >
        <Button asChild variant="outline">
          <Link to="/account/bookings">My bookings</Link>
        </Button>
      </PageMessage>
    )
  }

  return <BookingDetails booking={booking.data} />
}

function BookingDetails({ booking }: { booking: MyBooking }) {
  const [cancelOpen, setCancelOpen] = useState(false)
  // Only while waiting for payment: how long the seats stay held. The server decides - this only counts down.
  const secondsLeft = useSecondsLeft(booking.status === 1 ? booking.holdExpiresAtUtc : null)
  const nights = booking.nights === 0 ? 'day trip' : `${booking.nights} night${booking.nights === 1 ? '' : 's'}`

  return (
    <div className="grid gap-6">
      <PageHeader
        variant="display"
        eyebrow={
          <Button asChild variant="ghost" size="sm" className="-ml-3">
            <Link to="/account/bookings">
              <ArrowLeftIcon />
              My bookings
            </Link>
          </Button>
        }
        title={booking.packageTitle ?? 'Custom trip'}
        description={`${formatDate(booking.startDate)} → ${formatDate(booking.endDate)} · ${nights}`}
        actions={
          <>
            <span className="inline-flex h-6 items-center rounded-full bg-ink-100 px-2.5 font-mono text-xs font-medium text-ink-700 ring-1 ring-ink-200 ring-inset">
              {booking.bookingNo}
            </span>
            <BookingStatusBadge status={booking.status} />
          </>
        }
      />

      {booking.status === 1 && (
        <div className="flex flex-wrap items-center gap-x-4 gap-y-3 rounded-2xl bg-sun-50 p-4 ring-1 ring-sun-100 ring-inset sm:px-5">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-sun-100 text-sun-700">
            <TimerIcon className="size-5" />
          </span>
          <div className="grid min-w-0 flex-1 basis-56 gap-0.5">
            <p className="font-semibold text-ink-900">Not paid yet</p>
            <p className="text-sm text-sun-700">
              Your seats are held only for a short time
              {secondsLeft !== null && secondsLeft > 0 && (
                <>
                  {' '}
                  - <strong role="timer" className="nums font-semibold">{formatMinutesSeconds(secondsLeft)}</strong> left
                </>
              )}
              .
            </p>
          </div>
          <Button asChild>
            <Link to={`/checkout/${encodeURIComponent(booking.bookingNo)}`}>
              Pay now
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </div>
      )}

      {/* A state that stays true while the page is open, so it stays in the page (not a toast). */}
      {booking.refund && (
        <FormAlert kind={booking.refund.status === 3 || booking.refund.status === 6 ? 'error' : 'success'}>
          Refund {booking.refund.refundNo} of {formatTaka(booking.refund.amount)}: {refundLabels[booking.refund.status]}.
        </FormAlert>
      )}

      <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1.55fr)_minmax(0,1fr)]">
        <div className="grid gap-5">
          <TripSummary booking={booking} nights={nights} />
          <Travellers booking={booking} />
        </div>

        <div className="grid gap-5">
          <PriceBreakdown booking={booking} />
          <BookingDocuments booking={booking} />
          {booking.cancellation.canCancel && (
            <section className={panel}>
              <SectionHeader title="Cancel this booking" subtitle="By our cancellation policy" />
              <p className="text-sm text-ink-600">
                {booking.paidAmount > 0 ? (
                  <>
                    Cancelling now ({booking.cancellation.daysBeforeStart} days before the trip) gives back{' '}
                    <strong className="font-semibold text-ink-900">{formatTaka(booking.cancellation.refundAmount)}</strong> -{' '}
                    {booking.cancellation.refundPercent}% of what you paid.
                  </>
                ) : (
                  'Nothing has been paid yet - cancelling costs nothing.'
                )}
              </p>
              <Button variant="destructive" className="w-full" onClick={() => setCancelOpen(true)}>
                Cancel booking
              </Button>
              <CancelBookingDialog booking={booking} open={cancelOpen} onOpenChange={setCancelOpen} />
            </section>
          )}
        </div>
      </div>
    </div>
  )
}

function SectionHeader({ title, subtitle, aside }: { title: string; subtitle?: ReactNode; aside?: ReactNode }) {
  return (
    <header className="flex items-center justify-between gap-4">
      <div className="grid min-w-0 gap-0.5">
        <h2 className="text-base font-bold text-ink-900">{title}</h2>
        {subtitle && <p className="text-xs text-ink-500">{subtitle}</p>}
      </div>
      {aside}
    </header>
  )
}

/** "13 Oct 2026" over "Tuesday" - one end of the trip. */
function TripEnd({ label, date, align = 'start' }: { label: string; date: string; align?: 'start' | 'end' }) {
  const day = new Date(`${date}T00:00:00Z`)
  return (
    <div className={align === 'end' ? 'grid justify-items-end gap-0.5 text-right' : 'grid gap-0.5'}>
      <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">{label}</span>
      <span className="font-bold text-ink-900">{day.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })}</span>
      <span className="text-xs text-ink-500">{day.toLocaleDateString('en-GB', { weekday: 'long', timeZone: 'UTC' })}</span>
    </div>
  )
}

/** The trip from start to end (a dashed route between the two days), then who to contact about it. */
function TripSummary({ booking, nights }: { booking: MyBooking; nights: string }) {
  const rows: [string, ReactNode][] = [
    ['Contact', booking.contactName],
    ['Mobile', booking.contactPhone],
    ['Email', booking.contactEmail ?? <span className="font-normal text-ink-400">-</span>],
  ]
  if (booking.specialRequest) rows.push(['Request', <span className="whitespace-pre-line">{booking.specialRequest}</span>])
  if (booking.cancelledAtUtc) rows.push(['Cancelled', formatDateTime(booking.cancelledAtUtc)])

  return (
    <section className={panel}>
      <SectionHeader title="Trip summary" subtitle={bookingTypes[booking.bookingType]} />
      <div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 rounded-2xl bg-ink-50 p-4 ring-1 ring-ink-200/60 ring-inset sm:gap-5 sm:px-5">
        <TripEnd label="Starts" date={booking.startDate} />
        <div aria-hidden className="grid justify-items-center gap-1.5">
          <div className="flex w-full items-center gap-1.5 text-forest-600">
            <span className="h-0 flex-1 border-t-2 border-dashed border-ink-300" />
            <PlaneIcon className="size-4 shrink-0 rotate-45" />
            <span className="h-0 flex-1 border-t-2 border-dashed border-ink-300" />
          </div>
          <span className="rounded-full bg-card px-2.5 py-0.5 text-xs font-semibold text-forest-700 ring-1 ring-ink-200">{nights}</span>
        </div>
        <TripEnd label="Ends" date={booking.endDate} align="end" />
      </div>
      {/* Each row spans both columns (subgrid): labels and values line up, with a hairline between rows. */}
      <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-8 text-sm">
        {rows.map(([label, value]) => (
          <div key={label} className="col-span-2 grid grid-cols-subgrid items-baseline border-t border-ink-100 py-2.5 first:border-t-0 first:pt-0 last:pb-0">
            <dt className="text-ink-500">{label}</dt>
            <dd className="min-w-0 font-medium break-words text-ink-900">{value}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}

/** "2 adults · 1 child" - the count by type, for the travellers card's subtitle. */
function travellerCounts(b: MyBooking): string {
  return [
    b.adults > 0 && `${b.adults} adult${b.adults === 1 ? '' : 's'}`,
    b.children > 0 && `${b.children} ${b.children === 1 ? 'child' : 'children'}`,
    b.infants > 0 && `${b.infants} infant${b.infants === 1 ? '' : 's'}`,
  ]
    .filter(Boolean)
    .join(' · ')
}

function Travellers({ booking }: { booking: MyBooking }) {
  return (
    <section className={panel}>
      <SectionHeader title="Travellers" subtitle={travellerCounts(booking)} />
      <ul className="grid divide-y divide-ink-100">
        {booking.travellers.map((t, i) => (
          <li key={i} className="flex items-center gap-3 py-2.5 first:pt-0 last:pb-0">
            <UserAvatar name={t.fullName} size="sm" />
            <span className="min-w-0 flex-1 truncate text-sm font-medium text-ink-900">{t.fullName}</span>
            {t.isLead && <Badge variant="secondary">Lead</Badge>}
            <span className="w-14 text-right text-sm text-ink-500">{travellerTypes[t.type]}</span>
          </li>
        ))}
      </ul>
    </section>
  )
}

/**
 * The price snapshot taken when booking (later price changes never touch
 * it). A package booking's per-person prices add up to the total exactly; a
 * custom trip has one quoted total and no per-person prices, so it shows
 * just the total.
 */
function PriceBreakdown({ booking }: { booking: MyBooking }) {
  const lines = [
    { label: 'Adult', count: booking.adults, price: booking.adultPrice },
    { label: 'Child', count: booking.children, price: booking.childPrice },
    { label: 'Infant', count: booking.infants, price: booking.infantPrice },
  ].filter((line) => line.count > 0)
  const explainsTotal = lines.reduce((sum, line) => sum + line.price * line.count, 0) === booking.totalAmount

  return (
    <section className={panel}>
      <SectionHeader title="Price" subtitle="Fixed when you booked" />
      <dl className="grid gap-2.5 text-sm">
        {explainsTotal &&
          lines.map((line) => (
            <div key={line.label} className="flex items-baseline justify-between gap-4">
              <dt className="text-ink-500">
                {line.label} <span className="nums text-ink-400">· {formatTaka(line.price)} × {line.count}</span>
              </dt>
              <dd className="nums font-medium text-ink-900">{formatTaka(line.price * line.count)}</dd>
            </div>
          ))}
        <div className="flex items-baseline justify-between gap-4 border-t border-ink-100 pt-3 text-base font-bold text-ink-900">
          <dt>Total</dt>
          <dd className="nums">{formatTaka(booking.totalAmount)}</dd>
        </div>
        <div className="flex items-baseline justify-between gap-4">
          <dt className="text-ink-500">Paid</dt>
          <dd className={booking.paidAmount > 0 ? 'nums font-semibold text-forest-700' : 'nums font-medium text-ink-500'}>{formatTaka(booking.paidAmount)}</dd>
        </div>
      </dl>
    </section>
  )
}
