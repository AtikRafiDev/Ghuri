import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangleIcon,
  BadgeCheckIcon,
  CalendarDaysIcon,
  HourglassIcon,
  LockIcon,
  PhoneIcon,
  SearchXIcon,
  ShieldCheckIcon,
  StarIcon,
  TimerIcon,
  TimerOffIcon,
  UsersIcon,
  type LucideIcon,
} from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingKeys, bookingsApi, myBookingQuery, type MyBooking } from '../api/bookings.api'
import { CheckoutSteps } from '../components/CheckoutSteps'
import { PageMessage } from '../components/PageMessage'
import { rememberPaymentAttempt } from '../lib/checkoutSession'
import { formatMinutesSeconds, useSecondsLeft } from '../lib/useSecondsLeft'

/** How long a booking holds its seats (backend: the 20-minute hold) - a full countdown ring. */
const holdSeconds = 20 * 60

/** The ways to pay on SSLCommerz's page. */
const payMethods = ['bKash', 'Nagad', 'Rocket', 'Card']

/**
 * Checkout, step 2 (17-day plan, Day 9): the booking exists, its seats are
 * held - pay before the countdown ends. "Pay" asks the API for an
 * SSLCommerz payment page and sends the browser there. The booking is read
 * from the API, so a refresh (or coming back from the payment page) is safe.
 */
export function BookingPaymentPage() {
  const { bookingNo = '' } = useParams()
  useDocumentMeta({ title: `Pay for ${bookingNo}` })
  const booking = useQuery(myBookingQuery(bookingNo))

  if (booking.isPending) return <PageSpinner />

  if (booking.isError) {
    return toAppError(booking.error).status === 404 ? (
      <PageMessage icon={SearchXIcon} tone="sun" title="Booking not found" text="This booking doesn't exist, or it belongs to another account.">
        <Button asChild>
          <Link to="/packages">See packages</Link>
        </Button>
      </PageMessage>
    ) : (
      <PageMessage icon={AlertTriangleIcon} tone="clay" title="This booking couldn't be loaded" text={toAppError(booking.error).message}>
        <Button variant="outline" onClick={() => booking.refetch()}>
          Try again
        </Button>
      </PageMessage>
    )
  }

  return <PaymentStep booking={booking.data} />
}

function PaymentStep({ booking }: { booking: MyBooking }) {
  const queryClient = useQueryClient()
  const isPending = booking.status === 1
  const secondsLeft = useSecondsLeft(isPending ? booking.holdExpiresAtUtc : null)

  const pay = useMutation({
    mutationFn: () => bookingsApi.startPayment(booking.bookingNo),
    onSuccess: ({ paymentNo, paymentPageUrl }) => {
      // SSLCommerz brings the customer back with only the payment number - remember the booking.
      rememberPaymentAttempt(paymentNo, booking.bookingNo)
      window.location.assign(paymentPageUrl)
    },
    onError: (error) => {
      // The server says the time is up (or it's paid): reload, so the page shows why.
      if (['booking_hold_ended', 'booking_already_paid'].includes(toAppError(error).code))
        void queryClient.invalidateQueries({ queryKey: bookingKeys.mine(booking.bookingNo) })
    },
  })

  if (booking.status === 2) {
    return (
      <PageMessage icon={BadgeCheckIcon} title="This booking is paid" text={`Booking ${booking.bookingNo} is confirmed - your voucher is on its way by email.`}>
        <Button asChild variant="outline">
          <Link to={`/account/bookings/${encodeURIComponent(booking.bookingNo)}`}>View booking</Link>
        </Button>
      </PageMessage>
    )
  }

  // Expired or cancelled - or the countdown just reached 0 (the server will agree within a minute).
  if (!isPending || secondsLeft === 0) {
    return (
      <PageMessage
        icon={TimerOffIcon}
        tone="sun"
        title="Your seats were released"
        text={`The time to pay for booking ${booking.bookingNo} has run out. Nothing was charged - you can book the trip again.`}
      >
        {booking.packageSlug && (
          <Button asChild>
            <Link to={`/packages/${encodeURIComponent(booking.packageSlug)}`}>Book again</Link>
          </Button>
        )}
      </PageMessage>
    )
  }

  const redirecting = pay.isPending || pay.isSuccess

  return (
    <div className="grid gap-6">
      <CheckoutSteps current={2} className="max-w-md" />
      <PageHeader title="Pay for your booking" description="Your seats are reserved. Check the details, then pay before the timer runs out to confirm the trip." />

      {/* Laptop: details | pay card. Phone: the timer and the Pay button first. */}
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_23rem] lg:items-start lg:gap-8">
        <BookingDetails booking={booking} />

        <aside className="order-1 overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80 lg:sticky lg:top-24 lg:order-2">
          <div className="grid justify-items-center gap-4 px-5 pt-6 pb-5 text-center sm:px-6">
            {secondsLeft !== null && <Countdown secondsLeft={secondsLeft} />}
            {secondsLeft !== null && <HoldMessage secondsLeft={secondsLeft} />}
          </div>

          <div className="grid gap-4 border-t border-ink-100 bg-ink-50/70 p-5 sm:p-6">
            <div className="flex items-baseline justify-between gap-4">
              <span className="text-sm font-medium text-ink-500">Total to pay</span>
              <span className="text-2xl font-bold tracking-tight text-ink-900">{formatTaka(booking.totalAmount)}</span>
            </div>

            {pay.isError && !['booking_hold_ended', 'booking_already_paid'].includes(toAppError(pay.error).code) && (
              <FormAlert kind="error">{toAppError(pay.error).message}</FormAlert>
            )}

            <Button size="lg" className="w-full" disabled={redirecting} onClick={() => pay.mutate()}>
              {redirecting ? <Spinner /> : <LockIcon />}
              {redirecting ? 'Opening the payment page…' : `Pay ${formatTaka(booking.totalAmount)}`}
            </Button>

            {/* Trust row: where the money goes and how it can be paid. */}
            <div className="grid justify-items-center gap-2.5">
              <p className="flex items-center gap-1.5 text-xs text-ink-500">
                <ShieldCheckIcon className="size-4 shrink-0 text-forest-600" />
                You'll pay on SSLCommerz's secure page
              </p>
              <ul aria-label="Ways to pay" className="flex flex-wrap justify-center gap-1.5">
                {payMethods.map((method) => (
                  <li key={method} className="rounded-lg bg-card px-2.5 py-1 text-[0.6875rem] font-semibold text-ink-600 shadow-soft ring-1 ring-ink-200 ring-inset">
                    {method}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </aside>
      </div>
    </div>
  )
}

/** The booking being paid for: the trip, everyone on it, and the price snapshot. */
function BookingDetails({ booking }: { booking: MyBooking }) {
  return (
    <section className="order-2 grid gap-6 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6 lg:order-1">
      <header className="flex flex-wrap items-start justify-between gap-3">
        <div className="grid min-w-0 gap-1">
          <p className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Booking</p>
          <h2 className="text-lg font-bold text-ink-900">{booking.packageTitle ?? 'Custom trip'}</h2>
        </div>
        <span className="nums shrink-0 rounded-lg bg-ink-100 px-2.5 py-1 font-mono text-xs font-medium text-ink-700 ring-1 ring-ink-200 ring-inset">{booking.bookingNo}</span>
      </header>

      <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2.5 text-sm">
        <DetailRow icon={CalendarDaysIcon} label="Dates">
          {formatDate(booking.startDate)} → {formatDate(booking.endDate)} · {booking.nights} night{booking.nights === 1 ? '' : 's'}
        </DetailRow>
        <DetailRow icon={PhoneIcon} label="Contact">
          {booking.contactName} · {booking.contactPhone}
        </DetailRow>
      </dl>

      <div className="grid gap-3">
        <h3 className="flex items-center gap-2 text-sm font-bold text-ink-900">
          <UsersIcon className="size-4 text-ink-400" />
          Travellers
        </h3>
        <ul className="grid divide-y divide-ink-100 rounded-2xl ring-1 ring-ink-200/80">
          {booking.travellers.map((t, i) => (
            <li key={i} className="flex items-center gap-3 px-4 py-3 text-sm">
              <UserAvatar name={t.fullName} size="sm" />
              <span className="min-w-0 flex-1 truncate font-medium text-ink-900">{t.fullName}</span>
              {t.isLead && (
                <Badge variant="warning" className="h-5 px-2 text-[0.6875rem]">
                  <StarIcon className="fill-current" />
                  Lead
                </Badge>
              )}
              <span className="w-12 text-right text-ink-500">{t.type === 1 ? 'Adult' : t.type === 2 ? 'Child' : 'Infant'}</span>
            </li>
          ))}
        </ul>
      </div>

      <PriceLines booking={booking} />
    </section>
  )
}

/** A label | value row - the values line up in one column. */
function DetailRow({ icon: Icon, label, children }: { icon: LucideIcon; label: string; children: ReactNode }) {
  return (
    <>
      <dt className="flex items-center gap-2 text-ink-500">
        <Icon className="size-4 text-ink-400" />
        {label}
      </dt>
      <dd className="font-medium text-ink-900">{children}</dd>
    </>
  )
}

// The ring's colour: green with time to spare, amber under 5 minutes, terracotta in the last minute.
const ringTone = {
  forest: { stroke: 'stroke-forest-500', text: 'text-forest-600' },
  sun: { stroke: 'stroke-sun-500', text: 'text-sun-700' },
  clay: { stroke: 'stroke-clay-500', text: 'text-clay-600' },
}
const toneFor = (secondsLeft: number) => (secondsLeft < 60 ? 'clay' : secondsLeft < 5 * 60 ? 'sun' : 'forest')

/** "17:30 left to pay" in a ring that empties as the 20-minute hold runs down. */
function Countdown({ secondsLeft }: { secondsLeft: number }) {
  const tone = ringTone[toneFor(secondsLeft)]
  const share = Math.min(1, secondsLeft / holdSeconds)

  return (
    <div role="timer" aria-live="off" className="relative size-40">
      <svg viewBox="0 0 120 120" aria-hidden className="size-full -rotate-90">
        <circle cx="60" cy="60" r="52" fill="none" className="stroke-ink-100" strokeWidth="8" />
        <circle
          cx="60"
          cy="60"
          r="52"
          fill="none"
          className={tone.stroke}
          strokeWidth="8"
          strokeLinecap="round"
          pathLength={1}
          strokeDasharray={1}
          strokeDashoffset={1 - share}
          style={{ transition: 'stroke-dashoffset 1s linear, stroke 0.4s ease' }}
        />
      </svg>
      <div className="absolute inset-0 grid place-content-center justify-items-center gap-1">
        <TimerIcon aria-hidden className={cn('size-4', tone.text)} />
        {/* tabular figures: the digits change every second and must not jiggle. */}
        <span className="nums text-[2rem] leading-none font-bold tracking-tight text-ink-900">{formatMinutesSeconds(secondsLeft)}</span>
        <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">left to pay</span>
      </div>
    </div>
  )
}

/** The line under the ring - calm while there's time, a nudge under 5 minutes, a warning in the last minute. */
function HoldMessage({ secondsLeft }: { secondsLeft: number }) {
  const tone = toneFor(secondsLeft)
  if (tone === 'forest') {
    return <p className="max-w-64 text-sm text-ink-500">Your seats are held for you - pay before the timer runs out.</p>
  }
  return (
    <p
      className={cn(
        'flex w-full items-center gap-2.5 rounded-xl px-3.5 py-2.5 text-left text-sm font-medium ring-1 ring-inset',
        tone === 'sun' ? 'bg-sun-50 text-sun-700 ring-sun-100' : 'bg-clay-50 text-clay-700 ring-clay-100',
      )}
    >
      <HourglassIcon className="size-4 shrink-0" />
      {tone === 'sun' ? 'Under 5 minutes left - pay now to keep your seats.' : 'Less than a minute left - your seats are released when the timer ends.'}
    </p>
  )
}

/** The price snapshot taken when booking: later price changes on the package never change it. */
function PriceLines({ booking }: { booking: MyBooking }) {
  const lines = [
    { label: 'Adult', count: booking.adults, price: booking.adultPrice },
    { label: 'Child', count: booking.children, price: booking.childPrice },
    { label: 'Infant', count: booking.infants, price: booking.infantPrice },
  ].filter((line) => line.count > 0)

  return (
    <dl className="grid gap-3 border-t border-dashed border-ink-200 pt-5 text-sm">
      {lines.map((line) => (
        <div key={line.label} className="flex items-start justify-between gap-4">
          <dt className="grid gap-0.5">
            <span className="font-medium text-ink-900">{line.label}</span>
            <span className="nums text-xs text-ink-500">
              {formatTaka(line.price)} × {line.count}
            </span>
          </dt>
          <dd className="nums font-medium text-ink-900">{formatTaka(line.price * line.count)}</dd>
        </div>
      ))}
      <div className="mt-1 flex items-baseline justify-between gap-4 border-t border-ink-200 pt-4">
        <dt className="font-semibold text-ink-900">Total</dt>
        <dd className="text-xl font-bold tracking-tight text-forest-800">{formatTaka(booking.totalAmount)}</dd>
      </div>
    </dl>
  )
}
