import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon } from 'lucide-react'
import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { myBookingQuery, type MyBooking, type RefundStatus } from '@/features/booking/api/bookings.api'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { BookingDocuments } from '../components/BookingDocuments'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { CancelBookingDialog } from '../components/CancelBookingDialog'

const refundLabels: Record<RefundStatus, string> = {
  1: "requested - our team will send it to you within a few working days",
  2: 'approved - on its way',
  3: 'not approved - please contact us',
  4: 'being sent',
  5: 'refunded',
  6: 'failed - our team will contact you',
}

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
  const nights = booking.nights === 0 ? 'day trip' : `${booking.nights} night${booking.nights === 1 ? '' : 's'}`

  return (
    <div className="grid gap-6">
      <Link to="/account/bookings" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-4" /> My bookings
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="grid gap-1">
          <h1 className="text-2xl font-semibold">{booking.packageTitle ?? 'Custom trip'}</h1>
          <p className="text-muted-foreground">
            {formatDate(booking.startDate)} → {formatDate(booking.endDate)} · {nights}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="rounded-md bg-muted px-2 py-1 font-mono text-xs">{booking.bookingNo}</span>
          <BookingStatusBadge status={booking.status} />
        </div>
      </div>

      {booking.status === 1 && (
        <FormAlert kind="error">
          Not paid yet - your seats are held only for a short time.{' '}
          <Link to={`/checkout/${encodeURIComponent(booking.bookingNo)}`} className="font-medium underline underline-offset-4">
            Pay now
          </Link>
        </FormAlert>
      )}

      {booking.refund && (
        <FormAlert kind="success">
          Refund {booking.refund.refundNo} of {formatTaka(booking.refund.amount)}: {refundLabels[booking.refund.status]}.
        </FormAlert>
      )}

      <section className="grid gap-4 rounded-xl border bg-card p-4">
        <h2 className="font-semibold">Travellers</h2>
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
        <dl className="grid grid-cols-[1fr_auto] gap-y-1 text-sm">
          <dt className="text-muted-foreground">Total</dt>
          <dd className="font-medium">{formatTaka(booking.totalAmount)}</dd>
          <dt className="text-muted-foreground">Paid</dt>
          <dd>{formatTaka(booking.paidAmount)}</dd>
        </dl>
        <Separator />
        <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
          <dt className="text-muted-foreground">Contact</dt>
          <dd>{booking.contactName}</dd>
          <dt className="text-muted-foreground">Mobile</dt>
          <dd>{booking.contactPhone}</dd>
          <dt className="text-muted-foreground">Email</dt>
          <dd>{booking.contactEmail ?? '-'}</dd>
          {booking.specialRequest && (
            <>
              <dt className="text-muted-foreground">Request</dt>
              <dd className="whitespace-pre-line">{booking.specialRequest}</dd>
            </>
          )}
        </dl>
      </section>

      <BookingDocuments booking={booking} />

      {booking.cancellation.canCancel && (
        <section className="grid gap-2 rounded-xl border p-4">
          <h2 className="font-semibold">Cancel this booking</h2>
          <p className="text-sm text-muted-foreground">
            {booking.paidAmount > 0
              ? `Cancelling now (${booking.cancellation.daysBeforeStart} days before the trip) gives back ${formatTaka(booking.cancellation.refundAmount)} - ${booking.cancellation.refundPercent}% of what you paid.`
              : 'Nothing has been paid yet - cancelling costs nothing.'}
          </p>
          <div>
            <Button variant="outline" onClick={() => setCancelOpen(true)}>
              Cancel booking
            </Button>
          </div>
          <CancelBookingDialog booking={booking} open={cancelOpen} onOpenChange={setCancelOpen} />
        </section>
      )}
    </div>
  )
}
