import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { LockIcon } from 'lucide-react'
import { useState } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { quoteQuery, type BookingQuote, type QuoteParams } from '@/features/catalog/api/catalog.api'
import { PriceBreakdown } from '@/features/catalog/components/PriceBreakdown'
import { readSelection, toSelectionSearch, type BookingSelection } from '@/features/catalog/lib/bookingSelection'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { TextField } from '@/shared/components/TextField'
import { formatDate } from '@/shared/lib/dates'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingsApi, type TravellerType } from '../api/bookings.api'
import { PageMessage } from '../components/PageMessage'
import { forgetIdempotencyKey, idempotencyKeyFor } from '../lib/checkoutSession'
import { checkoutSchema, type CheckoutInput } from '../schemas/checkout.schema'

/** Answers that mean "this trip can't be booked as chosen" - the fix is choosing again on the package page. */
const tripProblems = new Set([
  'not_enough_seats',
  'booking_closed',
  'departure_not_bookable',
  'departure_not_found',
  'start_date_too_soon',
  'nights_out_of_range',
  'package_not_found',
])

/**
 * Checkout, step 1 of 2 (17-day plan, Day 9): who's travelling and how to
 * reach them. The trip comes from the URL (/checkout?package=…&adults=2…),
 * priced again by the API. Submitting books it - seats held 20 minutes -
 * and moves on to step 2, /checkout/TB100001, to pay.
 */
export function CheckoutPage() {
  useDocumentMeta({ title: 'Checkout' })
  const [search] = useSearchParams()
  const selection = readSelection(search)
  const quote = useQuery(quoteQuery(selection?.slug ?? '', selection?.params ?? null))

  if (!selection) {
    return (
      <PageMessage title="Nothing to book yet" text="Choose a package and a date first.">
        <Button asChild>
          <Link to="/packages">See packages</Link>
        </Button>
      </PageMessage>
    )
  }

  if (quote.isPending) return <PageSpinner />

  if (quote.isError) {
    // E.g. the seats went while the customer was logging in - the API's message says so.
    return (
      <PageMessage title="This trip can't be booked as chosen" text={toAppError(quote.error).message}>
        <Button asChild variant="outline">
          <Link to={`/packages/${encodeURIComponent(selection.slug)}`}>Back to the package</Link>
        </Button>
      </PageMessage>
    )
  }

  return <CheckoutForm selection={selection} quote={quote.data} />
}

function CheckoutForm({ selection, quote }: { selection: BookingSelection; quote: BookingQuote }) {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<{ message: string; tripProblem: boolean } | null>(null)
  // The same trip choice → the same text → the same Idempotency-Key (checkoutSession.ts).
  const selectionKey = toSelectionSearch(selection)

  const form = useForm<CheckoutInput>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: {
      travellers: travellerRows(selection.params, user?.fullName ?? ''),
      contactName: user?.fullName ?? '',
      contactPhone: user?.phone ?? '',
      contactEmail: user?.email ?? '',
      specialRequest: '',
    },
  })
  const { fields } = useFieldArray({ control: form.control, name: 'travellers' })
  const { errors, isSubmitting } = form.formState
  const packagePath = `/packages/${encodeURIComponent(selection.slug)}`

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const booking = await bookingsApi.create(
        {
          packageSlug: selection.slug,
          ...('departureId' in selection.params
            ? { departureId: selection.params.departureId }
            : { startDate: selection.params.startDate, nights: selection.params.nights }),
          travellers: values.travellers.map(({ type, fullName, isLead }) => ({ type, fullName, isLead })),
          contactName: values.contactName,
          contactPhone: values.contactPhone,
          contactEmail: values.contactEmail,
          specialRequest: values.specialRequest || null,
        },
        idempotencyKeyFor(selectionKey),
      )
      forgetIdempotencyKey(selectionKey)
      // replace: Back from the payment step goes to the package, never to this
      // form again (which would book a second time).
      navigate(`/checkout/${booking.bookingNo}`, { replace: true })
    } catch (error) {
      const message = applyServerErrors(form, error)
      if (message) setFormError({ message, tripProblem: tripProblems.has(toAppError(error).code) })
    }
  })

  return (
    <div className="grid gap-6">
      <div className="grid gap-1">
        <p className="text-sm font-medium text-muted-foreground">Step 1 of 2</p>
        <h1 className="text-2xl font-semibold tracking-tight">Traveller details</h1>
      </div>

      {/* Laptop: form | summary. Phone: summary first, so the customer sees what they're booking. */}
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem] lg:items-start">
        <form onSubmit={onSubmit} noValidate className="order-2 grid gap-8 lg:order-1">
          {formError && (
            <FormAlert kind="error">
              {formError.message}{' '}
              {formError.tripProblem && (
                <Link to={packagePath} className="font-medium underline underline-offset-4">
                  Choose another date
                </Link>
              )}
            </FormAlert>
          )}

          <fieldset className="grid gap-4">
            <legend className="mb-3 text-lg font-semibold">Who's travelling</legend>
            <p className="-mt-2 text-sm text-muted-foreground">Names as they appear on each traveller's ID.</p>
            {fields.map((field, index) => (
              <TextField
                key={field.id}
                label={travellerLabel(fields, index)}
                autoComplete={index === 0 ? 'name' : 'off'}
                error={errors.travellers?.[index]?.fullName?.message}
                {...form.register(`travellers.${index}.fullName`)}
              />
            ))}
            {errors.travellers?.message && <p className="text-sm text-destructive">{errors.travellers.message}</p>}
          </fieldset>

          <fieldset className="grid gap-4">
            <legend className="mb-3 text-lg font-semibold">Contact details</legend>
            <TextField label="Contact name" autoComplete="name" error={errors.contactName?.message} {...form.register('contactName')} />
            <div className="grid gap-4 sm:grid-cols-2">
              <TextField
                label="Mobile number"
                type="tel"
                autoComplete="tel"
                placeholder="01711000000"
                error={errors.contactPhone?.message}
                {...form.register('contactPhone')}
              />
              <TextField
                label="Email"
                type="email"
                autoComplete="email"
                hint="Your payment receipt and voucher are sent here."
                error={errors.contactEmail?.message}
                {...form.register('contactEmail')}
              />
            </div>
            <div className="grid gap-1.5">
              <Label htmlFor="specialRequest">Special requests (optional)</Label>
              <Textarea
                id="specialRequest"
                rows={3}
                placeholder="Dietary needs, seating, anything we should know"
                aria-invalid={errors.specialRequest ? true : undefined}
                {...form.register('specialRequest')}
              />
              {errors.specialRequest && <p className="text-sm text-destructive">{errors.specialRequest.message}</p>}
            </div>
          </fieldset>

          <div className="grid gap-2">
            <Button type="submit" size="lg" className="h-11 text-base" disabled={isSubmitting}>
              {isSubmitting ? <Spinner /> : <LockIcon />}
              Book and continue to payment
            </Button>
            <p className="text-sm text-muted-foreground">
              Your seats are held for 20 minutes while you pay. Nothing is charged until you pay.
            </p>
          </div>
        </form>

        <aside className="order-1 lg:sticky lg:top-4 lg:order-2">
          <TripSummary quote={quote} packagePath={packagePath} />
        </aside>
      </div>
    </div>
  )
}

/** The trip being booked, priced by the API - the same lines the customer saw on the package page. */
function TripSummary({ quote, packagePath }: { quote: BookingQuote; packagePath: string }) {
  const people = [
    `${quote.adults} adult${quote.adults === 1 ? '' : 's'}`,
    quote.children > 0 && `${quote.children} child${quote.children === 1 ? '' : 'ren'}`,
    quote.infants > 0 && `${quote.infants} infant${quote.infants === 1 ? '' : 's'}`,
  ].filter(Boolean)

  return (
    <section className="grid gap-4 rounded-xl border bg-card p-4">
      <div className="grid gap-1">
        <h2 className="font-semibold">{quote.packageTitle}</h2>
        <p className="text-sm text-muted-foreground">
          {formatDate(quote.startDate)} → {formatDate(quote.endDate)} · {quote.nights} night{quote.nights === 1 ? '' : 's'}
        </p>
        <p className="text-sm text-muted-foreground">{people.join(', ')}</p>
      </div>
      <PriceBreakdown quote={quote} />
      {quote.pricingMode === 2 && (
        <p className="text-xs text-muted-foreground">Hotel rooms are confirmed within 24 hours - if they can't be, you get a full refund.</p>
      )}
      <Link to={packagePath} className="text-sm text-muted-foreground underline underline-offset-4 hover:text-foreground">
        Change date or travellers
      </Link>
    </section>
  )
}

/** One row per traveller, adults first; the first adult is the lead traveller, pre-filled with the account's name. */
function travellerRows(params: QuoteParams, leadName: string): CheckoutInput['travellers'] {
  const rows: CheckoutInput['travellers'] = []
  const add = (type: TravellerType, count: number) => {
    for (let i = 0; i < count; i++) rows.push({ type, isLead: rows.length === 0, fullName: rows.length === 0 ? leadName : '' })
  }
  add(1, params.adults)
  add(2, params.children ?? 0)
  add(3, params.infants ?? 0)
  return rows
}

/** "Adult 1 · lead traveller", "Child 1", "Infant 1 (under 2)". */
function travellerLabel(rows: CheckoutInput['travellers'], index: number): string {
  const row = rows[index]
  const number = rows.slice(0, index + 1).filter((r) => r.type === row.type).length
  const kind = row.type === 1 ? 'Adult' : row.type === 2 ? 'Child' : 'Infant'
  return `${kind} ${number}${row.isLead ? ' · lead traveller' : row.type === 3 ? ' (under 2)' : ''}`
}
