import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeftIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { hotelLevels, quoteLineLabels, transferLabel } from '@/features/trips/api/trips.api'
import { TripStatusBadge } from '@/features/trips/components/TripStatusBadge'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ActionDialog } from '../../components/ActionDialog'
import { adminTripKeys, adminTripsApi, quoteRoles, type AdminTrip } from '../api/adminTrips.api'
import { QuoteEditor } from '../components/QuoteEditor'

/** Admin → Custom trips → one request: what the customer wants, the quote, and quote / reject (17-day plan, Day 15). */
export function AdminCustomTripPage() {
  const { tripNo = '' } = useParams()
  useDocumentMeta({ title: `Custom trip ${tripNo}` })
  const data = useQuery({ queryKey: adminTripKeys.one(tripNo), queryFn: () => adminTripsApi.get(tripNo) })

  if (data.isPending) return <PageSpinner />
  if (data.isError) {
    const error = toAppError(data.error)
    return (
      <div className="grid justify-items-start gap-3">
        <p className="text-destructive">{error.status === 404 ? `Request ${tripNo} doesn't exist.` : error.message}</p>
        <Button asChild variant="outline">
          <Link to="/admin/custom-trips">All requests</Link>
        </Button>
      </div>
    )
  }

  return <Details data={data.data} />
}

function Details({ data }: { data: AdminTrip }) {
  const { trip, customer } = data
  const { hasAnyRole } = useAuth()
  const queryClient = useQueryClient()
  const [sent, setSent] = useState(false)
  const [rejectOpen, setRejectOpen] = useState(false)
  const [reason, setReason] = useState('')
  const canQuote = hasAnyRole(quoteRoles) && (trip.status === 1 || trip.status === 2 || trip.status === 6)
  const route = trip.legs.map((l) => l.destinationName).join(' → ')

  return (
    <div className="grid gap-6">
      <Link to="/admin/custom-trips" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-4" /> All requests
      </Link>

      <div className="grid gap-1">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="font-mono text-2xl font-semibold">{trip.tripNo}</h1>
          <TripStatusBadge status={trip.status} />
        </div>
        <p className="text-muted-foreground">
          {route} · submitted {formatDateTime(trip.timeline.submittedAtUtc)}
        </p>
      </div>

      {sent && <FormAlert kind="success">Quote sent - the customer gets an email with the price in a few seconds.</FormAlert>}
      {trip.booking && (
        <FormAlert kind="success">
          Accepted - booking{' '}
          <Link to={`/admin/bookings/${encodeURIComponent(trip.booking.bookingNo)}`} className="font-mono font-medium underline underline-offset-4">
            {trip.booking.bookingNo}
          </Link>{' '}
          {trip.booking.status === 1 ? 'is waiting for payment.' : 'is paid.'}
        </FormAlert>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        <Section title="What they want">
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
            <dt className="text-muted-foreground">Dates</dt>
            <dd>
              {formatDate(trip.startDate)} → {formatDate(trip.endDate)} ({trip.totalNights} nights)
            </dd>
            <dt className="text-muted-foreground">People</dt>
            <dd>
              {trip.adults} adult(s), {trip.children} child(ren), {trip.infants} infant(s)
            </dd>
            <dt className="text-muted-foreground">Hotels</dt>
            <dd>{hotelLevels.find((h) => h.value === trip.hotelLevel)?.label}</dd>
            <dt className="text-muted-foreground">Budget</dt>
            <dd>{trip.budgetPerPerson ? `${formatTaka(trip.budgetPerPerson)} per person` : '-'}</dd>
          </dl>
          {trip.notes && <p className="text-sm whitespace-pre-line">“{trip.notes}”</p>}
        </Section>
        <Section title="Customer">
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
            <dt className="text-muted-foreground">Name</dt>
            <dd>{customer.fullName}</dd>
            <dt className="text-muted-foreground">Mobile</dt>
            <dd>{customer.phone}</dd>
            <dt className="text-muted-foreground">Email</dt>
            <dd>{customer.email ?? '-'}</dd>
            <dt className="text-muted-foreground">Booked before</dt>
            <dd>{data.previousBookings === 0 ? 'No - a new customer' : `${data.previousBookings} trip(s)`}</dd>
          </dl>
        </Section>
      </div>

      <Section title="Stops">
        <ol className="grid gap-2 text-sm">
          {trip.legs.map((leg) => (
            <li key={leg.sequence}>
              <span className="font-medium">
                {leg.sequence}. {leg.destinationName} · {leg.nights} night{leg.nights === 1 ? '' : 's'}
              </span>{' '}
              <span className="text-muted-foreground">
                {formatDate(leg.checkInDate)} → {formatDate(leg.checkOutDate)}
                {leg.transferToNext !== 1 && ` · then ${transferLabel(leg.transferToNext).toLowerCase()}`}
              </span>
            </li>
          ))}
        </ol>
      </Section>

      {trip.quote && (
        <Section title={`Current quote (version ${trip.quote.version}${data.quotedByName ? `, by ${data.quotedByName}` : ''})`}>
          <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 text-sm">
            {trip.quote.lines.map((l, i) => (
              <div key={i} className="contents">
                <dt>
                  <span className="text-muted-foreground">{quoteLineLabels[l.category]} · </span>
                  {l.description}
                </dt>
                <dd className="text-right tabular-nums">{formatTaka(l.amount)}</dd>
              </div>
            ))}
            <dt className="border-t pt-1 font-semibold">Total</dt>
            <dd className="border-t pt-1 text-right font-semibold tabular-nums">{formatTaka(trip.quote.total)}</dd>
          </dl>
          <p className={trip.quote.isExpired ? 'text-sm text-destructive' : 'text-sm text-muted-foreground'}>
            {trip.quote.isExpired ? 'Ran out' : 'Valid until'} {formatDateTime(trip.quote.expiresAtUtc)}
          </p>
        </Section>
      )}

      {trip.status === 5 && <FormAlert kind="error">Rejected: {trip.timeline.rejectReason}</FormAlert>}
      {trip.status === 7 && <FormAlert kind="error">Cancelled{trip.timeline.cancelReason ? `: ${trip.timeline.cancelReason}` : ''}</FormAlert>}

      {canQuote && (
        <Section title={trip.quote ? 'Send a new quote' : 'Send a quote'}>
          <p className="text-sm text-muted-foreground">
            {trip.quote
              ? 'A new quote replaces the current one, restarts its validity and emails the customer again.'
              : 'The customer gets an email with the price and a link to accept and pay.'}
          </p>
          <QuoteEditor trip={trip} onSent={() => setSent(true)} />
          <div className="border-t pt-4">
            <Button variant="ghost" className="text-destructive" onClick={() => setRejectOpen(true)}>
              We can't do this trip…
            </Button>
          </div>
        </Section>
      )}

      <ActionDialog
        open={rejectOpen}
        onOpenChange={setRejectOpen}
        title={`Reject ${trip.tripNo}?`}
        description="The customer is emailed this reason - write it for them, and suggest an alternative if you can."
        submitLabel="Reject request"
        destructive
        onSubmit={async () => {
          await adminTripsApi.reject(trip.tripNo, reason)
          setReason('')
          await queryClient.invalidateQueries({ queryKey: adminTripKeys.all })
        }}
      >
        <div className="grid gap-1.5">
          <Label htmlFor="reject-reason">Reason</Label>
          <Textarea id="reject-reason" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
        </div>
      </ActionDialog>
    </div>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="grid content-start gap-3 rounded-xl border bg-card p-4">
      <h2 className="font-semibold">{title}</h2>
      {children}
    </section>
  )
}
