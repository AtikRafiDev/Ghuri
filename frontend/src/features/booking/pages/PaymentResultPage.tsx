import { useQuery } from '@tanstack/react-query'
import { ArrowRightIcon, CircleXIcon, HourglassIcon, LogInIcon, RotateCcwIcon, ShieldCheckIcon, Undo2Icon } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useAuth } from '@/features/auth/useAuth'
import { SuccessPanel } from '@/shared/components/SuccessBurst'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingKeys, bookingsApi, type PaymentResult } from '../api/bookings.api'
import { CheckoutSteps } from '../components/CheckoutSteps'
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
  // "Try again" goes back to paying; "View your booking" to the booking in My account.
  const bookingLink = (label: string, to: 'pay' | 'account') =>
    bookingNo && (
      <Button asChild>
        <Link to={to === 'pay' ? `/checkout/${bookingNo}` : `/account/bookings/${bookingNo}`}>
          {to === 'pay' && <RotateCcwIcon />}
          {label}
        </Link>
      </Button>
    )
  const seePackages = (
    <Button asChild variant="outline">
      <Link to="/packages">See packages</Link>
    </Button>
  )

  if (outcome === 'cancel') {
    return (
      <ResultFrame step={2}>
        <PageMessage
          icon={CircleXIcon}
          tone="clay"
          title="Payment cancelled"
          text="Nothing was charged. Your seats stay held until the countdown ends - you can still pay."
        >
          {bookingLink('Try again', 'pay')}
        </PageMessage>
      </ResultFrame>
    )
  }

  if (!waitForConfirmation) {
    return (
      <ResultFrame step={2}>
        <PageMessage
          icon={CircleXIcon}
          tone="clay"
          title="The payment didn't go through"
          text="Nothing was charged. You can try again while your seats are still held, or use another payment method."
        >
          {bookingLink('Try again', 'pay') ?? seePackages}
        </PageMessage>
      </ResultFrame>
    )
  }

  // ---- outcome=success: wait for the real confirmation ----

  if (authStatus === 'anonymous') {
    return (
      <ResultFrame step={2}>
        <PageMessage
          icon={LogInIcon}
          title="Thank you - log in to see your booking"
          text="Your payment is being confirmed. Log in to see your booking's status - you'll also get an email once it's confirmed."
        >
          <Button asChild>
            <Link to="/login" state={{ from: location.pathname + location.search }}>
              Log in
            </Link>
          </Button>
        </PageMessage>
      </ResultFrame>
    )
  }

  if (payment?.status === 3 && payment.bookingStatus === 2) {
    return (
      <ResultFrame step={3}>
        <SuccessPanel
          title="Payment received - you're booked!"
          actions={
            <>
              {bookingLink('View your booking', 'account')}
              <Button asChild variant="outline">
                <Link to="/account/bookings">My bookings</Link>
              </Button>
              <Button asChild variant="ghost">
                <Link to="/">
                  Back to home
                  <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
                </Link>
              </Button>
            </>
          }
        >
          <p>Your booking is confirmed and your voucher is on its way by email.</p>
          {/* A small receipt: what was booked and what was paid. */}
          <dl className="mt-3 grid gap-2.5 rounded-2xl bg-card p-4 text-left text-sm shadow-card ring-1 ring-ink-200/80 sm:min-w-80">
            <div className="flex items-center justify-between gap-6">
              <dt className="text-ink-500">Booking</dt>
              <dd className="font-mono font-semibold text-ink-900">{payment.bookingNo}</dd>
            </div>
            <div className="flex items-center justify-between gap-6">
              <dt className="text-ink-500">Payment</dt>
              <dd className="font-mono text-ink-700">{payment.paymentNo}</dd>
            </div>
            <div className="flex items-baseline justify-between gap-6 border-t border-dashed border-ink-200 pt-3">
              <dt className="font-semibold text-ink-900">Amount paid</dt>
              <dd className="text-lg leading-tight font-bold text-forest-700">{formatTaka(payment.amount)}</dd>
            </div>
          </dl>
        </SuccessPanel>
      </ResultFrame>
    )
  }

  if (payment?.status === 3) {
    // Paid, but too late: the hold ended and the seats were taken, or the
    // booking was cancelled meanwhile. The money goes back in full.
    return (
      <ResultFrame step={2}>
        <PageMessage
          icon={Undo2Icon}
          tone="sun"
          title="We received your payment, but couldn't keep your booking"
          text={`The time to pay had run out and the seats are no longer available. We'll refund ${formatTaka(payment.amount)} in full - our team will contact you.`}
        >
          {seePackages}
        </PageMessage>
      </ResultFrame>
    )
  }

  if (isSettled(payment)) {
    return (
      <ResultFrame step={2}>
        <PageMessage icon={CircleXIcon} tone="clay" title="The payment didn't go through" text="Nothing was charged. You can try again while your seats are still held.">
          {bookingLink('Try again', 'pay') ?? seePackages}
        </PageMessage>
      </ResultFrame>
    )
  }

  if (gaveUp || result.isError) {
    return (
      <ResultFrame step={2}>
        <PageMessage
          icon={HourglassIcon}
          tone="sun"
          title="Still confirming your payment"
          text="This is taking longer than usual - there's nothing more you need to do. Your booking is confirmed as soon as the payment is verified, and we'll email you."
        >
          {bookingLink('View your booking', 'account')}
        </PageMessage>
      </ResultFrame>
    )
  }

  return (
    <ResultFrame step={2}>
      <PageMessage visual={<Confirming />} title="Thank you - we're confirming your payment" text="This usually takes a moment. Please keep this page open.">
        <span role="status" className="flex items-center gap-2 rounded-full bg-forest-50 px-4 py-2 text-sm font-medium text-forest-700 ring-1 ring-forest-100 ring-inset">
          <Spinner /> Checking with the payment service…
        </span>
      </PageMessage>
    </ResultFrame>
  )
}

/** Every outcome sits under the booking steps: still on "Payment", or all ticked once confirmed. */
function ResultFrame({ step, children }: { step: 2 | 3; children: ReactNode }) {
  return (
    <div className="grid gap-6 sm:gap-8">
      <CheckoutSteps current={step} className="mx-auto max-w-md" />
      {children}
    </div>
  )
}

/** The calm "please wait" mark: a shield in a slowly turning ring, breathing outward. Decorative only. */
function Confirming() {
  return (
    <span aria-hidden className="relative flex size-20 items-center justify-center">
      <span className="absolute inset-0 animate-[ring-ping_2.4s_ease-out_infinite] rounded-full border-2 border-forest-200" />
      <svg viewBox="0 0 80 80" className="absolute inset-0 size-full animate-[spin_1.8s_linear_infinite]">
        <circle cx="40" cy="40" r="36" fill="none" className="stroke-forest-100" strokeWidth="4" />
        <path d="M40 4a36 36 0 0 1 36 36" fill="none" className="stroke-forest-500" strokeWidth="4" strokeLinecap="round" />
      </svg>
      <span className="relative flex size-14 items-center justify-center rounded-full bg-forest-50 text-forest-600">
        <ShieldCheckIcon className="size-6" />
      </span>
    </span>
  )
}
