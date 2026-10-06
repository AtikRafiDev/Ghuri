import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon, LockIcon } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, Navigate, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useSecondsLeft } from '@/features/booking/lib/useSecondsLeft'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { TextField } from '@/shared/components/TextField'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { useAuth } from '@/features/auth/useAuth'
import { myTripQuery, tripsApi, type Trip } from '../api/trips.api'
import { formatTimeLeft } from '../lib/timeLeft'

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
 * made and the customer pays on the usual payment page.
 */
export function AcceptQuotePage() {
  const { tripNo = '' } = useParams()
  useDocumentMeta({ title: `Accept quote ${tripNo}` })
  const trip = useQuery(myTripQuery(tripNo))

  if (trip.isPending) return <PageSpinner />
  if (trip.isError) {
    return (
      <PageMessage title="This trip couldn't be loaded" text={toAppError(trip.error).message}>
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
      <PageMessage title="There's no quote to accept" text="This quote has run out or was replaced. Open the trip to see where it stands.">
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
      // The usual payment page: countdown, "Pay", SSLCommerz. replace: Back goes to the trip, not to this form.
      navigate(`/checkout/${encodeURIComponent(booking.bookingNo)}`, { replace: true })
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <div className="mx-auto grid max-w-xl gap-6">
      <Link to={tripPath} className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-4" /> Back to the trip
      </Link>

      <div className="grid gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Accept your quote</h1>
        <p className="text-muted-foreground">
          {trip.legs.map((l) => l.destinationName).join(' → ')} · {formatDate(trip.startDate)} → {formatDate(trip.endDate)}
        </p>
      </div>

      <div className="flex items-baseline justify-between gap-2 rounded-xl border-2 border-primary bg-card p-4">
        <span>Total for everyone</span>
        <span className="text-2xl font-semibold tabular-nums">{formatTaka(quote.total)}</span>
      </div>
      {secondsLeft !== null && (
        <p className="text-sm text-muted-foreground">This price stands for {formatTimeLeft(secondsLeft)} more.</p>
      )}

      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}

        <section className="grid gap-3 rounded-xl border bg-card p-4">
          <h2 className="font-semibold">Who's travelling</h2>
          <p className="text-sm text-muted-foreground">As on their ID - the names go on the voucher the hotels see.</p>
          {fields.map((field, index) => (
            <TextField
              key={field.id}
              id={`traveller-${index}`}
              label={travellerLabel(fields, index)}
              autoComplete={index === 0 ? 'name' : 'off'}
              error={errors.travellers?.[index]?.fullName?.message}
              {...form.register(`travellers.${index}.fullName`)}
            />
          ))}
        </section>

        <div className="grid gap-1.5">
          <Label htmlFor="specialRequest">Anything else we should know? (optional)</Label>
          <Textarea id="specialRequest" maxLength={1000} {...form.register('specialRequest')} />
        </div>

        <Button type="submit" size="lg" className="h-11 text-base" disabled={isSubmitting || secondsLeft === 0}>
          {isSubmitting ? <Spinner /> : <LockIcon />}
          Accept & go to payment
        </Button>
        <p className="text-center text-xs text-muted-foreground">
          You then have 20 minutes to pay. bKash, Nagad and cards through SSLCommerz.
        </p>
      </form>
    </div>
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

/** "Adult 1 · lead traveller", "Child 1", "Infant 1 (under 2)". */
function travellerLabel(rows: { type: 1 | 2 | 3; isLead: boolean }[], index: number): string {
  const row = rows[index]
  const number = rows.slice(0, index + 1).filter((r) => r.type === row.type).length
  const kind = row.type === 1 ? 'Adult' : row.type === 2 ? 'Child' : 'Infant'
  return `${kind} ${number}${row.isLead ? ' · lead traveller' : row.type === 3 ? ' (under 2)' : ''}`
}
