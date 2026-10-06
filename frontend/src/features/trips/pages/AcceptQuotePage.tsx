import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangleIcon, ArrowLeftIcon, ClockIcon, LockIcon, StarIcon, TimerIcon, TimerOffIcon, UsersIcon } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, Navigate, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useSecondsLeft } from '@/features/booking/lib/useSecondsLeft'
import { CheckoutSteps } from '@/features/booking/components/CheckoutSteps'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { FieldMessage } from '@/shared/components/TextField'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { useAuth } from '@/features/auth/useAuth'
import { myTripQuery, quoteLineLabels, tripsApi, type Trip } from '../api/trips.api'
import { formatTimeLeft } from '../lib/timeLeft'
import { RoutePath } from '../components/RoutePath'

// The same rules as the API (AcceptCustomTripQuoteValidator); the rows - who is
// an adult, a child, the lead - come from the trip, only the names are typed.
const acceptSchema = z.object({
  travellers: z.array(
    z.object({
      type: z.union([z.literal(1), z.literal(2), z.literal(3)]),
      isLead: z.boolean(),
      fullName: z.string().trim().min(1, "Enter the traveller's full name.").max(150, 'At most 150 characters.'),
    }),
  ),
  specialRequest: z.string().trim().max(1000, 'At most 1000 characters.'),
})
type AcceptInput = z.infer<typeof acceptSchema>

/**
 * /account/trips/:tripNo/accept - "Accept & pay" (17-day plan, Day 15): the
 * names of everyone travelling (decided 2026-10-06), then the booking is
 * made and the customer pays on the usual payment page. For a custom trip
 * this is step 1 of the same three booking steps.
 */
export function AcceptQuotePage() {
  const { tripNo = '' } = useParams()
  useDocumentMeta({ title: `Accept quote ${tripNo}` })
  const trip = useQuery(myTripQuery(tripNo))

  if (trip.isPending) return <PageSpinner />
  if (trip.isError) {
    return (
      <PageMessage icon={AlertTriangleIcon} tone="clay" title="This trip couldn't be loaded" text={toAppError(trip.error).message}>
        <Button asChild variant="outline">
          <Link to="/account/trips">My trips</Link>
        </Button>
      </PageMessage>
    )
  }

  // Already accepted and waiting for payment: straight to paying it.
  if (trip.data.booking?.status === 1) return <Navigate to={`/checkout/${encodeURIComponent(trip.data.booking.bookingNo)}`} replace />

  if (trip.data.status !== 2 || !trip.data.quote || trip.data.quote.isExpired) {
    return (
      <PageMessage
        icon={TimerOffIcon}
        tone="sun"
        title="There's no quote to accept"
        text="This quote has run out or was replaced. Open the trip to see where it stands."
      >
        <Button asChild>
          <Link to={`/account/trips/${encodeURIComponent(tripNo)}`}>Open the trip</Link>
        </Button>
      </PageMessage>
    )
  }

  return <AcceptForm trip={trip.data} />
}

function AcceptForm({ trip }: { trip: Trip }) {
  const quote = trip.quote!
  const { user } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const secondsLeft = useSecondsLeft(quote.expiresAtUtc)

  const form = useForm<AcceptInput>({
    resolver: zodResolver(acceptSchema),
    defaultValues: { travellers: travellerRows(trip, user?.fullName ?? ''), specialRequest: '' },
  })
  const { fields } = useFieldArray({ control: form.control, name: 'travellers' })
  const { errors, isSubmitting } = form.formState
  const tripPath = `/account/trips/${encodeURIComponent(trip.tripNo)}`

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const booking = await tripsApi.accept(trip.tripNo, { travellers: values.travellers, specialRequest: values.specialRequest || null })
      notify.success('Quote accepted', { description: `Booking ${booking.bookingNo} is held for 20 minutes - pay to confirm your trip.` })
      // The usual payment page: countdown, "Pay", SSLCommerz. replace: Back goes to the trip, not to this form.
      navigate(`/checkout/${encodeURIComponent(booking.bookingNo)}`, { replace: true })
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <div className="grid gap-6">
      <CheckoutSteps current={1} className="max-w-md" />
      <PageHeader
        eyebrow={
          <Button asChild variant="ghost" size="sm" className="-ml-3">
            <Link to={tripPath}>
              <ArrowLeftIcon />
              Back to the trip
            </Link>
          </Button>
        }
        title="Accept your quote"
        description={`${trip.legs.map((l) => l.destinationName).join(' → ')} · ${formatDate(trip.startDate)} → ${formatDate(trip.endDate)}`}
      />

      {/* Laptop: form | quote. Phone: the quote first, so the customer sees what they're accepting. */}
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-start">
        <form onSubmit={onSubmit} noValidate className="order-2 grid min-w-0 gap-6 lg:order-1">
          {formError && <FormAlert kind="error">{formError}</FormAlert>}

          <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
            <header className="flex items-center gap-3">
              <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-600">
                <UsersIcon className="size-[18px]" />
              </span>
              <div className="grid gap-0.5">
                <h2 className="text-base font-bold text-ink-900">Who's travelling</h2>
                <p className="text-sm text-ink-500">As on their ID - the names go on the voucher the hotels see.</p>
              </div>
            </header>

            <ol className="grid divide-y divide-ink-100">
              {fields.map((field, index) => {
                const inputId = `traveller-${index}`
                const error = errors.travellers?.[index]?.fullName?.message
                return (
                  <li key={field.id} className="grid gap-2 py-4 first:pt-0 last:pb-0 sm:grid-cols-[11rem_minmax(0,1fr)] sm:gap-4">
                    {/* Who this row is - its number, kind and (for the first adult) the lead badge - centred on the input. */}
                    <div className="flex items-center gap-3 sm:h-10">
                      <span
                        className={cn(
                          'flex size-8 shrink-0 items-center justify-center rounded-full text-xs font-bold',
                          field.isLead ? 'bg-primary text-white' : 'bg-forest-50 text-forest-700 ring-1 ring-forest-100 ring-inset',
                        )}
                      >
                        {index + 1}
                      </span>
                      <Label htmlFor={inputId}>
                        {travellerLabel(fields, index)}
                        {field.isLead && <span className="sr-only"> · lead traveller</span>}
                      </Label>
                      {field.isLead && (
                        <Badge variant="warning" className="h-5 px-2 text-[0.6875rem]">
                          <StarIcon className="fill-current" />
                          Lead
                        </Badge>
                      )}
                    </div>
                    <div className="grid content-start gap-2">
                      <Input
                        id={inputId}
                        autoComplete={index === 0 ? 'name' : 'off'}
                        placeholder="Full name"
                        aria-invalid={error ? true : undefined}
                        aria-describedby={error ? `${inputId}-message` : undefined}
                        {...form.register(`travellers.${index}.fullName`)}
                      />
                      <FieldMessage id={`${inputId}-message`} error={error} />
                    </div>
                  </li>
                )
              })}
            </ol>

            <FormField label="Anything else we should know? (optional)" htmlFor="specialRequest" error={errors.specialRequest?.message}>
              <Textarea
                id="specialRequest"
                maxLength={1000}
                placeholder="Dietary needs, a late check-in, anything that helps"
                aria-invalid={errors.specialRequest ? true : undefined}
                {...form.register('specialRequest')}
              />
            </FormField>
          </section>

          <div className="grid gap-3">
            <Button type="submit" size="lg" className="w-full" disabled={isSubmitting || secondsLeft === 0}>
              {isSubmitting ? <Spinner /> : <LockIcon />}
              Accept & go to payment
            </Button>
            <p className="text-center text-sm text-balance text-ink-500">
              <TimerIcon className="mr-1.5 inline size-4 -translate-y-px align-middle text-ink-400" />
              You then have 20 minutes to pay. bKash, Nagad and cards through SSLCommerz.
            </p>
          </div>
        </form>

        <QuoteSummary trip={trip} secondsLeft={secondsLeft} />
      </div>
    </div>
  )
}

/** The quote being accepted: the route, the lines, the total, and how long the price stands. */
function QuoteSummary({ trip, secondsLeft }: { trip: Trip; secondsLeft: number | null }) {
  const quote = trip.quote!
  return (
    <aside className="order-1 overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80 lg:sticky lg:top-24 lg:order-2">
      {/* Dark brand header: the trip and its price. */}
      <div className="brand-surface relative isolate grid gap-3 overflow-hidden bg-gradient-to-br from-forest-700 to-forest-950 px-5 py-5 text-white">
        <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
        <div aria-hidden className="absolute -top-16 -right-10 -z-10 size-40 rounded-full bg-sun-500/20 blur-3xl" />
        <h2 className="text-[0.6875rem] font-semibold tracking-wider text-forest-100/70 uppercase">Your quote · {trip.tripNo}</h2>
        <RoutePath stops={trip.legs.map((l) => l.destinationName)} onDark className="text-sm" />
        <div className="grid">
          <span className="text-xs text-forest-100/75">Total for everyone</span>
          <span className="text-3xl leading-tight font-bold tracking-tight">{formatTaka(quote.total)}</span>
        </div>
      </div>

      <div className="grid gap-4 p-5">
        <dl className="grid gap-2.5 text-sm">
          {quote.lines.map((line, i) => (
            <div key={i} className="flex items-baseline justify-between gap-4">
              <dt className="min-w-0 truncate text-ink-500" title={line.description}>
                {quoteLineLabels[line.category]}
              </dt>
              <dd className="nums shrink-0 font-medium text-ink-900">{formatTaka(line.amount)}</dd>
            </div>
          ))}
          <div className="flex items-baseline justify-between gap-4 border-t border-ink-200 pt-3">
            <dt className="font-semibold text-ink-900">Total</dt>
            <dd className="nums font-bold text-ink-900">{formatTaka(quote.total)}</dd>
          </div>
        </dl>
        {secondsLeft !== null && (
          <p className="flex items-center gap-2 rounded-xl bg-sun-50 px-3.5 py-2.5 text-xs font-medium text-sun-700 ring-1 ring-sun-100 ring-inset">
            <ClockIcon className="size-4 shrink-0" />
            This price stands for {formatTimeLeft(secondsLeft)} more.
          </p>
        )}
      </div>
    </aside>
  )
}

/** One row per traveller the quote was priced for: adults first (the first is the lead), then children, then infants. */
function travellerRows(trip: Trip, leadName: string): AcceptInput['travellers'] {
  const rows: AcceptInput['travellers'] = []
  const add = (type: 1 | 2 | 3, count: number) => {
    for (let i = 0; i < count; i++) rows.push({ type, isLead: rows.length === 0, fullName: rows.length === 0 ? leadName : '' })
  }
  add(1, trip.adults)
  add(2, trip.children)
  add(3, trip.infants)
  return rows
}

/** "Adult 1", "Child 1", "Infant 1 (under 2)" - the lead traveller gets a badge beside it. */
function travellerLabel(rows: { type: 1 | 2 | 3; isLead: boolean }[], index: number): string {
  const row = rows[index]
  const number = rows.slice(0, index + 1).filter((r) => r.type === row.type).length
  const kind = row.type === 1 ? 'Adult' : row.type === 2 ? 'Child' : 'Infant'
  return `${kind} ${number}${row.type === 3 ? ' (under 2)' : ''}`
}
