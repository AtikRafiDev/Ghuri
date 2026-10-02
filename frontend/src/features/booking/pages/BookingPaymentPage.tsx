import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CircleCheckIcon, LockIcon, TimerIcon } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingKeys, bookingsApi, myBookingQuery, type MyBooking } from '../api/bookings.api'
import { PageMessage } from '../components/PageMessage'
import { rememberPaymentAttempt } from '../lib/checkoutSession'
import { formatMinutesSeconds, useSecondsLeft } from '../lib/useSecondsLeft'

/**
 * Checkout, step 2 of 2 (17-day plan, Day 9): the booking exists, its seats
 * are held - pay before the countdown ends. "Pay" asks the API for an
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
      <PageMessage title="Booking not found" text="This booking doesn't exist, or it belongs to another account.">
        <Button asChild>
          <Link to="/packages">See packages</Link>
        </Button>
      </PageMessage>
    ) : (
      <PageMessage title="This booking couldn't be loaded" text={toAppError(booking.error).message}>
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
      <PageMessage title="This booking is paid" text={`Booking ${booking.bookingNo} is confirmed - your voucher is on its way by email.`}>
        <Button asChild variant="outline">
          <Link to="/account">My account</Link>
        </Button>
      </PageMessage>
    )
  }

  // Expired or cancelled - or the countdown just reached 0 (the server will agree within a minute).
  if (!isPending || secondsLeft === 0) {
    return (
      <PageMessage
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
    <div className="mx-auto grid max-w-xl gap-6">
      <div className="grid gap-1">
        <p className="text-sm font-medium text-muted-foreground">Step 2 of 2</p>
        <h1 className="text-2xl font-semibold tracking-tight">Pay for your booking</h1>
      </div>

      {secondsLeft !== null && <Countdown secondsLeft={secondsLeft} />}

      <section className="grid gap-4 rounded-xl border bg-card p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="grid gap-1">
            <h2 className="font-semibold">{booking.packageTitle ?? 'Custom trip'}</h2>
            <p className="text-sm text-muted-foreground">
              {formatDate(booking.startDate)} → {formatDate(booking.endDate)} · {booking.nights} night{booking.nights === 1 ? '' : 's'}
            </p>
          </div>
          <span className="shrink-0 rounded-md bg-muted px-2 py-1 font-mono text-xs">{booking.bookingNo}</span>
        </div>

        <ul className="grid gap-1 text-sm">
          {booking.travellers.map((t, i) => (
            <li key={i} className="flex justify-between gap-2">
              <span>{t.fullName}</span>
              <span className="text-muted-foreground">
                {t.type === 1 ? 'Adult' : t.type === 2 ? 'Child' : 'Infant'}
                {t.isLead && ' · lead'}
              </span>
            </li>
          ))}
        </ul>

        <Separator />
        <PriceLines booking={booking} />
      </section>

      {pay.isError && !['booking_hold_ended', 'booking_already_paid'].includes(toAppError(pay.error).code) && (
        <FormAlert kind="error">{toAppError(pay.error).message}</FormAlert>
      )}

      <div className="grid gap-2">
        <Button size="lg" className="h-11 text-base" disabled={redirecting} onClick={() => pay.mutate()}>
          {redirecting ? <Spinner /> : <LockIcon />}
          {redirecting ? 'Opening the payment page…' : `Pay ${formatTaka(booking.totalAmount)}`}
        </Button>
        <p className="text-center text-sm text-muted-foreground">
          You'll pay on SSLCommerz's secure page - bKash, Nagad, Rocket or card.
        </p>
      </div>
    </div>
  )
}

/** "Seats held for 14:59" - amber in the last 2 minutes. */
function Countdown({ secondsLeft }: { secondsLeft: number }) {
  const hurry = secondsLeft <= 120
  return (
    <div
      role="timer"
      aria-live="off"
      className={cn(
        'flex items-center gap-2 rounded-lg border p-3 text-sm',
        hurry ? 'border-amber-300 bg-amber-50 text-amber-900 dark:border-amber-800 dark:bg-amber-950 dark:text-amber-200' : 'bg-muted/50',
      )}
    >
      <TimerIcon className="size-4 shrink-0" />
      <span>
        Your seats are held for <strong className="font-mono tabular-nums">{formatMinutesSeconds(secondsLeft)}</strong> - pay before
        then.
      </span>
    </div>
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
    <dl className="grid gap-1.5 text-sm">
      {lines.map((line) => (
        <div key={line.label} className="flex justify-between gap-4">
          <dt className="text-muted-foreground">
            {line.label} · {formatTaka(line.price)} × {line.count}
          </dt>
          <dd className="tabular-nums">{formatTaka(line.price * line.count)}</dd>
        </div>
      ))}
      <div className="mt-1 flex items-center justify-between gap-4 text-base font-semibold">
        <dt className="flex items-center gap-1.5">
          <CircleCheckIcon className="size-4 text-muted-foreground" />
          Total
        </dt>
        <dd className="tabular-nums">{formatTaka(booking.totalAmount)}</dd>
      </div>
    </dl>
  )
}
