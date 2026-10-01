import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { ArrowDownIcon, ArrowUpIcon, PlusIcon, Trash2Icon } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { maxItineraryDays, packageKeys, packagesApi, PricingMode, type AdminPackage } from '../api/packages.api'
import {
  emptyDay,
  itinerarySchema,
  toItineraryForm,
  toItineraryRequest,
  type ItineraryInput,
} from '../schemas/itinerary.schema'

const meals = [
  { name: 'breakfast', label: 'Breakfast' },
  { name: 'lunch', label: 'Lunch' },
  { name: 'dinner', label: 'Dinner' },
] as const

/** The day-by-day plan: add, edit, re-order and remove days, then save them all at once. */
export function PackageItineraryTab({ pkg }: { pkg: AdminPackage }) {
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<{ kind: 'error' | 'success'; text: string } | null>(null)

  const form = useForm<ItineraryInput>({ resolver: zodResolver(itinerarySchema), defaultValues: toItineraryForm(pkg) })
  const { errors, isSubmitting, isDirty } = form.formState

  // useFieldArray manages the LIST of days: each row gets a stable "id"
  // (for React's key) that survives moving it up or down.
  const { fields, append, remove, swap } = useFieldArray({ control: form.control, name: 'days' })

  const target = expectedDays(pkg)

  const onSubmit = form.handleSubmit(async (values) => {
    setMessage(null)
    try {
      await packagesApi.saveItinerary(pkg.id, toItineraryRequest(values))
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      // The saved values become the new "clean" state, so "Unsaved changes" goes away.
      form.reset(values)
      setMessage({ kind: 'success', text: 'Itinerary saved.' })
    } catch (error) {
      // e.g. 422 package_must_stay_publishable: a published fixed package needs exactly DurationDays days.
      setMessage({ kind: 'error', text: toAppError(error).message })
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate>
      <Card>
        <CardHeader>
          <CardTitle>Itinerary</CardTitle>
          <CardDescription>One card per day, in order - the first is Day 1.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          {/* The publish rule, visible while editing - no surprise at publish time. */}
          <p className={cn('text-sm', target.ok(fields.length) ? 'text-muted-foreground' : 'text-amber-700 dark:text-amber-400')}>
            {fields.length} {fields.length === 1 ? 'day' : 'days'} · {target.text}
          </p>

          {fields.length === 0 && (
            <p className="rounded-md border border-dashed py-8 text-center text-sm text-muted-foreground">
              No days yet - add Day 1.
            </p>
          )}

          <ol className="grid gap-3">
            {fields.map((field, index) => {
              const dayErrors = errors.days?.[index]
              return (
                <li key={field.id} className="grid gap-3 rounded-lg border p-3">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">Day {index + 1}</span>
                    <div className="flex">
                      <Button type="button" variant="ghost" size="icon-sm" aria-label={`Move day ${index + 1} up`} disabled={index === 0} onClick={() => swap(index, index - 1)}>
                        <ArrowUpIcon />
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Move day ${index + 1} down`}
                        disabled={index === fields.length - 1}
                        onClick={() => swap(index, index + 1)}
                      >
                        <ArrowDownIcon />
                      </Button>
                      <Button type="button" variant="ghost" size="icon-sm" aria-label={`Remove day ${index + 1}`} onClick={() => remove(index)}>
                        <Trash2Icon />
                      </Button>
                    </div>
                  </div>

                  <div className="grid gap-3 md:grid-cols-2">
                    <TextField
                      id={`days.${index}.title`}
                      label="Title"
                      placeholder="e.g. Arrive in Cox's Bazar"
                      error={dayErrors?.title?.message}
                      {...form.register(`days.${index}.title`)}
                    />
                    <TextField
                      id={`days.${index}.accommodation`}
                      label="Night at (hotel)"
                      placeholder="Empty = no hotel this night"
                      error={dayErrors?.accommodation?.message}
                      {...form.register(`days.${index}.accommodation`)}
                    />
                  </div>

                  <FormField label="What happens" htmlFor={`days.${index}.description`} error={dayErrors?.description?.message}>
                    <Textarea
                      id={`days.${index}.description`}
                      rows={3}
                      aria-invalid={dayErrors?.description ? true : undefined}
                      {...form.register(`days.${index}.description`)}
                    />
                  </FormField>

                  <div className="flex flex-wrap items-center gap-4">
                    <span className="text-sm text-muted-foreground">Meals included:</span>
                    {meals.map((meal) => (
                      <Controller
                        key={meal.name}
                        control={form.control}
                        name={`days.${index}.${meal.name}`}
                        render={({ field: tick }) => (
                          <div className="flex items-center gap-1.5">
                            <Checkbox
                              id={`days.${index}.${meal.name}`}
                              checked={tick.value}
                              onCheckedChange={(checked) => tick.onChange(checked === true)}
                            />
                            <Label htmlFor={`days.${index}.${meal.name}`}>{meal.label}</Label>
                          </div>
                        )}
                      />
                    ))}
                  </div>
                </li>
              )
            })}
          </ol>

          {errors.days?.message && <p className="text-sm text-destructive">{errors.days.message}</p>}

          <Button
            type="button"
            variant="outline"
            className="w-fit"
            disabled={fields.length >= maxItineraryDays}
            onClick={() => append(emptyDay)}
          >
            <PlusIcon />
            Add day {fields.length + 1}
          </Button>

          {message && <FormAlert kind={message.kind}>{message.text}</FormAlert>}

          <div className="flex items-center justify-end gap-2">
            {isDirty && <span className="text-sm text-muted-foreground">Unsaved changes</span>}
            <Button type="button" variant="outline" disabled={!isDirty || isSubmitting} onClick={() => form.reset()}>
              Undo changes
            </Button>
            <Button type="submit" disabled={!isDirty || isSubmitting}>
              {isSubmitting && <Spinner />}
              Save itinerary
            </Button>
          </div>
        </CardContent>
      </Card>
    </form>
  )
}

/**
 * How many days the publish rule wants (same as TourPackage.GetPublishProblems):
 * fixed = exactly DurationDays; flexible = 1 up to the longest stay (MaxNights + 1).
 */
function expectedDays(pkg: AdminPackage): { text: string; ok: (count: number) => boolean } {
  if (pkg.pricingMode === PricingMode.FlexibleStay && pkg.maxNights !== null) {
    const longest = pkg.maxNights + 1
    return { text: `1 to ${longest} days (the longest stay)`, ok: (n) => n >= 1 && n <= longest }
  }
  return { text: `the package lasts ${pkg.durationDays} days`, ok: (n) => n === pkg.durationDays }
}
