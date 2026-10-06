import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery } from '@tanstack/react-query'
import { ArrowDownIcon, ArrowUpIcon, MapPinIcon, PlusIcon, Trash2Icon } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { destinationsQuery } from '@/features/catalog/api/catalog.api'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { addDays, formatDate, todayInBangladesh } from '@/shared/lib/dates'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { hotelLevels, tripLimits, tripsApi, transferModes, type TransferMode } from '../api/trips.api'
import { planTripSchema, type PlanTripInput } from '../schemas/planTrip.schema'

const newLeg = { destinationId: '', nights: 2, transferToNext: 2 as TransferMode }

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
  const [startDate, legs] = useWatch({ control: form.control, name: ['startDate', 'legs'] })

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
      navigate(`/account/trips/${encodeURIComponent(trip.tripNo)}`, { replace: true, state: { justSubmitted: true } })
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  const destinationName = (id: string) => destinations.data?.find((d) => d.id === id)?.name

  return (
    <div className="mx-auto grid max-w-5xl gap-6">
      <div className="grid gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Plan my trip</h1>
        <p className="text-muted-foreground">
          Add the places you want to visit, in order. We plan the hotels and transport and send you a price - usually within 24 hours.
        </p>
      </div>

      <form onSubmit={onSubmit} noValidate className="grid items-start gap-6 lg:grid-cols-[1fr_20rem]">
        <div className="grid gap-6">
          {formError && <FormAlert kind="error">{formError}</FormAlert>}

          <section className="grid gap-4 rounded-xl border bg-card p-4">
            <h2 className="font-semibold">When and who</h2>
            <div className="grid gap-4 sm:grid-cols-2">
              <TextField
                label="First day"
                type="date"
                min={earliest}
                hint={`${formatDate(earliest)} or later`}
                error={errors.startDate?.message}
                {...form.register('startDate')}
              />
              <div className="grid gap-1.5">
                <Label htmlFor="hotelLevel">Hotels</Label>
                <Controller
                  control={form.control}
                  name="hotelLevel"
                  render={({ field }) => (
                    <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v))}>
                      <SelectTrigger id="hotelLevel">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {hotelLevels.map((h) => (
                          <SelectItem key={h.value} value={String(h.value)}>
                            {h.label} - {h.hint}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-4">
              <TextField label="Adults" type="number" min={1} error={errors.adults?.message} {...form.register('adults', { valueAsNumber: true })} />
              <TextField
                label="Children"
                type="number"
                min={0}
                hint="2-11 years"
                error={errors.children?.message}
                {...form.register('children', { valueAsNumber: true })}
              />
              <TextField
                label="Infants"
                type="number"
                min={0}
                hint="Under 2"
                error={errors.infants?.message}
                {...form.register('infants', { valueAsNumber: true })}
              />
            </div>
          </section>

          <section className="grid gap-4 rounded-xl border bg-card p-4">
            <div className="flex items-center justify-between gap-2">
              <h2 className="font-semibold">Where to</h2>
              <span className="text-sm text-muted-foreground">
                {fields.length} of {tripLimits.maxLegs}
              </span>
            </div>

            {destinations.isError && <FormAlert kind="error">The destinations couldn't be loaded - please refresh the page.</FormAlert>}

            <ol className="grid gap-3">
              {fields.map((field, index) => {
                const isLast = index === fields.length - 1
                const legErrors = errors.legs?.[index]
                return (
                  <li key={field.id} className="grid gap-3 rounded-lg border p-3">
                    <div className="flex items-center justify-between gap-2">
                      <span className="flex items-center gap-2 text-sm font-medium">
                        <MapPinIcon className="size-4 text-muted-foreground" /> Stop {index + 1}
                      </span>
                      <div className="flex gap-1">
                        <Button type="button" variant="ghost" size="icon-sm" aria-label="Move up" disabled={index === 0} onClick={() => move(index, index - 1)}>
                          <ArrowUpIcon />
                        </Button>
                        <Button type="button" variant="ghost" size="icon-sm" aria-label="Move down" disabled={isLast} onClick={() => move(index, index + 1)}>
                          <ArrowDownIcon />
                        </Button>
                        <Button type="button" variant="ghost" size="icon-sm" aria-label="Remove" disabled={fields.length === 1} onClick={() => remove(index)}>
                          <Trash2Icon />
                        </Button>
                      </div>
                    </div>

                    <div className="grid gap-3 sm:grid-cols-[1fr_7rem]">
                      <div className="grid gap-1.5">
                        <Label htmlFor={`leg-${index}-destination`}>Destination</Label>
                        <Controller
                          control={form.control}
                          name={`legs.${index}.destinationId`}
                          render={({ field: f }) => (
                            <Select value={f.value} onValueChange={f.onChange} disabled={destinations.isPending}>
                              <SelectTrigger id={`leg-${index}-destination`} aria-invalid={legErrors?.destinationId ? true : undefined}>
                                <SelectValue placeholder={destinations.isPending ? 'Loading…' : 'Choose a place'} />
                              </SelectTrigger>
                              <SelectContent>
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
                        {legErrors?.destinationId && <p className="text-sm text-destructive">{legErrors.destinationId.message}</p>}
                      </div>
                      <TextField
                        id={`leg-${index}-nights`}
                        label="Nights"
                        type="number"
                        min={1}
                        max={tripLimits.maxNightsPerLeg}
                        error={legErrors?.nights?.message}
                        {...form.register(`legs.${index}.nights`, { valueAsNumber: true })}
                      />
                    </div>

                    {!isLast && (
                      <div className="grid gap-1.5 sm:max-w-60">
                        <Label htmlFor={`leg-${index}-transfer`}>Then travel to stop {index + 2} by</Label>
                        <Controller
                          control={form.control}
                          name={`legs.${index}.transferToNext`}
                          render={({ field: f }) => (
                            <Select value={String(f.value)} onValueChange={(v) => f.onChange(Number(v))}>
                              <SelectTrigger id={`leg-${index}-transfer`}>
                                <SelectValue />
                              </SelectTrigger>
                              <SelectContent>
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
                  </li>
                )
              })}
            </ol>

            {(errors.legs?.root?.message ?? errors.legs?.message) && (
              <p className="text-sm text-destructive">{errors.legs?.root?.message ?? errors.legs?.message}</p>
            )}

            <div>
              <Button type="button" variant="outline" disabled={fields.length >= tripLimits.maxLegs} onClick={() => append(newLeg)}>
                <PlusIcon />
                Add a destination
              </Button>
            </div>
          </section>

          <section className="grid gap-4 rounded-xl border bg-card p-4">
            <h2 className="font-semibold">Anything else?</h2>
            <TextField
              label="Budget per person (৳, optional)"
              type="number"
              min={1}
              hint="Helps us choose - it's not a limit."
              error={errors.budgetPerPerson?.message}
              {...form.register('budgetPerPerson', { setValueAs: (v: string) => (v === '' || v === null ? null : Number(v)) })}
            />
            <div className="grid gap-1.5">
              <Label htmlFor="notes">Notes (optional)</Label>
              <Textarea id="notes" maxLength={2000} placeholder="A sea-facing room, vegetarian meals, a day trip to Saint Martin…" {...form.register('notes')} />
              {errors.notes && <p className="text-sm text-destructive">{errors.notes.message}</p>}
            </div>
          </section>
        </div>

        <TripSummary startDate={startDate} legs={legs ?? []} nameOf={destinationName} submitting={isSubmitting} />
      </form>
    </div>
  )
}

/**
 * Every stop's check-in and check-out: each starts the day the previous one
 * ends - the same sums the API does (backend: CustomTrip.Submit). A nights
 * box being typed in (empty, NaN) counts as 0. No start date yet → no dates.
 */
function stopDates(startDate: string, legs: PlanTripInput['legs']) {
  return legs.reduce<{ destinationId: string; nights: number; from: string; to: string }[]>((stops, leg) => {
    const nights = Number.isFinite(leg.nights) && leg.nights > 0 ? leg.nights : 0
    const from = stops.length > 0 ? stops[stops.length - 1].to : startDate
    stops.push({ destinationId: leg.destinationId, nights, from, to: from ? addDays(from, nights) : '' })
    return stops
  }, [])
}

/** The plan so far, with every stop's dates. */
function TripSummary({
  startDate,
  legs,
  nameOf,
  submitting,
}: {
  startDate: string
  legs: PlanTripInput['legs']
  nameOf: (id: string) => string | undefined
  submitting: boolean
}) {
  const stops = stopDates(startDate, legs)
  const total = stops.reduce((sum, stop) => sum + stop.nights, 0)

  return (
    <aside className="grid gap-4 rounded-xl border bg-card p-4 lg:sticky lg:top-4">
      <h2 className="font-semibold">Your trip</h2>
      <ol className="grid gap-3 text-sm">
        {stops.map((stop, i) => (
          <li key={i} className="grid gap-0.5">
            <span className="font-medium">
              {i + 1}. {nameOf(stop.destinationId) ?? 'Choose a place'} · {stop.nights} night{stop.nights === 1 ? '' : 's'}
            </span>
            <span className="text-muted-foreground">
              {stop.from ? `${formatDate(stop.from)} → ${formatDate(stop.to)}` : 'Pick the first day to see dates'}
            </span>
          </li>
        ))}
      </ol>
      <p className="border-t pt-3 text-sm">
        <strong>{total}</strong> night{total === 1 ? '' : 's'} in all
        {startDate && total > 0 && <span className="text-muted-foreground"> · back on {formatDate(addDays(startDate, total))}</span>}
      </p>
      <Button type="submit" size="lg" disabled={submitting}>
        {submitting && <Spinner />}
        Send my request
      </Button>
      <p className="text-xs text-muted-foreground">Free and without obligation - you decide once you see the price.</p>
    </aside>
  )
}
