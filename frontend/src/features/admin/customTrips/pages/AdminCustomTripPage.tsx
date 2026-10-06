import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeftIcon,
  ArrowRightIcon,
  BusIcon,
  CarIcon,
  ClockIcon,
  PlaneIcon,
  QuoteIcon,
  SearchXIcon,
  ShipIcon,
  TrainFrontIcon,
  XCircleIcon,
  type LucideIcon,
} from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { hotelLevels, quoteLineLabels, transferLabel, type TransferMode } from '@/features/trips/api/trips.api'
import { TripStatusBadge } from '@/features/trips/components/TripStatusBadge'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ActionDialog } from '../../components/ActionDialog'
import { adminTripKeys, adminTripsApi, quoteRoles, type AdminTrip } from '../api/adminTrips.api'
import { QuoteEditor } from '../components/QuoteEditor'

// How the customer wants to get from one stop to the next (1 = they arrange it themselves - no icon, no line).
const transferIcons: Partial<Record<TransferMode, LucideIcon>> = { 2: BusIcon, 3: TrainFrontIcon, 4: PlaneIcon, 5: CarIcon, 6: ShipIcon }

/** "2 adults · 1 child" - only the kinds of traveller that are there. */
function describePeople(adults: number, children: number, infants: number): string {
  const part = (n: number, one: string, many: string) => (n > 0 ? `${n} ${n === 1 ? one : many}` : null)
  return [part(adults, 'adult', 'adults'), part(children, 'child', 'children'), part(infants, 'infant', 'infants')].filter(Boolean).join(' · ')
}

const backLink = (
  <Button asChild variant="ghost" size="sm" className="-ml-3 text-ink-500">
    <Link to="/admin/custom-trips">
      <ArrowLeftIcon className="group-hover/button:-translate-x-0.5" />
      All requests
    </Link>
  </Button>
)

/** Admin → Custom trips → one request: what the customer wants, the quote, and quote / reject (17-day plan, Day 15). */
export function AdminCustomTripPage() {
  const { tripNo = '' } = useParams()
  useDocumentMeta({ title: `Custom trip ${tripNo}` })
  const data = useQuery({ queryKey: adminTripKeys.one(tripNo), queryFn: () => adminTripsApi.get(tripNo) })

  if (data.isPending) return <PageSpinner />
  if (data.isError) {
    const error = toAppError(data.error)
    return (
      <div className="grid gap-6">
        <PageHeader eyebrow={backLink} title={<span className="font-mono">{tripNo}</span>} />
        {error.status === 404 ? (
          <EmptyState icon={SearchXIcon} title={`Request ${tripNo} doesn't exist`} text="Check the number, or find the request in the queue.">
            <Button asChild variant="outline">
              <Link to="/admin/custom-trips">All requests</Link>
            </Button>
          </EmptyState>
        ) : (
          <div className="grid justify-items-start gap-3">
            <FormAlert kind="error">{error.message}</FormAlert>
            <Button variant="outline" size="sm" onClick={() => data.refetch()}>
              Try again
            </Button>
          </div>
        )}
      </div>
    )
  }

  return <Details data={data.data} />
}

function Details({ data }: { data: AdminTrip }) {
  const { trip, customer } = data
  const { hasAnyRole } = useAuth()
  const queryClient = useQueryClient()
  const [rejectOpen, setRejectOpen] = useState(false)
  const [reason, setReason] = useState('')
  const canQuote = hasAnyRole(quoteRoles) && (trip.status === 1 || trip.status === 2 || trip.status === 6)
  const route = trip.legs.map((l) => l.destinationName).join(' → ')
  const hotel = hotelLevels.find((h) => h.value === trip.hotelLevel)

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow={backLink}
        title={<span className="font-mono tracking-tight">{trip.tripNo}</span>}
        titleAside={<TripStatusBadge status={trip.status} />}
        description={
          <>
            {route} · submitted {formatDateTime(trip.timeline.submittedAtUtc)}
          </>
        }
      />

      {/* States that must stay in view (not pop-ups): where this request ended up. */}
      {trip.booking && (
        <FormAlert kind="success">
          Accepted - booking{' '}
          <Link to={`/admin/bookings/${encodeURIComponent(trip.booking.bookingNo)}`} className="font-mono font-semibold underline underline-offset-4">
            {trip.booking.bookingNo}
          </Link>{' '}
          {trip.booking.status === 1 ? 'is waiting for payment.' : 'is paid.'}
        </FormAlert>
      )}
      {trip.status === 5 && <FormAlert kind="error">Rejected: {trip.timeline.rejectReason}</FormAlert>}
      {trip.status === 7 && <FormAlert kind="error">Cancelled{trip.timeline.cancelReason ? `: ${trip.timeline.cancelReason}` : ''}</FormAlert>}

      <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,22rem)]">
        {/* Main column: what they asked for, the route, and the quote being written. On a phone both
            columns dissolve (display: contents) into one list, re-ordered so the customer comes right after the request. */}
        <div className="stagger contents lg:grid lg:min-w-0 lg:gap-5">
          <Panel className="order-1" title="What they want" subtitle="The trip as the customer designed it">
            <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-6 gap-y-2.5 text-sm">
              <dt className="text-ink-500">Dates</dt>
              <dd className="flex flex-wrap items-center gap-x-2 font-medium text-ink-900">
                {formatDate(trip.startDate)}
                <ArrowRightIcon aria-hidden className="size-3.5 text-ink-400" />
                <span className="sr-only">to</span>
                {formatDate(trip.endDate)}
                <span className="font-normal text-ink-500">({trip.totalNights} nights)</span>
              </dd>
              <dt className="text-ink-500">People</dt>
              <dd className="font-medium text-ink-900">{describePeople(trip.adults, trip.children, trip.infants)}</dd>
              <dt className="text-ink-500">Hotels</dt>
              <dd className="font-medium text-ink-900">
                {hotel?.label}
                {hotel && <span className="font-normal text-ink-500"> · {hotel.hint}</span>}
              </dd>
              <dt className="text-ink-500">Budget</dt>
              <dd className="font-medium text-ink-900">
                {trip.budgetPerPerson ? `${formatTaka(trip.budgetPerPerson)} per person` : <span className="font-normal text-ink-400">Not given</span>}
              </dd>
            </dl>
            {trip.notes && (
              <blockquote className="relative rounded-2xl bg-ink-50 px-4 py-3.5 pl-11 text-sm whitespace-pre-line text-ink-700 ring-1 ring-ink-200/60 ring-inset">
                <QuoteIcon aria-hidden className="absolute top-3.5 left-4 size-4 text-forest-400" />
                {trip.notes}
              </blockquote>
            )}
          </Panel>

          <Panel className="order-3" title="Route" subtitle={`${trip.legs.length} ${trip.legs.length === 1 ? 'stop' : 'stops'}, in order`}>
            <ol className="grid">
              {trip.legs.map((leg, i) => {
                const isLast = i === trip.legs.length - 1
                const TransferIcon = transferIcons[leg.transferToNext]
                return (
                  <li key={leg.sequence} className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-4">
                    {/* The stop's number, and a dashed line down to the next stop. */}
                    <span aria-hidden className="relative flex w-8 justify-center self-stretch">
                      <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-forest-50 text-sm font-bold text-forest-700 ring-1 ring-forest-200 ring-inset">
                        {leg.sequence}
                      </span>
                      {!isLast && <span className="absolute top-10 bottom-2 border-l-2 border-dashed border-forest-200" />}
                    </span>
                    <div className={cn('grid min-w-0 content-start gap-1 pt-1', !isLast && 'pb-6')}>
                      <span className="flex flex-wrap items-center gap-2">
                        <span className="font-semibold text-ink-900">{leg.destinationName}</span>
                        <Badge variant="neutral">
                          {leg.nights} night{leg.nights === 1 ? '' : 's'}
                        </Badge>
                      </span>
                      <span className="text-sm text-ink-500">
                        {formatDate(leg.checkInDate)} → {formatDate(leg.checkOutDate)}
                      </span>
                      {leg.transferToNext !== 1 && (
                        <span className="mt-2 inline-flex w-fit items-center gap-1.5 rounded-full bg-card px-2.5 py-1 text-xs font-medium text-ink-600 ring-1 ring-ink-200">
                          {TransferIcon && <TransferIcon className="size-3.5 text-forest-600" />}
                          Then by {transferLabel(leg.transferToNext).toLowerCase()}
                        </span>
                      )}
                    </div>
                  </li>
                )
              })}
            </ol>
          </Panel>

          {canQuote && (
            <Panel
              className="order-5"
              title={trip.quote ? 'Send a new quote' : 'Send a quote'}
              subtitle={
                trip.quote
                  ? 'A new quote replaces the current one, restarts its validity and emails the customer again.'
                  : 'The customer gets an email with the price and a link to accept and pay.'
              }
            >
              <QuoteEditor
                trip={trip}
                secondaryAction={
                  <Button variant="ghost" className="text-clay-600 hover:bg-clay-50 hover:text-clay-700" onClick={() => setRejectOpen(true)}>
                    <XCircleIcon />
                    We can't do this trip…
                  </Button>
                }
              />
            </Panel>
          )}
        </div>

        {/* Side column: who's asking, and the price they have now - in view while a new one is written. */}
        <div className="stagger contents lg:sticky lg:top-20 lg:grid lg:min-w-0 lg:gap-5">
          <Panel className="order-2" title="Customer">
            <div className="flex items-center gap-3">
              <UserAvatar name={customer.fullName} size="lg" />
              <span className="grid min-w-0 gap-1">
                <span className="truncate font-semibold text-ink-900">{customer.fullName}</span>
                {data.previousBookings === 0 ? (
                  <Badge variant="neutral">New customer</Badge>
                ) : (
                  <Badge variant="success" dot>
                    Returning customer
                  </Badge>
                )}
              </span>
            </div>
            <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-6 gap-y-2.5 text-sm">
              <dt className="text-ink-500">Mobile</dt>
              <dd className="font-medium text-ink-900">
                <a href={`tel:${customer.phone}`} className="nums hover:text-forest-700 hover:underline">
                  {customer.phone}
                </a>
              </dd>
              <dt className="text-ink-500">Email</dt>
              <dd className="font-medium break-all text-ink-900">
                {customer.email ? (
                  <a href={`mailto:${customer.email}`} className="hover:text-forest-700 hover:underline">
                    {customer.email}
                  </a>
                ) : (
                  <span className="font-normal text-ink-400">-</span>
                )}
              </dd>
              <dt className="text-ink-500">Booked before</dt>
              <dd className="font-medium text-ink-900">
                {data.previousBookings === 0 ? 'No - a new customer' : `${data.previousBookings} ${data.previousBookings === 1 ? 'trip' : 'trips'}`}
              </dd>
            </dl>
          </Panel>

          {trip.quote && (
            <Panel className="order-4" title="Current quote" subtitle={`Version ${trip.quote.version}${data.quotedByName ? ` · by ${data.quotedByName}` : ''}`}>
              {/* Each line: what it is on the left, its amount on the right, level with the description's last line. */}
              <dl className="grid gap-3 text-sm">
                {trip.quote.lines.map((l, i) => (
                  <div key={i} className="flex items-end justify-between gap-4">
                    <dt className="grid min-w-0">
                      <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">{quoteLineLabels[l.category]}</span>
                      <span className="text-ink-900">{l.description}</span>
                    </dt>
                    <dd className="nums shrink-0 text-right font-medium text-ink-900">{formatTaka(l.amount)}</dd>
                  </div>
                ))}
                <div className="flex items-baseline justify-between gap-4 border-t border-ink-100 pt-3">
                  <dt className="font-semibold text-ink-900">Total</dt>
                  <dd className="nums text-base font-bold text-ink-900">{formatTaka(trip.quote.total)}</dd>
                </div>
              </dl>
              <p
                className={cn(
                  'flex items-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium ring-1 ring-inset',
                  trip.quote.isExpired ? 'bg-clay-50 text-clay-700 ring-clay-100' : 'bg-ink-50 text-ink-600 ring-ink-200/60',
                )}
              >
                <ClockIcon className="size-4 shrink-0" />
                {trip.quote.isExpired ? 'Ran out' : 'Valid until'} {formatDateTime(trip.quote.expiresAtUtc)}
              </p>
            </Panel>
          )}
        </div>
      </div>

      <ActionDialog
        open={rejectOpen}
        onOpenChange={setRejectOpen}
        title={`Reject ${trip.tripNo}?`}
        description="The customer is emailed this reason - write it for them, and suggest an alternative if you can."
        submitLabel="Reject request"
        destructive
        successMessage="Request rejected"
        successDescription="The customer is emailed your reason."
        onSubmit={async () => {
          await adminTripsApi.reject(trip.tripNo, reason)
          setReason('')
          await queryClient.invalidateQueries({ queryKey: adminTripKeys.all })
        }}
      >
        <FormField label="Reason" htmlFor="reject-reason">
          <Textarea id="reject-reason" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
        </FormField>
      </ActionDialog>
    </div>
  )
}

/**
 * One white panel on the page: title + subtitle on the left, an optional extra on the right.
 * className carries its phone order (order-n), which the wide two-column layout ignores.
 */
function Panel({ title, subtitle, aside, className, children }: { title: string; subtitle?: string; aside?: ReactNode; className?: string; children: ReactNode }) {
  return (
    <section className={cn('grid min-w-0 content-start gap-4 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6 lg:order-none', className)}>
      <header className="flex items-center justify-between gap-4">
        <div className="grid min-w-0 gap-0.5">
          <h2 className="text-base font-bold text-ink-900">{title}</h2>
          {subtitle && <p className="text-xs text-ink-500">{subtitle}</p>}
        </div>
        {aside && <div className="shrink-0">{aside}</div>}
      </header>
      {children}
    </section>
  )
}
