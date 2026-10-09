import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { PlusIcon, SendIcon, Trash2Icon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { quoteLineLabels, type QuoteLineCategory, type Trip } from '@/features/trips/api/trips.api'
import { AnimatedNumber } from '@/shared/components/AnimatedNumber'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { FieldMessage } from '@/shared/components/TextField'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { adminTripKeys, adminTripsApi } from '../api/adminTrips.api'

// The same limits the API checks (backend: QuoteCustomTripValidator).
const quoteSchema = z.object({
  itinerary: z.string().trim().min(1, 'Write the plan, day by day.').max(4000, 'At most 4000 characters.'),
  validDays: z.number({ error: 'Enter the days.' }).int().min(1, 'At least 1 day.').max(14, 'At most 14 days.'),
  lines: z
    .array(
      z.object({
        category: z.union([z.literal(1), z.literal(2), z.literal(3), z.literal(4), z.literal(5), z.literal(6)]),
        description: z.string().trim().min(1, 'Describe it.').max(200),
        amount: z.number({ error: 'Enter the amount.' }).positive('Above zero.'),
      }),
    )
    .min(1, 'Add at least one price line.')
    .max(30, 'At most 30 lines.'),
})
type QuoteInput = z.infer<typeof quoteSchema>

const categories = Object.entries(quoteLineLabels).map(([value, label]) => ({ value: Number(value) as QuoteLineCategory, label }))

// One price line's columns - category, description, amount, remove - shared by the
// column headings and every row, so they line up exactly. On a phone a line stacks.
const lineColumns = 'sm:grid-cols-[10rem_minmax(0,1fr)_9.5rem_2.5rem]'

/**
 * Staff price a custom trip (17-day plan, Day 15: "quote editor: itinerary
 * text, price lines (hotel, transport, meals…), total, validity"). Starts
 * from the current quote when re-quoting; sending replaces it and emails the
 * customer, and a toast confirms it went. secondaryAction sits at the start
 * of the footer (the page's "Reject trip"); it renders inside the <form>, so
 * give it type="button".
 */
export function QuoteEditor({ trip, secondaryAction }: { trip: Trip; secondaryAction?: ReactNode }) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const form = useForm<QuoteInput>({
    resolver: zodResolver(quoteSchema),
    defaultValues: trip.quote
      ? { itinerary: trip.quote.itinerary, validDays: 3, lines: trip.quote.lines.map((l) => ({ ...l })) }
      : { itinerary: '', validDays: 3, lines: [{ category: 1, description: '', amount: Number.NaN }] },
  })
  const { fields, append, remove } = useFieldArray({ control: form.control, name: 'lines' })
  const { errors, isSubmitting } = form.formState
  const lines = useWatch({ control: form.control, name: 'lines' })
  const total = (lines ?? []).reduce((sum, l) => sum + (Number.isFinite(l.amount) && l.amount > 0 ? l.amount : 0), 0)

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await adminTripsApi.quote(trip.tripNo, values)
      await queryClient.invalidateQueries({ queryKey: adminTripKeys.all })
      notify.success(trip.quote ? 'Updated quote sent' : 'Quote sent', { description: 'The customer gets an email with the price in a few seconds.' })
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-6">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <FormField label="Itinerary (the customer reads this)" htmlFor="itinerary" error={errors.itinerary?.message}>
        <Textarea
          id="itinerary"
          rows={8}
          placeholder={'Day 1: Fly Dhaka → Cox\'s Bazar, check in at …\nDay 2: …'}
          aria-invalid={errors.itinerary ? true : undefined}
          aria-describedby={errors.itinerary ? 'itinerary-message' : undefined}
          {...form.register('itinerary')}
        />
      </FormField>

      <div role="group" aria-labelledby="price-lines-label" className="grid gap-3">
        <Label id="price-lines-label">Price lines (for everyone, in ৳)</Label>

        <div aria-hidden className={`hidden gap-2 text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase sm:grid ${lineColumns}`}>
          <span>Category</span>
          <span>Description</span>
          <span className="text-right">Amount</span>
        </div>

        <ul className="grid gap-3 sm:gap-2">
          {fields.map((field, index) => {
            const lineErrors = errors.lines?.[index]
            const descriptionId = `lines-${index}-description`
            const amountId = `lines-${index}-amount`
            return (
              <li
                key={field.id}
                className={`grid animate-fade-up grid-cols-[minmax(0,1fr)_auto] items-start gap-2 rounded-2xl bg-ink-50/70 p-3 ring-1 ring-ink-200/60 ring-inset sm:bg-transparent sm:p-0 sm:ring-0 ${lineColumns}`}
              >
                <Controller
                  control={form.control}
                  name={`lines.${index}.category`}
                  render={({ field: f }) => (
                    <Select value={String(f.value)} onValueChange={(v) => f.onChange(Number(v))}>
                      <SelectTrigger aria-label={`Line ${index + 1}: category`} className="order-1 w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent position="popper">
                        {categories.map((c) => (
                          <SelectItem key={c.value} value={String(c.value)}>
                            {c.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
                <div className="order-3 col-span-2 grid gap-1.5 sm:order-2 sm:col-span-1">
                  <Input
                    id={descriptionId}
                    placeholder="e.g. Sea Pearl, 3 nights, 2 rooms"
                    aria-label={`Line ${index + 1}: description`}
                    maxLength={200}
                    aria-invalid={lineErrors?.description ? true : undefined}
                    aria-describedby={lineErrors?.description ? `${descriptionId}-message` : undefined}
                    {...form.register(`lines.${index}.description`)}
                  />
                  <FieldMessage id={`${descriptionId}-message`} error={lineErrors?.description?.message} />
                </div>
                <div className="order-4 col-span-2 grid gap-1.5 sm:order-3 sm:col-span-1">
                  {/* ৳ sits inside the box on the left; the number is right-aligned, like the column of amounts it belongs to. */}
                  <div className="relative">
                    <span aria-hidden className="pointer-events-none absolute top-1/2 left-3.5 -translate-y-1/2 text-sm font-semibold text-ink-500">
                      ৳
                    </span>
                    <Input
                      id={amountId}
                      type="number"
                      min={1}
                      step="0.01"
                      placeholder="0"
                      aria-label={`Line ${index + 1}: amount`}
                      className="nums pl-8 text-right"
                      aria-invalid={lineErrors?.amount ? true : undefined}
                      aria-describedby={lineErrors?.amount ? `${amountId}-message` : undefined}
                      {...form.register(`lines.${index}.amount`, { valueAsNumber: true })}
                    />
                  </div>
                  <FieldMessage id={`${amountId}-message`} error={lineErrors?.amount?.message} />
                </div>
                <Tooltip>
                  <TooltipTrigger asChild>
                    {/* A disabled button gets no hover, so the tooltip hangs on a wrapper. */}
                    <span className="order-2 inline-flex sm:order-4" tabIndex={fields.length === 1 ? 0 : -1}>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        aria-label={`Remove line ${index + 1}`}
                        disabled={fields.length === 1}
                        onClick={() => remove(index)}
                        className="text-ink-400 hover:bg-clay-50 hover:text-clay-600"
                      >
                        <Trash2Icon />
                      </Button>
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>{fields.length === 1 ? 'A quote needs at least one line' : 'Remove line'}</TooltipContent>
                </Tooltip>
              </li>
            )
          })}
        </ul>
        {errors.lines?.root?.message && <FieldMessage id="lines-message" error={errors.lines.root.message} />}

        {/* Add a line on the left; the live total on the right, its right edge on the amounts' right edge. */}
        <div className="flex flex-wrap items-center justify-between gap-3 sm:pr-12">
          <Button type="button" variant="outline" size="sm" onClick={() => append({ category: 2, description: '', amount: Number.NaN })}>
            <PlusIcon />
            Add line
          </Button>
          <p className="flex items-baseline gap-3" aria-live="polite">
            <span className="text-sm font-medium text-ink-500">Total</span>
            <span className="nums text-xl font-bold tracking-tight text-ink-900">
              <AnimatedNumber value={total} format={(n) => formatTaka(Math.round(n * 100) / 100)} duration={500} />
            </span>
          </p>
        </div>
      </div>

      {/* Footer: the secondary action on the left; how long the price holds, and Send, on the right. */}
      <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-3 border-t border-ink-100 pt-5">
        <div>{secondaryAction}</div>
        <div className="flex flex-wrap items-start gap-3">
          <div className="grid gap-1.5">
            <div className="flex items-center gap-2.5">
              <Label htmlFor="validDays" className="whitespace-nowrap text-ink-600">
                Valid for
              </Label>
              <Input
                id="validDays"
                type="number"
                min={1}
                max={14}
                className="nums w-20 text-center"
                aria-invalid={errors.validDays ? true : undefined}
                aria-describedby={errors.validDays ? 'validDays-message' : undefined}
                {...form.register('validDays', { valueAsNumber: true })}
              />
              <span className="text-sm text-ink-600">days</span>
            </div>
            <FieldMessage id="validDays-message" error={errors.validDays?.message} />
          </div>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? <Spinner /> : <SendIcon className="group-hover/button:translate-x-0.5" />}
            {trip.quote ? 'Send updated quote' : 'Send quote'}
          </Button>
        </div>
      </div>
    </form>
  )
}
