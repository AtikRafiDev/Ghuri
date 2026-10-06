import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowDownIcon,
  ArrowUpIcon,
  BedDoubleIcon,
  BedSingleIcon,
  CalendarDaysIcon,
  CheckIcon,
  HotelIcon,
  MapIcon,
  MinusIcon,
  PlusIcon,
  SendIcon,
  SparklesIcon,
  Trash2Icon,
  TrainFrontIcon,
  type LucideIcon,
} from 'lucide-react'
import { useState, type ComponentProps, type ReactNode } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { destinationsQuery } from '@/features/catalog/api/catalog.api'
import { cn } from '@/lib/utils'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { FieldMessage, TextField } from '@/shared/components/TextField'
import { addDays, formatDate, todayInBangladesh } from '@/shared/lib/dates'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { hotelLevels, tripLimits, tripsApi, transferModes, type HotelLevel, type TransferMode } from '../api/trips.api'
import { planTripSchema, type PlanTripInput } from '../schemas/planTrip.schema'

const newLeg = { destinationId: '', nights: 2, transferToNext: 2 as TransferMode }

/** An icon for each hotel level's card: a single bed, a double bed, a touch of sparkle. */
const hotelIcons: Record<HotelLevel, LucideIcon> = { 1: BedSingleIcon, 2: BedDoubleIcon, 3: SparklesIcon }

/** A number box without the browser's tiny up/down arrows (the stepper buttons or the suffix take that place). */
const noSpinners = '[appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none'

/** The traveller counters: field, label, the age band under it, and the lowest allowed. */
const travellerCounts = [
  { name: 'adults', label: 'Adults', hint: '12 years and over', lowest: 1 },
  { name: 'children', label: 'Children', hint: '2-11 years', lowest: 0 },
  { name: 'infants', label: 'Infants', hint: 'Under 2', lowest: 0 },
] as const

/**
 * "Plan my trip" (17-day plan, Day 14): the customer lists the places they
 * want to see, in order, with the nights at each - the dates of every stop
 * are worked out live as they type. Staff then send a price (Day 13's queue).
 */
export function PlanTripPage() {
  useDocumentMeta({ title: 'Plan my trip', description: 'Tell us where you want to go - we plan it and send you a price.' })
  const navigate = useNavigate()
  const destinations = useQuery(destinationsQuery(false))
  const [formError, setFormError] = useState<string | null>(null)
  const earliest = addDays(todayInBangladesh(), tripLimits.minLeadDays)

  const form = useForm<PlanTripInput>({
    resolver: zodResolver(planTripSchema),
    defaultValues: {
      startDate: '',
      adults: 2,
      children: 0,
      infants: 0,
      hotelLevel: 2,
      budgetPerPerson: null,
      notes: '',
      legs: [newLeg],
    },
  })
  const { fields, append, remove, move } = useFieldArray({ control: form.control, name: 'legs' })
  const { errors, isSubmitting } = form.formState
  const [startDate, legs, adults, children, infants, hotelLevel] = useWatch({
    control: form.control,
    name: ['startDate', 'legs', 'adults', 'children', 'infants', 'hotelLevel'],
  })
  const counts = { adults, children, infants }
  const stops = stopDates(startDate, legs ?? [])

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const trip = await tripsApi.submit({
        ...values,
        notes: values.notes || null,
        legs: values.legs.map((leg, i) => ({
          ...leg,
          transferToNext: i === values.legs.length - 1 ? 1 : leg.transferToNext, // nowhere to go after the last stop
        })),
      })
      notify.success('Trip request sent!', { description: "Our team is planning it now - we'll email you a price, usually within 24 hours." })
      navigate(`/account/trips/${encodeURIComponent(trip.tripNo)}`, { replace: true, state: { justSubmitted: true } })
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  /** The − / + buttons: one more or one fewer, kept within the limits; checked again only once the form was sent. */
  const step = (name: (typeof travellerCounts)[number]['name'], lowest: number, delta: number) => {
    const current = Number.isFinite(counts[name]) ? counts[name] : 0
    const next = Math.min(tripLimits.maxPeople, Math.max(lowest, current + delta))
    form.setValue(name, next, { shouldDirty: true, shouldValidate: form.formState.isSubmitted })
  }

  const destinationName = (id: string) => destinations.data?.find((d) => d.id === id)?.name

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Plan my trip"
        description="Add the places you want to visit, in order. We plan the hotels and transport and send you a price - usually within 24 hours."
      />

      <form onSubmit={onSubmit} noValidate className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_23rem] lg:gap-8">
        <div className="grid gap-6">
          {formError && <FormAlert kind="error">{formError}</FormAlert>}

          <Panel icon={CalendarDaysIcon} title="When and who" subtitle="The first day of the trip, and everyone going.">
            <div className="sm:max-w-xs">
              <TextField
                label="First day"
                type="date"
                min={earliest}
                hint={`${formatDate(earliest)} or later`}
                error={errors.startDate?.message}
                {...form.register('startDate')}
              />
            </div>

            <fieldset className="grid gap-2">
              <legend className="mb-2 text-[0.8125rem] font-semibold text-ink-700">Travellers</legend>
              <div className="grid divide-y divide-ink-100 rounded-2xl ring-1 ring-ink-200/80">
                {travellerCounts.map(({ name, label, hint, lowest }) => (
                  <CountRow
                    key={name}
                    id={name}
                    label={label}
                    hint={hint}
                    value={counts[name]}
                    lowest={lowest}
                    error={errors[name]?.message}
                    onStep={(delta) => step(name, lowest, delta)}
                    {...form.register(name, { valueAsNumber: true })}
                  />
                ))}
              </div>
            </fieldset>
          </Panel>

          <Panel
            icon={MapIcon}
            title="Where to"
            subtitle="Your stops in order - each one starts the day the one before ends."
            aside={
              <span className="nums rounded-full bg-ink-100 px-2.5 py-1 text-xs font-semibold text-ink-600">
                {fields.length} of {tripLimits.maxLegs}
              </span>
            }
          >
            {destinations.isError && <FormAlert kind="error">The destinations couldn't be loaded - please refresh the page.</FormAlert>}

            <div>
              <ol className="grid">
                {fields.map((field, index) => {
                  const isLast = index === fields.length - 1
                  const legErrors = errors.legs?.[index]
                  const stop = stops[index]
                  return (
                    <li key={field.id} className="relative grid grid-cols-[2.25rem_minmax(0,1fr)] gap-x-3 pb-4 sm:grid-cols-[2.5rem_minmax(0,1fr)] sm:gap-x-4">
                      {/* The route: a dashed line from this stop's node down to the next node (or the "add" node). */}
                      <span aria-hidden className="absolute top-8 -bottom-8 left-[17px] border-l-2 border-dashed border-forest-200 sm:left-[19px]" />
                      <span className="relative z-10 mt-3 flex size-9 items-center justify-center rounded-full bg-primary text-sm font-bold text-white shadow-[0_6px_16px_-6px_rgb(23_88_63/0.7)] ring-4 ring-card sm:size-10">
                        {index + 1}
                      </span>

                      <div className="grid gap-4 rounded-2xl bg-card p-4 ring-1 ring-ink-200/80 transition-shadow duration-200 hover:shadow-soft">
                        <div className="flex items-center justify-between gap-2">
                          <div className="grid min-w-0 gap-0.5">
                            <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Stop {index + 1}</span>
                            <span className="nums truncate text-sm font-medium text-ink-600">
                              {stop?.from ? `${shortDate(stop.from)} → ${shortDate(stop.to)}` : 'No dates yet'}
                            </span>
                          </div>
                          <div className="flex shrink-0 gap-0.5">
                            <Button type="button" variant="ghost" size="icon-sm" aria-label="Move up" disabled={index === 0} onClick={() => move(index, index - 1)}>
                              <ArrowUpIcon />
                            </Button>
                            <Button type="button" variant="ghost" size="icon-sm" aria-label="Move down" disabled={isLast} onClick={() => move(index, index + 1)}>
                              <ArrowDownIcon />
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon-sm"
                              aria-label="Remove"
                              className="hover:bg-clay-50 hover:text-clay-600"
                              disabled={fields.length === 1}
                              onClick={() => remove(index)}
                            >
                              <Trash2Icon />
                            </Button>
                          </div>
                        </div>

                        <div className="grid gap-4 sm:grid-cols-[minmax(0,1fr)_9rem]">
                          <FormField label="Destination" htmlFor={`leg-${index}-destination`} error={legErrors?.destinationId?.message}>
                            <Controller
                              control={form.control}
                              name={`legs.${index}.destinationId`}
                              render={({ field: f }) => (
                                <Select value={f.value} onValueChange={f.onChange} disabled={destinations.isPending}>
                                  <SelectTrigger
                                    id={`leg-${index}-destination`}
                                    className="w-full"
                                    aria-invalid={legErrors?.destinationId ? true : undefined}
                                    aria-describedby={legErrors?.destinationId ? `leg-${index}-destination-message` : undefined}
                                  >
                                    <SelectValue placeholder={destinations.isPending ? 'Loading…' : 'Choose a place'} />
                                  </SelectTrigger>
                                  <SelectContent position="popper">
                                    {destinations.data?.map((d) => (
                                      <SelectItem key={d.id} value={d.id}>
                                        {d.name}
                                        {d.isInternational && ` (${d.countryName})`}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              )}
                            />
                          </FormField>
                          <div className="grid content-start gap-2">
                            <Label htmlFor={`leg-${index}-nights`}>Nights</Label>
                            <div className="relative">
                              <Input
                                id={`leg-${index}-nights`}
                                type="number"
                                inputMode="numeric"
                                min={1}
                                max={tripLimits.maxNightsPerLeg}
                                className={cn('nums pr-16', noSpinners)}
                                aria-invalid={legErrors?.nights ? true : undefined}
                                aria-describedby={legErrors?.nights ? `leg-${index}-nights-message` : undefined}
                                {...form.register(`legs.${index}.nights`, { valueAsNumber: true })}
                              />
                              <span aria-hidden className="pointer-events-none absolute top-1/2 right-3.5 -translate-y-1/2 text-sm text-ink-400">
                                nights
                              </span>
                            </div>
                            <FieldMessage id={`leg-${index}-nights-message`} error={legErrors?.nights?.message} />
                          </div>
                        </div>

                        {!isLast && (
                          <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 border-t border-dashed border-ink-200 pt-4">
                            <Label htmlFor={`leg-${index}-transfer`} className="text-ink-600">
                              <TrainFrontIcon className="size-4 text-ink-400" />
                              Then travel to stop {index + 2} by
                            </Label>
                            <Controller
                              control={form.control}
                              name={`legs.${index}.transferToNext`}
                              render={({ field: f }) => (
                                <Select value={String(f.value)} onValueChange={(v) => f.onChange(Number(v))}>
                                  <SelectTrigger id={`leg-${index}-transfer`} className="w-full sm:w-48">
                                    <SelectValue />
                                  </SelectTrigger>
                                  <SelectContent position="popper">
                                    {transferModes.map((m) => (
                                      <SelectItem key={m.value} value={String(m.value)}>
                                        {m.label}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              )}
                            />
                          </div>
                        )}
                      </div>
                    </li>
                  )
                })}
              </ol>

              {/* Where the route continues: a dashed "+" node and the button that adds the next stop. */}
              <div className="grid grid-cols-[2.25rem_minmax(0,1fr)] items-center gap-x-3 sm:grid-cols-[2.5rem_minmax(0,1fr)] sm:gap-x-4">
                <span aria-hidden className="relative z-10 flex size-9 items-center justify-center rounded-full border-2 border-dashed border-forest-300 bg-card text-forest-600 sm:size-10">
                  <PlusIcon className="size-4" />
                </span>
                <Button
                  type="button"
                  variant="outline"
                  className="w-full justify-start border-dashed border-forest-300 text-forest-700"
                  disabled={fields.length >= tripLimits.maxLegs}
                  onClick={() => append(newLeg)}
                >
                  <PlusIcon />
                  Add a destination
                </Button>
              </div>
            </div>

            {(errors.legs?.root?.message ?? errors.legs?.message) && (
              <p className="text-[0.8125rem] font-medium text-destructive">{errors.legs?.root?.message ?? errors.legs?.message}</p>
            )}
          </Panel>

          <Panel icon={HotelIcon} title="Hotels and extras" subtitle="How you like to stay, and anything that helps us plan.">
            <fieldset className="grid gap-2">
              <legend className="mb-2 text-[0.8125rem] font-semibold text-ink-700">Hotels</legend>
              <Controller
                control={form.control}
                name="hotelLevel"
                render={({ field }) => (
                  <div className="grid gap-3 sm:grid-cols-3">
                    {hotelLevels.map((h) => {
                      const Icon = hotelIcons[h.value]
                      const checked = field.value === h.value
                      return (
                        <label
                          key={h.value}
                          className={cn(
                            'relative flex cursor-pointer items-center gap-3 rounded-2xl bg-card p-3.5 ring-1 transition-[box-shadow,background-color] duration-200 has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-forest-500',
                            checked ? 'bg-forest-50/70 ring-2 ring-forest-500' : 'ring-ink-200 hover:shadow-soft hover:ring-forest-300',
                          )}
                        >
                          <input
                            type="radio"
                            name={field.name}
                            value={h.value}
                            checked={checked}
                            onChange={() => field.onChange(h.value)}
                            onBlur={field.onBlur}
                            className="sr-only"
                          />
                          <span
                            className={cn(
                              'flex size-10 shrink-0 items-center justify-center rounded-xl transition-colors',
                              checked ? 'bg-forest-600 text-white' : 'bg-forest-50 text-forest-600',
                            )}
                          >
                            <Icon className="size-[18px]" />
                          </span>
                          <span className="grid min-w-0">
                            <span className="text-sm font-semibold text-ink-900">{h.label}</span>
                            <span className="text-xs text-ink-500">{h.hint}</span>
                          </span>
                          {checked && (
                            <span aria-hidden className="absolute top-2 right-2 flex size-5 animate-scale-in items-center justify-center rounded-full bg-forest-600 text-white">
                              <CheckIcon className="size-3" strokeWidth={3} />
                            </span>
                          )}
                        </label>
                      )
                    })}
                  </div>
                )}
              />
            </fieldset>

            <div className="grid content-start gap-2 sm:max-w-xs">
              <Label htmlFor="budgetPerPerson">Budget per person (optional)</Label>
              <div className="relative">
                <span aria-hidden className="pointer-events-none absolute top-1/2 left-3.5 -translate-y-1/2 text-base font-semibold text-ink-500">
                  ৳
                </span>
                <Input
                  id="budgetPerPerson"
                  type="number"
                  inputMode="numeric"
                  min={1}
                  className="nums pl-8"
                  aria-invalid={errors.budgetPerPerson ? true : undefined}
                  aria-describedby="budgetPerPerson-message"
                  {...form.register('budgetPerPerson', { setValueAs: (v: string) => (v === '' || v === null ? null : Number(v)) })}
                />
              </div>
              <FieldMessage id="budgetPerPerson-message" error={errors.budgetPerPerson?.message} hint="Helps us choose - it's not a limit." />
            </div>

            <FormField label="Notes (optional)" htmlFor="notes" error={errors.notes?.message}>
              <Textarea
                id="notes"
                maxLength={2000}
                placeholder="A sea-facing room, vegetarian meals, a day trip to Saint Martin…"
                aria-invalid={errors.notes ? true : undefined}
                {...form.register('notes')}
              />
            </FormField>
          </Panel>
        </div>

        <TripSummary
          startDate={startDate}
          stops={stops}
          people={{ adults, children, infants }}
          hotelLevel={hotelLevel}
          nameOf={destinationName}
          submitting={isSubmitting}
        />
      </form>
    </div>
  )
}

/** One white panel of the form: an icon tile, the title and a line under it (an optional extra on the right), then the fields. */
function Panel({ icon: Icon, title, subtitle, aside, children }: { icon: LucideIcon; title: string; subtitle: string; aside?: ReactNode; children: ReactNode }) {
  return (
    <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <header className="flex items-center justify-between gap-4">
        <div className="flex min-w-0 items-center gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-600">
            <Icon className="size-[18px]" />
          </span>
          <div className="grid gap-0.5">
            <h2 className="text-base font-bold text-ink-900">{title}</h2>
            <p className="text-sm text-ink-500">{subtitle}</p>
          </div>
        </div>
        {aside && <div className="shrink-0">{aside}</div>}
      </header>
      {children}
    </section>
  )
}

/**
 * "Adults  [−] 2 [+]": a traveller count as a row - the label and age band on
 * the left, the stepper on the right. The number box still takes typing; the
 * buttons only add or take one away.
 */
function CountRow({
  id,
  label,
  hint,
  value,
  lowest,
  error,
  onStep,
  ...inputProps
}: ComponentProps<'input'> & { id: string; label: string; hint: string; value: number; lowest: number; error?: string; onStep: (delta: number) => void }) {
  const current = Number.isFinite(value) ? value : 0
  return (
    <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 px-4 py-3">
      <div className="grid gap-1">
        <Label htmlFor={id}>{label}</Label>
        <span className="text-xs text-ink-500">{hint}</span>
      </div>
      <div className="flex items-center gap-2">
        <Button type="button" variant="outline" size="icon" aria-label={`One fewer (${label.toLowerCase()})`} disabled={current <= lowest} onClick={() => onStep(-1)}>
          <MinusIcon />
        </Button>
        <Input
          id={id}
          type="number"
          inputMode="numeric"
          min={lowest}
          max={tripLimits.maxPeople}
          className={cn('nums w-14 px-2 text-center font-semibold', noSpinners)}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${id}-message` : undefined}
          {...inputProps}
        />
        <Button type="button" variant="outline" size="icon" aria-label={`One more (${label.toLowerCase()})`} disabled={current >= tripLimits.maxPeople} onClick={() => onStep(1)}>
          <PlusIcon />
        </Button>
      </div>
      {error && (
        <div className="basis-full">
          <FieldMessage id={`${id}-message`} error={error} />
        </div>
      )}
    </div>
  )
}

type Stop = { destinationId: string; nights: number; from: string; to: string }

/**
 * Every stop's check-in and check-out: each starts the day the previous one
 * ends - the same sums the API does (backend: CustomTrip.Submit). A nights
 * box being typed in (empty, NaN) counts as 0. No start date yet → no dates.
 */
function stopDates(startDate: string, legs: PlanTripInput['legs']) {
  return legs.reduce<Stop[]>((stops, leg) => {
    const nights = Number.isFinite(leg.nights) && leg.nights > 0 ? leg.nights : 0
    const from = stops.length > 0 ? stops[stops.length - 1].to : startDate
    stops.push({ destinationId: leg.destinationId, nights, from, to: from ? addDays(from, nights) : '' })
    return stops
  }, [])
}

/** "2026-10-20" → "20 Oct" - short enough for a stop's card. */
function shortDate(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', timeZone: 'UTC' })
}

/** The plan so far, with every stop's dates, as a route - and the button that sends it. */
function TripSummary({
  startDate,
  stops,
  people,
  hotelLevel,
  nameOf,
  submitting,
}: {
  startDate: string
  stops: Stop[]
  people: { adults: number; children: number; infants: number }
  hotelLevel: HotelLevel
  nameOf: (id: string) => string | undefined
  submitting: boolean
}) {
  const total = stops.reduce((sum, stop) => sum + stop.nights, 0)
  const count = (n: number) => (Number.isFinite(n) ? n : 0)
  const who = [
    `${count(people.adults)} adult${count(people.adults) === 1 ? '' : 's'}`,
    count(people.children) > 0 && `${count(people.children)} child${count(people.children) === 1 ? '' : 'ren'}`,
    count(people.infants) > 0 && `${count(people.infants)} infant${count(people.infants) === 1 ? '' : 's'}`,
  ].filter(Boolean)

  return (
    <aside className="overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80 lg:sticky lg:top-24">
      {/* Dark brand header: the length of the trip at a glance. */}
      <div className="brand-surface relative isolate grid gap-1 overflow-hidden bg-gradient-to-br from-forest-700 to-forest-950 px-5 py-5 text-white sm:px-6">
        <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
        <div aria-hidden className="absolute -top-16 -right-10 -z-10 size-40 rounded-full bg-sun-500/20 blur-3xl" />
        <h2 className="text-[0.6875rem] font-semibold tracking-wider text-forest-100/70 uppercase">Your trip</h2>
        <p className="text-2xl font-bold tracking-tight">
          {total} night{total === 1 ? '' : 's'} <span className="text-base font-semibold text-forest-100/80">in all</span>
        </p>
        <p className="text-sm text-forest-100/75">
          {startDate && total > 0 ? `Back on ${formatDate(addDays(startDate, total))}` : 'Pick the first day to see dates'}
        </p>
      </div>

      <div className="grid gap-5 p-5 sm:p-6">
        {/* The route: a dot per stop on a dashed line, nights on the right. */}
        <ol className="relative grid gap-4 text-sm">
          <span aria-hidden className="absolute top-2 bottom-2 left-[7px] border-l-2 border-dashed border-ink-200" />
          {stops.map((stop, i) => {
            const name = nameOf(stop.destinationId)
            return (
              <li key={i} className="relative flex items-start gap-3">
                <span
                  aria-hidden
                  className={cn(
                    'mt-0.5 size-4 shrink-0 rounded-full border-[3px] bg-card',
                    i === 0 || i === stops.length - 1 ? 'border-forest-600' : 'border-forest-400',
                  )}
                />
                <div className="grid min-w-0 flex-1 gap-0.5">
                  <span className={cn('truncate font-semibold', name ? 'text-ink-900' : 'text-ink-400')}>
                    {i + 1}. {name ?? 'Choose a place'}
                  </span>
                  <span className="text-xs text-ink-500">{stop.from ? `${formatDate(stop.from)} → ${formatDate(stop.to)}` : 'Dates follow the first day'}</span>
                </div>
                <span className="nums shrink-0 rounded-full bg-forest-50 px-2 py-0.5 text-xs font-semibold text-forest-700">
                  {stop.nights} night{stop.nights === 1 ? '' : 's'}
                </span>
              </li>
            )
          })}
        </ol>

        <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 border-t border-dashed border-ink-200 pt-4 text-sm">
          <dt className="text-ink-500">Travellers</dt>
          <dd className="text-right font-medium text-ink-900">{who.join(', ')}</dd>
          <dt className="text-ink-500">Hotels</dt>
          <dd className="text-right font-medium text-ink-900">{hotelLevels.find((h) => h.value === hotelLevel)?.label}</dd>
        </dl>

        <Button type="submit" size="lg" className="w-full" disabled={submitting}>
          {submitting ? <Spinner /> : <SendIcon />}
          Send my request
        </Button>
        <p className="-mt-2 text-center text-xs text-ink-500">Free and without obligation - you decide once you see the price.</p>
      </div>
    </aside>
  )
}
