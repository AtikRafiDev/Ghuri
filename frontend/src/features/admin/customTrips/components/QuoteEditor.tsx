import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { PlusIcon, Trash2Icon } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { quoteLineLabels, type QuoteLineCategory, type Trip } from '@/features/trips/api/trips.api'
import { FormAlert } from '@/shared/components/FormAlert'
import { formatTaka } from '@/shared/lib/format'
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

/**
 * Staff price a custom trip (17-day plan, Day 15: "quote editor: itinerary
 * text, price lines (hotel, transport, meals…), total, validity"). Starts
 * from the current quote when re-quoting; sending replaces it and emails the customer.
 */
export function QuoteEditor({ trip, onSent }: { trip: Trip; onSent: () => void }) {
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
      onSent()
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-4">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <div className="grid gap-1.5">
        <Label htmlFor="itinerary">Itinerary (the customer reads this)</Label>
        <Textarea
          id="itinerary"
          rows={8}
          placeholder={'Day 1: Fly Dhaka → Cox\'s Bazar, check in at …\nDay 2: …'}
          aria-invalid={errors.itinerary ? true : undefined}
          {...form.register('itinerary')}
        />
        {errors.itinerary && <p className="text-sm text-destructive">{errors.itinerary.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Price lines (for everyone, in ৳)</Label>
        {fields.map((field, index) => {
          const lineErrors = errors.lines?.[index]
          return (
            <div key={field.id} className="grid gap-2 sm:grid-cols-[9rem_1fr_9rem_auto] sm:items-start">
              <Controller
                control={form.control}
                name={`lines.${index}.category`}
                render={({ field: f }) => (
                  <Select value={String(f.value)} onValueChange={(v) => f.onChange(Number(v))}>
                    <SelectTrigger aria-label="Category">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {categories.map((c) => (
                        <SelectItem key={c.value} value={String(c.value)}>
                          {c.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              <div className="grid gap-1">
                <Input placeholder="e.g. Sea Pearl, 3 nights, 2 rooms" aria-label="Description" maxLength={200} {...form.register(`lines.${index}.description`)} />
                {lineErrors?.description && <p className="text-sm text-destructive">{lineErrors.description.message}</p>}
              </div>
              <div className="grid gap-1">
                <Input type="number" min={1} step="0.01" placeholder="Amount" aria-label="Amount" {...form.register(`lines.${index}.amount`, { valueAsNumber: true })} />
                {lineErrors?.amount && <p className="text-sm text-destructive">{lineErrors.amount.message}</p>}
              </div>
              <Button type="button" variant="ghost" size="icon" aria-label="Remove line" disabled={fields.length === 1} onClick={() => remove(index)}>
                <Trash2Icon />
              </Button>
            </div>
          )
        })}
        {errors.lines?.root?.message && <p className="text-sm text-destructive">{errors.lines.root.message}</p>}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <Button type="button" variant="outline" size="sm" onClick={() => append({ category: 2, description: '', amount: Number.NaN })}>
            <PlusIcon />
            Add line
          </Button>
          <span className="text-sm">
            Total: <strong className="text-lg tabular-nums">{formatTaka(total)}</strong>
          </span>
        </div>
      </div>

      <div className="flex flex-wrap items-end justify-between gap-4 border-t pt-4">
        <div className="grid w-40 gap-1.5">
          <Label htmlFor="validDays">Valid for (days)</Label>
          <Input id="validDays" type="number" min={1} max={14} {...form.register('validDays', { valueAsNumber: true })} />
          {errors.validDays && <p className="text-sm text-destructive">{errors.validDays.message}</p>}
        </div>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          {trip.quote ? 'Send updated quote' : 'Send quote'}
        </Button>
      </div>
    </form>
  )
}
