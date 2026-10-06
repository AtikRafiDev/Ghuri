import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeftIcon, CheckCircle2Icon, CircleIcon } from 'lucide-react'
import { useState } from 'react'
import { Link, useLocation, useParams } from 'react-router'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { hotelLevels, myTripQuery, quoteLineLabels, transferLabel, tripKeys, tripsApi, type Trip } from '../api/trips.api'
import { TripStatusBadge } from '../components/TripStatusBadge'

/**
 * /account/trips/:tripNo - one custom trip: the stops with their dates, the
 * quote once it's there, and a timeline of what happened (17-day plan,
 * Day 14). The quote email links here.
 */
export function MyTripPage() {
  const { tripNo = '' } = useParams()
  useDocumentMeta({ title: `Trip ${tripNo}` })
  const trip = useQuery(myTripQuery(tripNo))

  if (trip.isPending) return <PageSpinner />
  if (trip.isError) {
    const error = toAppError(trip.error)
    return (
      <PageMessage
        title={error.status === 404 ? 'Trip not found' : "This trip couldn't be loaded"}
        text={error.status === 404 ? "This trip doesn't exist, or it belongs to another account." : error.message}
      >
        <Button asChild variant="outline">
          <Link to="/account/trips">My trips</Link>
        </Button>
      </PageMessage>
    )
  }

  return <TripDetails trip={trip.data} />
}

function TripDetails({ trip }: { trip: Trip }) {
  const location = useLocation()
  const justSubmitted = (location.state as { justSubmitted?: boolean } | null)?.justSubmitted === true
  const [cancelOpen, setCancelOpen] = useState(false)
  const people = [`${trip.adults} adult${trip.adults === 1 ? '' : 's'}`]
  if (trip.children) people.push(`${trip.children} child${trip.children === 1 ? '' : 'ren'}`)
  if (trip.infants) people.push(`${trip.infants} infant${trip.infants === 1 ? '' : 's'}`)

  return (
    <div className="grid gap-6">
      <Link to="/account/trips" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-4" /> My trips
      </Link>

      {justSubmitted && (
        <FormAlert kind="success">Your request is on its way to our team. We'll email you a price, usually within 24 hours.</FormAlert>
      )}

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="grid gap-1">
          <h1 className="text-2xl font-semibold">{trip.legs.map((l) => l.destinationName).join(' → ')}</h1>
          <p className="text-muted-foreground">
            {formatDate(trip.startDate)} → {formatDate(trip.endDate)} · {trip.totalNights} nights · {people.join(', ')} ·{' '}
            {hotelLevels.find((h) => h.value === trip.hotelLevel)?.label} hotels
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="rounded-md bg-muted px-2 py-1 font-mono text-xs">{trip.tripNo}</span>
          <TripStatusBadge status={trip.status} />
        </div>
      </div>

      {trip.quote && <QuoteCard trip={trip} />}
      {trip.status === 5 && trip.timeline.rejectReason && (
        <FormAlert kind="error">We're sorry - we can't arrange this trip: {trip.timeline.rejectReason}</FormAlert>
      )}

      <section className="grid gap-3 rounded-xl border bg-card p-4">
        <h2 className="font-semibold">Your stops</h2>
        <ol className="grid gap-3">
          {trip.legs.map((leg) => (
            <li key={leg.sequence} className="grid gap-0.5 text-sm">
              <span className="font-medium">
                {leg.sequence}. {leg.destinationName} · {leg.nights} night{leg.nights === 1 ? '' : 's'}
              </span>
              <span className="text-muted-foreground">
                {formatDate(leg.checkInDate)} → {formatDate(leg.checkOutDate)}
                {leg.transferToNext !== 1 && ` · then by ${transferLabel(leg.transferToNext).toLowerCase()}`}
              </span>
            </li>
          ))}
        </ol>
        {(trip.notes || trip.budgetPerPerson) && (
          <div className="grid gap-1 border-t pt-3 text-sm text-muted-foreground">
            {trip.budgetPerPerson && <span>Budget: {formatTaka(trip.budgetPerPerson)} per person</span>}
            {trip.notes && <span className="whitespace-pre-line">Notes: {trip.notes}</span>}
          </div>
        )}
      </section>

      <Timeline trip={trip} />

      {trip.canCancel && (
        <section className="grid justify-items-start gap-2 rounded-xl border p-4">
          <h2 className="font-semibold">Changed your mind?</h2>
          <p className="text-sm text-muted-foreground">You can withdraw this request at no cost - nothing has been booked yet.</p>
          <Button variant="outline" onClick={() => setCancelOpen(true)}>
            Cancel request
          </Button>
          <CancelTripDialog tripNo={trip.tripNo} open={cancelOpen} onOpenChange={setCancelOpen} />
        </section>
      )}
    </div>
  )
}

/** The price staff sent: the plan, the lines, the total and until when it stands. */
function QuoteCard({ trip }: { trip: Trip }) {
  const quote = trip.quote!
  const live = trip.status === 2 && !quote.isExpired

  return (
    <section className={live ? 'grid gap-4 rounded-xl border-2 border-primary bg-card p-4' : 'grid gap-4 rounded-xl border bg-card p-4 opacity-80'}>
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 className="font-semibold">{quote.version > 1 ? 'Your updated quote' : 'Your quote'}</h2>
        <span className="text-2xl font-semibold tabular-nums">{formatTaka(quote.total)}</span>
      </div>
      <p className="text-sm whitespace-pre-line">{quote.itinerary}</p>
      <dl className="grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 border-t pt-3 text-sm">
        {quote.lines.map((line, i) => (
          <div key={i} className="contents">
            <dt>
              <span className="text-muted-foreground">{quoteLineLabels[line.category]} · </span>
              {line.description}
            </dt>
            <dd className="text-right tabular-nums">{formatTaka(line.amount)}</dd>
          </div>
        ))}
        <dt className="border-t pt-1 font-semibold">Total for everyone</dt>
        <dd className="border-t pt-1 text-right font-semibold tabular-nums">{formatTaka(quote.total)}</dd>
      </dl>
      <p className={live ? 'text-sm' : 'text-sm text-destructive'}>
        {live
          ? `This price is valid until ${formatDateTime(quote.expiresAtUtc)}.`
          : `This quote ran out on ${formatDateTime(quote.expiresAtUtc)}. Contact us for a new price.`}
      </p>
    </section>
  )
}

/** What happened, step by step - done steps ticked, with their time. */
function Timeline({ trip }: { trip: Trip }) {
  const t = trip.timeline
  const steps: { label: string; at: string | null; note?: string | null }[] = [
    { label: 'Request sent', at: t.submittedAtUtc },
    { label: trip.quote && trip.quote.version > 1 ? `Quote sent (version ${trip.quote.version})` : 'Quote sent', at: t.quotedAtUtc },
  ]
  if (t.expiredAtUtc) steps.push({ label: 'Quote expired', at: t.expiredAtUtc })
  if (t.rejectedAtUtc) steps.push({ label: 'Not possible', at: t.rejectedAtUtc, note: t.rejectReason })
  if (t.cancelledAtUtc) steps.push({ label: 'Cancelled by you', at: t.cancelledAtUtc, note: t.cancelReason })
  if (!t.rejectedAtUtc && !t.cancelledAtUtc) {
    steps.push({ label: 'Accepted', at: t.acceptedAtUtc }, { label: 'Paid - trip confirmed', at: t.paidAtUtc })
  }

  return (
    <section className="grid gap-3 rounded-xl border bg-card p-4">
      <h2 className="font-semibold">Progress</h2>
      <ol className="grid gap-3">
        {steps.map((step) => (
          <li key={step.label} className="flex gap-3 text-sm">
            {step.at ? <CheckCircle2Icon className="mt-0.5 size-4 shrink-0 text-primary" /> : <CircleIcon className="mt-0.5 size-4 shrink-0 text-muted-foreground" />}
            <div className="grid">
              <span className={step.at ? 'font-medium' : 'text-muted-foreground'}>{step.label}</span>
              {step.at && <span className="text-muted-foreground">{formatDateTime(step.at)}</span>}
              {step.note && <span className="text-muted-foreground">{step.note}</span>}
            </div>
          </li>
        ))}
      </ol>
    </section>
  )
}

function CancelTripDialog({ tripNo, open, onOpenChange }: { tripNo: string; open: boolean; onOpenChange: (open: boolean) => void }) {
  const queryClient = useQueryClient()
  const cancel = useMutation({
    mutationFn: () => tripsApi.cancel(tripNo, null),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: tripKeys.all })
      onOpenChange(false)
    },
  })

  return (
    <AlertDialog open={open} onOpenChange={(next) => !cancel.isPending && onOpenChange(next)}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Cancel request {tripNo}?</AlertDialogTitle>
          <AlertDialogDescription>Our team stops working on it. You can always send a new request later.</AlertDialogDescription>
        </AlertDialogHeader>
        {cancel.isError && <FormAlert kind="error">{toAppError(cancel.error).message}</FormAlert>}
        <AlertDialogFooter>
          <AlertDialogCancel disabled={cancel.isPending}>Keep it</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            disabled={cancel.isPending}
            onClick={(event) => {
              event.preventDefault()
              cancel.mutate()
            }}
          >
            {cancel.isPending && <Spinner />}
            Cancel request
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
