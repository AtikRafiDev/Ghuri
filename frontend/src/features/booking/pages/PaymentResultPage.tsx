import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useAuth } from '@/features/auth/useAuth'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingKeys, bookingsApi, type PaymentResult } from '../api/bookings.api'
import { PageMessage } from '../components/PageMessage'
import { bookingForPayment } from '../lib/checkoutSession'

/** How often to ask "confirmed yet?", and when to stop asking. */
const pollEveryMs = 2_000
const giveUpAfterMs = 60_000

/** Settled = no longer waiting for SSLCommerz (status 1 initiated / 2 pending). */
const isSettled = (payment: PaymentResult | undefined) => payment !== undefined && payment.status > 2

/**
 * Where the API sends the customer after SSLCommerz's page:
 * /payment/result?payment=PAY100001&outcome=success|fail|cancel.
 *
 * The "outcome" in the address is only what SSLCommerz's page said - anyone
 * can type it. On "success" this page asks the API every 2 seconds until the
 * payment is really confirmed (SSLCommerz's validation API, Day 10), and
 * gives up waiting after a minute: by then the IPN or staff take over and
 * the customer gets an email. Seeing the result needs a login; the page
 * itself doesn't.
 */
export function PaymentResultPage() {
  useDocumentMeta({ title: 'Payment' })
  const [search] = useSearchParams()
  const location = useLocation()
  const { status: authStatus } = useAuth()
  const paymentNo = search.get('payment') ?? ''
  const outcome = search.get('outcome')
  const waitForConfirmation = outcome === 'success' && paymentNo !== ''

  const [gaveUp, setGaveUp] = useState(false)
  useEffect(() => {
    if (!waitForConfirmation) return
    const timer = setTimeout(() => setGaveUp(true), giveUpAfterMs)
    return () => clearTimeout(timer)
  }, [waitForConfirmation])

  const result = useQuery({
    queryKey: bookingKeys.payment(paymentNo),
    queryFn: () => bookingsApi.paymentResult(paymentNo),
    enabled: waitForConfirmation && authStatus === 'authenticated',
    refetchInterval: (query) => (gaveUp || isSettled(query.state.data) ? false : pollEveryMs),
    retry: 1,
  })

  const payment = result.data
  const bookingNo = payment?.bookingNo ?? (paymentNo ? bookingForPayment(paymentNo) : null)
  const viewBooking = (label: string) =>
    bookingNo && (
      <Button asChild>
        <Link to={`/checkout/${bookingNo}`}>{label}</Link>
      </Button>
    )
  const seePackages = (
    <Button asChild variant="outline">
      <Link to="/packages">See packages</Link>
    </Button>
  )

  if (outcome === 'cancel') {
    return (
      <PageMessage title="Payment cancelled" text="Nothing was charged. Your seats stay held until the countdown ends - you can still pay.">
        {viewBooking('Try again')}
      </PageMessage>
    )
  }

  if (!waitForConfirmation) {
    return (
      <PageMessage
        title="The payment didn't go through"
        text="Nothing was charged. You can try again while your seats are still held, or use another payment method."
      >
        {viewBooking('Try again') ?? seePackages}
      </PageMessage>
    )
  }

  // ---- outcome=success: wait for the real confirmation ----

  if (authStatus === 'anonymous') {
    return (
      <PageMessage
        title="Thank you - log in to see your booking"
        text="Your payment is being confirmed. Log in to see your booking's status - you'll also get an email once it's confirmed."
      >
        <Button asChild>
          <Link to="/login" state={{ from: location.pathname + location.search }}>
            Log in
          </Link>
        </Button>
      </PageMessage>
    )
  }

  if (payment?.status === 3 && payment.bookingStatus === 2) {
    return (
      <PageMessage
        title="Payment received - you're booked!"
        text={`Booking ${payment.bookingNo} is confirmed. Your voucher is on its way by email.`}
      >
        {viewBooking('View your booking')}
      </PageMessage>
    )
  }

  if (payment?.status === 3) {
    // Paid, but too late: the hold ended and the seats were taken, or the
    // booking was cancelled meanwhile. The money goes back in full.
    return (
      <PageMessage
        title="We received your payment, but couldn't keep your booking"
        text={`The time to pay had run out and the seats are no longer available. We'll refund ${formatTaka(payment.amount)} in full - our team will contact you.`}
      >
        {seePackages}
      </PageMessage>
    )
  }

  if (isSettled(payment)) {
    return (
      <PageMessage
        title="The payment didn't go through"
        text="Nothing was charged. You can try again while your seats are still held."
      >
        {viewBooking('Try again') ?? seePackages}
      </PageMessage>
    )
  }

  if (gaveUp || result.isError) {
    return (
      <PageMessage
        title="Still confirming your payment"
        text="This is taking longer than usual - there's nothing more you need to do. Your booking is confirmed as soon as the payment is verified, and we'll email you."
      >
        {viewBooking('View your booking')}
      </PageMessage>
    )
  }

  return (
    <PageMessage
      title="Thank you - we're confirming your payment"
      text="This usually takes a moment. Please keep this page open."
    >
      <span className="flex items-center gap-2 text-sm text-muted-foreground">
        <Spinner /> Checking with the payment service…
      </span>
    </PageMessage>
  )
}
