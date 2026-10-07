import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangleIcon,
  ArrowLeftIcon,
  ArrowRightIcon,
  BusIcon,
  CarIcon,
  CheckIcon,
  CircleCheckIcon,
  ClockIcon,
  HourglassIcon,
  PlaneIcon,
  SearchXIcon,
  ShipIcon,
  TimerOffIcon,
  TrainFrontIcon,
  XIcon,
  type LucideIcon,
} from 'lucide-react'
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
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { useSecondsLeft } from '@/features/booking/lib/useSecondsLeft'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { SuccessBurst } from '@/shared/components/SuccessBurst'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { hotelLevels, myTripQuery, quoteLineLabels, transferLabel, tripKeys, tripsApi, type TransferMode, type Trip } from '../api/trips.api'
import { RoutePath } from '../components/RoutePath'
import { TripStatusBadge } from '../components/TripStatusBadge'
import { formatTimeLeft } from '../lib/timeLeft'

/** An icon for each way of getting to the next stop (1 = the customer arranges it - nothing shown). */
const transferIcons: Partial<Record<TransferMode, LucideIcon>> = { 2: BusIcon, 3: TrainFrontIcon, 4: PlaneIcon, 5: CarIcon, 6: ShipIcon }

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
        icon={error.status === 404 ? SearchXIcon : AlertTriangleIcon}
        tone={error.status === 404 ? 'sun' : 'clay'}
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
      <PageHeader
        variant="display"
        eyebrow={
          <Button asChild variant="ghost" size="sm" className="-ml-3">
            <Link to="/account/trips">
              <ArrowLeftIcon />
              My trips
            </Link>
          </Button>
        }
        title={<RoutePath stops={trip.legs.map((l) => l.destinationName)} size="lg" />}
        description={
          <>
            {formatDate(trip.startDate)} → {formatDate(trip.endDate)} · {trip.totalNights} nights · {people.join(', ')} ·{' '}
            {hotelLevels.find((h) => h.value === trip.hotelLevel)?.label} hotels
          </>
        }
        actions={
          <>
            <span className="rounded-lg bg-ink-100 px-2.5 py-1 font-mono text-xs font-medium text-ink-700 ring-1 ring-ink-200 ring-inset">{trip.tripNo}</span>
            <TripStatusBadge status={trip.status} />
          </>
        }
      />

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_17rem] lg:items-start">
        <div className="grid min-w-0 gap-6">
          {trip.status === 1 && !trip.quote && <WaitingCard justSubmitted={justSubmitted} />}
          {trip.quote && <QuoteCard trip={trip} />}
          {trip.status === 5 && trip.timeline.rejectReason && (
            <FormAlert kind="error">We're sorry - we can't arrange this trip: {trip.timeline.rejectReason}</FormAlert>
          )}
          <StopsCard trip={trip} />
        </div>

        <div className="grid gap-6 lg:sticky lg:top-24">
          <Timeline trip={trip} />

          {trip.canCancel && (
            <section className="grid gap-3 rounded-3xl border border-dashed border-ink-300 p-5">
              <h2 className="text-base font-bold text-ink-900">Changed your mind?</h2>
              <p className="text-sm text-ink-500">You can withdraw this request at no cost - nothing has been booked yet.</p>
              <Button variant="destructive" className="w-full" onClick={() => setCancelOpen(true)}>
                Cancel request
              </Button>
              <CancelTripDialog tripNo={trip.tripNo} open={cancelOpen} onOpenChange={setCancelOpen} />
            </section>
          )}
        </div>
      </div>
    </div>
  )
}

/**
 * Before the price arrives: what happens next. Straight after sending the
 * request (the plan page passes justSubmitted) it celebrates with the
 * success burst; on later visits an hourglass shows it's in progress.
 */
function WaitingCard({ justSubmitted }: { justSubmitted: boolean }) {
  return (
    <section className="flex flex-col items-start gap-4 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:flex-row sm:items-center sm:gap-5 sm:p-6">
      {justSubmitted ? (
        <SuccessBurst size={72} />
      ) : (
        <span className="flex size-14 shrink-0 items-center justify-center rounded-2xl bg-sun-50 text-sun-700 ring-8 ring-sun-50/60">
          <HourglassIcon className="size-6" />
        </span>
      )}
      <div className="grid gap-1">
        <h2 className="text-lg font-bold text-ink-900">{justSubmitted ? "Request sent - we're on it!" : "We're preparing your quote"}</h2>
        <p className="text-sm text-ink-500">
          Our team is planning the hotels and transport. We'll email you a price, usually within 24 hours - it will show up here too.
        </p>
      </div>
    </section>
  )
}

/**
 * The price staff sent: the plan, the lines, the total and how long it
 * stands (ticking) - with "Accept & pay" while it's live, "Pay now" once
 * accepted, "View booking" once paid (17-day plan, Day 15).
 */
function QuoteCard({ trip }: { trip: Trip }) {
  const quote = trip.quote!
  const secondsLeft = useSecondsLeft(trip.status === 2 ? quote.expiresAtUtc : null)
  const live = trip.status === 2 && !quote.isExpired && secondsLeft !== 0
  const booking = trip.booking

  return (
    <section
      className={cn(
        'grid gap-5 rounded-3xl bg-card p-5 sm:p-6',
        // The live quote is the page's main thing: a green frame and a deeper shadow.
        live ? 'shadow-lift ring-2 ring-forest-500' : 'shadow-card ring-1 ring-ink-200/80',
      )}
    >
      <header className="flex items-start justify-between gap-4">
        <div className="grid min-w-0 gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-base font-bold text-ink-900">{quote.version > 1 ? 'Your updated quote' : 'Your quote'}</h2>
            {quote.version > 1 && <Badge variant="neutral">Version {quote.version}</Badge>}
          </div>
          <p className="text-xs text-ink-500">Sent {formatDateTime(quote.quotedAtUtc)}</p>
        </div>
        <div className="grid shrink-0 justify-items-end">
          <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Total</span>
          <span className="text-2xl leading-tight font-bold tracking-tight text-forest-800 sm:text-3xl">{formatTaka(quote.total)}</span>
        </div>
      </header>

      <div className="grid gap-1.5 rounded-2xl bg-ink-50 p-4 ring-1 ring-ink-100 ring-inset">
        <h3 className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">The plan</h3>
        <p className="text-sm leading-relaxed whitespace-pre-line text-ink-700">{quote.itinerary}</p>
      </div>

      {/* Line items: the category in a fixed-width tag, so the descriptions form a column; amounts right-aligned. */}
      <dl className="grid gap-3 text-sm">
        {quote.lines.map((line, i) => (
          <div key={i} className="flex items-start justify-between gap-4">
            <dt className="flex min-w-0 items-start gap-3">
              <span className="mt-px inline-flex h-5 w-[5.5rem] shrink-0 items-center justify-center rounded-md bg-forest-50 text-[0.6875rem] font-semibold text-forest-700">
                {quoteLineLabels[line.category]}
              </span>
              <span className="text-ink-700">{line.description}</span>
            </dt>
            <dd className="nums shrink-0 font-medium text-ink-900">{formatTaka(line.amount)}</dd>
          </div>
        ))}
        <div className="flex items-baseline justify-between gap-4 border-t border-ink-200 pt-3">
          <dt className="font-semibold text-ink-900">Total for everyone</dt>
          <dd className="nums text-lg font-bold text-ink-900">{formatTaka(quote.total)}</dd>
        </div>
      </dl>

      {live && (
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-100 pt-5">
          <div className="grid gap-1.5">
            {secondsLeft !== null && (
              <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-sun-50 px-2.5 py-1 text-xs font-semibold text-sun-700 ring-1 ring-sun-100 ring-inset">
                <ClockIcon className="size-3.5" />
                {formatTimeLeft(secondsLeft)} left
              </span>
            )}
            <span className="text-sm text-ink-500">Valid until {formatDateTime(quote.expiresAtUtc)}</span>
          </div>
          <Button asChild size="lg">
            <Link to={`/account/trips/${encodeURIComponent(trip.tripNo)}/accept`}>
              Accept & pay
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </div>
      )}
      {booking?.status === 1 && (
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-100 pt-5">
          <p className="max-w-sm text-sm text-ink-600">You accepted this quote - booking {booking.bookingNo} is waiting for your payment.</p>
          <Button asChild size="lg">
            <Link to={`/checkout/${encodeURIComponent(booking.bookingNo)}`}>
              Pay now
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </div>
      )}
      {booking && booking.status !== 1 && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-forest-50 p-4 ring-1 ring-forest-100 ring-inset">
          <p className="flex items-start gap-2.5 text-sm font-medium text-forest-800">
            <CircleCheckIcon className="mt-px size-4 shrink-0 text-forest-600" />
            Paid - your trip is booked as {booking.bookingNo}. The voucher is in your email.
          </p>
          <Button asChild variant="outline">
            <Link to={`/account/bookings/${encodeURIComponent(booking.bookingNo)}`}>View booking</Link>
          </Button>
        </div>
      )}
      {!live && !booking && (trip.status === 2 || trip.status === 6) && (
        <p className="flex items-start gap-2.5 rounded-xl bg-clay-50 px-3.5 py-3 text-sm font-medium text-clay-700 ring-1 ring-clay-100 ring-inset">
          <TimerOffIcon className="mt-px size-4 shrink-0" />
          This quote ran out on {formatDateTime(quote.expiresAtUtc)}. Contact us for a new price.
        </p>
      )}
    </section>
  )
}

/** The stops in order as a route: numbered nodes on a dashed line, each with its dates and how to get on to the next. */
function StopsCard({ trip }: { trip: Trip }) {
  return (
    <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <header className="grid gap-0.5">
        <h2 className="text-base font-bold text-ink-900">Your stops</h2>
        <p className="text-xs text-ink-500">
          {trip.legs.length} stop{trip.legs.length === 1 ? '' : 's'} · {trip.totalNights} nights
        </p>
      </header>

      <ol className="grid">
        {trip.legs.map((leg, i) => {
          const last = i === trip.legs.length - 1
          const TransferIcon = transferIcons[leg.transferToNext]
          return (
            <li key={leg.sequence} className="relative grid grid-cols-[2rem_minmax(0,1fr)] gap-x-3 pb-6 last:pb-0">
              {!last && <span aria-hidden className="absolute top-9 bottom-1 left-[15px] border-l-2 border-dashed border-forest-200" />}
              <span className="flex size-8 items-center justify-center rounded-full bg-primary text-xs font-bold text-white shadow-[0_6px_16px_-6px_rgb(23_88_63/0.7)]">
                {leg.sequence}
              </span>
              <div className="grid gap-1 pt-1">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-semibold text-ink-900">{leg.destinationName}</span>
                  <span className="nums rounded-full bg-forest-50 px-2 py-0.5 text-xs font-semibold text-forest-700">
                    {leg.nights} night{leg.nights === 1 ? '' : 's'}
                  </span>
                </div>
                <span className="text-sm text-ink-500">
                  {formatDate(leg.checkInDate)} → {formatDate(leg.checkOutDate)}
                </span>
                {leg.transferToNext !== 1 && (
                  <span className="mt-1.5 inline-flex w-fit items-center gap-1.5 rounded-full bg-ink-100 px-2.5 py-1 text-xs font-medium text-ink-600">
                    {TransferIcon && <TransferIcon className="size-3.5 text-ink-500" />}
                    Then by {transferLabel(leg.transferToNext).toLowerCase()}
                  </span>
                )}
              </div>
            </li>
          )
        })}
      </ol>

      {(trip.notes || trip.budgetPerPerson) && (
        <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2.5 border-t border-dashed border-ink-200 pt-4 text-sm">
          {trip.budgetPerPerson && (
            <>
              <dt className="text-ink-500">Budget</dt>
              <dd className="font-medium text-ink-900">{formatTaka(trip.budgetPerPerson)} per person</dd>
            </>
          )}
          {trip.notes && (
            <>
              <dt className="text-ink-500">Notes</dt>
              <dd className="whitespace-pre-line text-ink-700">{trip.notes}</dd>
            </>
          )}
        </dl>
      )}
    </section>
  )
}

type StepKind = 'done' | 'next' | 'todo' | 'warn' | 'stop'

/** What happened, step by step, as a vertical timeline - done steps ticked with their time, the next one ringed. */
function Timeline({ trip }: { trip: Trip }) {
  const t = trip.timeline
  const steps: { label: string; at: string | null; note?: string | null; kind?: StepKind }[] = [
    { label: 'Request sent', at: t.submittedAtUtc },
    { label: trip.quote && trip.quote.version > 1 ? `Quote sent (version ${trip.quote.version})` : 'Quote sent', at: t.quotedAtUtc },
  ]
  if (t.expiredAtUtc) steps.push({ label: 'Quote expired', at: t.expiredAtUtc, kind: 'warn' })
  if (t.rejectedAtUtc) steps.push({ label: 'Not possible', at: t.rejectedAtUtc, note: t.rejectReason, kind: 'stop' })
  if (t.cancelledAtUtc) steps.push({ label: 'Cancelled by you', at: t.cancelledAtUtc, note: t.cancelReason, kind: 'stop' })
  if (!t.rejectedAtUtc && !t.cancelledAtUtc) {
    steps.push({ label: 'Accepted', at: t.acceptedAtUtc }, { label: 'Paid - trip confirmed', at: t.paidAtUtc })
  }
  // The first step not reached yet is "next" - unless the trip ended (expired, rejected, cancelled).
  const ended = steps.some((s) => s.kind === 'warn' || s.kind === 'stop')
  const nextIndex = ended ? -1 : steps.findIndex((s) => !s.at)
  const kinds = steps.map((s, i): StepKind => s.kind ?? (s.at ? 'done' : i === nextIndex ? 'next' : 'todo'))

  return (
    <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80">
      <h2 className="text-base font-bold text-ink-900">Progress</h2>
      <ol className="grid">
        {steps.map((step, i) => {
          const kind = kinds[i]
          const last = i === steps.length - 1
          return (
            <li key={step.label} className="relative flex gap-3 pb-5 last:pb-0">
              {/* The line down to the next step: solid green into a step that went well, grey into how it ended, dashed grey until then. */}
              {!last && (
                <span
                  aria-hidden
                  className={cn(
                    'absolute top-7 bottom-1 left-[11px] border-l-2',
                    kinds[i + 1] === 'done' ? 'border-forest-300' : kinds[i + 1] === 'warn' || kinds[i + 1] === 'stop' ? 'border-ink-200' : 'border-dashed border-ink-200',
                  )}
                />
              )}
              <TimelineNode kind={kind} />
              <div className="grid min-w-0 gap-0.5 pt-0.5">
                <span className={cn('text-sm font-semibold', kind === 'todo' ? 'text-ink-400' : kind === 'next' ? 'text-forest-700' : 'text-ink-900')}>
                  {step.label}
                  <span className="sr-only">{kind === 'next' ? ' (next step)' : kind === 'todo' ? ' (not yet)' : ''}</span>
                </span>
                {step.at && <span className="text-xs text-ink-500">{formatDateTime(step.at)}</span>}
                {kind === 'next' && <span className="text-xs text-ink-500">Up next</span>}
                {step.note && <span className="text-xs text-ink-500">{step.note}</span>}
              </div>
            </li>
          )
        })}
      </ol>
    </section>
  )
}

/** The dot of one timeline step: a green tick, a ringed "next", an empty circle, or amber / terracotta for how it ended. */
function TimelineNode({ kind }: { kind: StepKind }) {
  const base = 'relative z-10 flex size-6 shrink-0 items-center justify-center rounded-full'
  if (kind === 'done') {
    return (
      <span className={cn(base, 'bg-forest-600 text-white')}>
        <CheckIcon className="size-3.5" strokeWidth={3} />
      </span>
    )
  }
  if (kind === 'next') {
    return (
      <span className={cn(base, 'bg-card ring-2 ring-forest-500 ring-inset')}>
        <span className="absolute inset-0 animate-[ring-ping_2s_ease-out_infinite] rounded-full border-2 border-forest-300" />
        <span className="size-2 rounded-full bg-forest-500" />
      </span>
    )
  }
  if (kind === 'warn') {
    return (
      <span className={cn(base, 'bg-sun-100 text-sun-700')}>
        <HourglassIcon className="size-3.5" />
      </span>
    )
  }
  if (kind === 'stop') {
    return (
      <span className={cn(base, 'bg-clay-100 text-clay-600')}>
        <XIcon className="size-3.5" strokeWidth={3} />
      </span>
    )
  }
  return <span className={cn(base, 'bg-card ring-2 ring-ink-200 ring-inset')} />
}

function CancelTripDialog({ tripNo, open, onOpenChange }: { tripNo: string; open: boolean; onOpenChange: (open: boolean) => void }) {
  const queryClient = useQueryClient()
  const cancel = useMutation({
    mutationFn: () => tripsApi.cancel(tripNo, null),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: tripKeys.all })
      onOpenChange(false)
      notify.success('Request cancelled', { description: `Our team has stopped working on ${tripNo}. You can send a new request any time.` })
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
