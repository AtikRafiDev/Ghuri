import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { CalendarCheckIcon, CalendarDaysIcon, LockIcon, MoonIcon, PencilLineIcon, SearchXIcon, StarIcon, TimerIcon, UsersIcon, type LucideIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useFieldArray, useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { quoteQuery, type BookingQuote, type QuoteParams } from '@/features/catalog/api/catalog.api'
import { readSelection, toSelectionSearch, type BookingSelection } from '@/features/catalog/lib/bookingSelection'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { FieldMessage, TextField } from '@/shared/components/TextField'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingsApi, type TravellerType } from '../api/bookings.api'
import { CheckoutSteps } from '../components/CheckoutSteps'
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
 * Checkout, step 1 (17-day plan, Day 9): who's travelling and how to reach
 * them. The trip comes from the URL (/checkout?package=…&adults=2…), priced
 * again by the API. Submitting books it - seats held 20 minutes - and moves
 * on to step 2, /checkout/TB100001, to pay.
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
      <PageMessage icon={SearchXIcon} tone="sun" title="This trip can't be booked as chosen" text={toAppError(quote.error).message}>
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
      notify.success('Seats held for 20 minutes', {
        description: `Booking ${booking.bookingNo} is reserved for you - pay before the timer runs out to confirm it.`,
      })
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
      <CheckoutSteps current={1} className="max-w-md" />
      <PageHeader
        variant="display"
        eyebrow="Book your trip"
        title="Traveller details"
        description="Who's going, and how we reach you about the trip. Nothing is charged on this step."
      />

      {/* Laptop: form | summary. Phone: summary first, so the customer sees what they're booking. */}
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_23rem] lg:items-start lg:gap-8">
        <form onSubmit={onSubmit} noValidate className="order-2 grid gap-6 lg:order-1">
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

          <FormSection icon={UsersIcon} title="Who's travelling" subtitle="Names as they appear on each traveller's ID.">
            <ol className="grid divide-y divide-ink-100">
              {fields.map((field, index) => {
                const inputId = `traveller-${index}`
                const error = errors.travellers?.[index]?.fullName?.message
                return (
                  <li key={field.id} className="grid gap-2 py-4 first:pt-0 last:pb-0 sm:grid-cols-[12rem_minmax(0,1fr)] sm:gap-4">
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
            {errors.travellers?.message && <p className="text-[0.8125rem] font-medium text-destructive">{errors.travellers.message}</p>}
          </FormSection>

          <FormSection icon={PencilLineIcon} title="Contact details" subtitle="Where we send the receipt, the voucher and any trip updates.">
            <div className="grid gap-5">
              <TextField label="Contact name" autoComplete="name" error={errors.contactName?.message} {...form.register('contactName')} />
              <div className="grid gap-5 sm:grid-cols-2">
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
              <FormField label="Special requests (optional)" htmlFor="specialRequest" error={errors.specialRequest?.message}>
                <Textarea
                  id="specialRequest"
                  rows={3}
                  placeholder="Dietary needs, seating, anything we should know"
                  aria-invalid={errors.specialRequest ? true : undefined}
                  {...form.register('specialRequest')}
                />
              </FormField>
            </div>
          </FormSection>

          <div className="grid gap-3">
            <Button type="submit" size="lg" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? <Spinner /> : <LockIcon />}
              Book and continue to payment
            </Button>
            <p className="text-center text-sm text-balance text-ink-500">
              <TimerIcon className="mr-1.5 inline size-4 -translate-y-px align-middle text-ink-400" />
              Your seats are held for 20 minutes while you pay. Nothing is charged until you pay.
            </p>
          </div>
        </form>

        <aside className="order-1 lg:sticky lg:top-24 lg:order-2">
          <TripSummary quote={quote} packagePath={packagePath} />
        </aside>
      </div>
    </div>
  )
}

/** One white panel of the form: an icon tile, the title and a line under it, then the fields. */
function FormSection({ icon: Icon, title, subtitle, children }: { icon: LucideIcon; title: string; subtitle: string; children: ReactNode }) {
  return (
    <fieldset className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <legend className="sr-only">{title}</legend>
      <div aria-hidden className="flex items-center gap-3">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-600">
          <Icon className="size-[18px]" />
        </span>
        <div className="grid gap-0.5">
          <p className="text-base font-bold text-ink-900">{title}</p>
          <p className="text-sm text-ink-500">{subtitle}</p>
        </div>
      </div>
      {children}
    </fieldset>
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
    <section className="overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80">
      {/* Dark brand header: what's being booked. */}
      <div className="brand-surface relative isolate overflow-hidden bg-gradient-to-br from-forest-700 to-forest-950 px-5 py-5 text-white sm:px-6">
        <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
        <div aria-hidden className="absolute -top-16 -right-10 -z-10 size-40 rounded-full bg-sun-500/20 blur-3xl" />
        <p className="text-[0.6875rem] font-semibold tracking-wider text-forest-100/70 uppercase">Your trip</p>
        <h2 className="mt-1 text-lg leading-snug font-bold">{quote.packageTitle}</h2>
      </div>

      <div className="grid gap-5 p-5 sm:p-6">
        <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2.5 text-sm">
          <SummaryRow icon={CalendarDaysIcon} label="Starts">
            {formatDate(quote.startDate)}
          </SummaryRow>
          <SummaryRow icon={CalendarCheckIcon} label="Ends">
            {formatDate(quote.endDate)}
          </SummaryRow>
          <SummaryRow icon={MoonIcon} label="Stay">
            {quote.nights} night{quote.nights === 1 ? '' : 's'}
          </SummaryRow>
          <SummaryRow icon={UsersIcon} label="Travellers">
            {people.join(', ')}
          </SummaryRow>
        </dl>

        {/* The price, line by line: the amounts form one right-aligned column, the total under them. */}
        <dl className="grid gap-3 border-t border-dashed border-ink-200 pt-5 text-sm">
          {quote.lines.map((line) => (
            <div key={line.label} className="flex items-start justify-between gap-4">
              <dt className="grid gap-0.5">
                <span className="font-medium text-ink-900">{line.label}</span>
                <span className="nums text-xs text-ink-500">
                  {formatTaka(line.unitPrice)} × {line.quantity}
                </span>
              </dt>
              <dd className="nums font-medium text-ink-900">{formatTaka(line.amount)}</dd>
            </div>
          ))}
          <div className="mt-1 flex items-center justify-between gap-4 border-t border-ink-200 pt-4">
            <dt className="grid">
              <span className="font-semibold text-ink-900">Total</span>
              <span className="text-xs text-ink-500">For everyone travelling</span>
            </dt>
            <dd className="text-2xl font-bold tracking-tight text-forest-800">{formatTaka(quote.total)}</dd>
          </div>
        </dl>

        {quote.pricingMode === 2 && (
          <p className="rounded-xl bg-sun-50 px-3.5 py-2.5 text-xs text-sun-700 ring-1 ring-sun-100 ring-inset">
            Hotel rooms are confirmed within 24 hours - if they can't be, you get a full refund.
          </p>
        )}

        <Button asChild variant="outline" className="w-full">
          <Link to={packagePath}>Change date or travellers</Link>
        </Button>
      </div>
    </section>
  )
}

/** A label | value row of the summary - the values line up in one column. */
function SummaryRow({ icon: Icon, label, children }: { icon: LucideIcon; label: string; children: ReactNode }) {
  return (
    <>
      <dt className="flex items-center gap-2 text-ink-500">
        <Icon className="size-4 text-ink-400" />
        {label}
      </dt>
      <dd className="text-right font-medium text-ink-900">{children}</dd>
    </>
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

/** "Adult 1", "Child 1", "Infant 1 (under 2)" - the lead traveller gets a badge beside it. */
function travellerLabel(rows: CheckoutInput['travellers'], index: number): string {
  const row = rows[index]
  const number = rows.slice(0, index + 1).filter((r) => r.type === row.type).length
  const kind = row.type === 1 ? 'Adult' : row.type === 2 ? 'Child' : 'Infant'
  return `${kind} ${number}${row.type === 3 ? ' (under 2)' : ''}`
}
