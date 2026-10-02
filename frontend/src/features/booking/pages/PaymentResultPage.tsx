import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { PageMessage } from '../components/PageMessage'
import { bookingForPayment } from '../lib/checkoutSession'

/**
 * Where the API sends the customer after SSLCommerz's page:
 * /payment/result?payment=PAY100001&outcome=success|fail|cancel.
 *
 * Day 9: shows the outcome SSLCommerz reported. That report alone is not
 * trusted - the booking is confirmed only once the API has checked the
 * payment with SSLCommerz itself (Day 10, which also makes this page wait
 * for that answer). No login needed to SEE this page.
 */
export function PaymentResultPage() {
  useDocumentMeta({ title: 'Payment' })
  const [search] = useSearchParams()
  const paymentNo = search.get('payment') ?? ''
  const outcome = search.get('outcome')
  const bookingNo = paymentNo ? bookingForPayment(paymentNo) : null

  const backToBooking = bookingNo && (
    <Button asChild>
      <Link to={`/checkout/${bookingNo}`}>{outcome === 'success' ? 'View your booking' : 'Try again'}</Link>
    </Button>
  )

  if (outcome === 'success') {
    return (
      <PageMessage
        title="Thank you - we're confirming your payment"
        text="This usually takes a moment. Your booking is confirmed as soon as the payment is verified, and your voucher is emailed to you."
      >
        <span className="flex items-center gap-2 text-sm text-muted-foreground">
          <Spinner /> Checking with the payment service…
        </span>
        {backToBooking}
      </PageMessage>
    )
  }

  if (outcome === 'cancel') {
    return (
      <PageMessage title="Payment cancelled" text="Nothing was charged. Your seats stay held until the countdown ends - you can still pay.">
        {backToBooking}
      </PageMessage>
    )
  }

  return (
    <PageMessage
      title="The payment didn't go through"
      text="Nothing was charged. You can try again while your seats are still held, or use another payment method."
    >
      {backToBooking ?? (
        <Button asChild variant="outline">
          <Link to="/packages">See packages</Link>
        </Button>
      )}
    </PageMessage>
  )
}
