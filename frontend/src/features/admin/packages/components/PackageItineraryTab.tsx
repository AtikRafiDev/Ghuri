import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { ArrowDownIcon, ArrowUpIcon, CircleAlertIcon, CircleCheckIcon, ListOrderedIcon, PlusIcon, Trash2Icon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Controller, useFieldArray, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
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
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<ItineraryInput>({ resolver: zodResolver(itinerarySchema), defaultValues: toItineraryForm(pkg) })
  const { errors, isSubmitting, isDirty } = form.formState

  // useFieldArray manages the LIST of days: each row gets a stable "id"
  // (for React's key) that survives moving it up or down.
  const { fields, append, remove, swap } = useFieldArray({ control: form.control, name: 'days' })

  const target = expectedDays(pkg)
  const onTarget = target.ok(fields.length)

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await packagesApi.saveItinerary(pkg.id, toItineraryRequest(values))
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      // The saved values become the new "clean" state, so "Unsaved changes" goes away.
      form.reset(values)
      notify.success('Itinerary saved', { description: `${values.days.length} ${values.days.length === 1 ? 'day' : 'days'}` })
    } catch (error) {
      // e.g. 422 package_must_stay_publishable: a published fixed package needs exactly DurationDays days.
      setFormError(toAppError(error).message)
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-6">
      <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
        <header className="flex flex-wrap items-center justify-between gap-4">
          <div className="grid min-w-0 gap-0.5">
            <h2 className="text-base font-bold text-ink-900">Itinerary</h2>
            <p className="text-sm text-ink-500">One card per day, in order - the first is Day 1.</p>
          </div>
          {/* The publish rule, visible while editing - no surprise at publish time. */}
          <span
            className={cn(
              'inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold ring-1 ring-inset',
              onTarget ? 'bg-forest-50 text-forest-700 ring-forest-100' : 'bg-sun-50 text-sun-700 ring-sun-100',
            )}
          >
            {onTarget ? <CircleCheckIcon className="size-3.5" /> : <CircleAlertIcon className="size-3.5" />}
            <span className="nums">
              {fields.length} {fields.length === 1 ? 'day' : 'days'} · {target.text}
            </span>
          </span>
        </header>

        {fields.length === 0 ? (
          <EmptyState icon={ListOrderedIcon} title="No days yet" text="Add Day 1 to start the plan." className="py-10" />
        ) : (
          // A numbered timeline: a disc per day, joined by a line down to the next.
          <ol className="grid">
            {fields.map((field, index) => {
              const dayErrors = errors.days?.[index]
              return (
                <li key={field.id} className="relative grid grid-cols-[2rem_1fr] gap-x-3 pb-5 sm:grid-cols-[2.5rem_1fr] sm:gap-x-4">
                  <span aria-hidden className="absolute top-8 bottom-0 left-4 w-px -translate-x-1/2 bg-forest-200 sm:top-10 sm:left-5" />
                  <span
                    aria-hidden
                    className="nums relative flex size-8 items-center justify-center rounded-full bg-primary text-sm font-bold text-white shadow-soft ring-4 ring-card sm:size-10"
                  >
                    {index + 1}
                  </span>

                  <div className="grid min-w-0 gap-4 rounded-2xl border border-ink-200 bg-card p-4 shadow-soft transition-colors duration-200 focus-within:border-forest-300 hover:border-forest-200 sm:p-5">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Day {index + 1}</span>
                      <div className="-my-1 -mr-1 flex">
                        <DayAction label={`Move day ${index + 1} up`} tip="Move up" disabled={index === 0} onClick={() => swap(index, index - 1)}>
                          <ArrowUpIcon />
                        </DayAction>
                        <DayAction
                          label={`Move day ${index + 1} down`}
                          tip="Move down"
                          disabled={index === fields.length - 1}
                          onClick={() => swap(index, index + 1)}
                        >
                          <ArrowDownIcon />
                        </DayAction>
                        <DayAction label={`Remove day ${index + 1}`} tip="Remove" danger onClick={() => remove(index)}>
                          <Trash2Icon />
                        </DayAction>
                      </div>
                    </div>

                    <div className="grid gap-5 md:grid-cols-2">
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

                    <div className="flex flex-wrap items-center gap-2">
                      <span className="mr-1 text-[0.8125rem] font-semibold text-ink-700">Meals included</span>
                      {meals.map((meal) => (
                        <Controller
                          key={meal.name}
                          control={form.control}
                          name={`days.${index}.${meal.name}`}
                          render={({ field: tick }) => (
                            // A chip: the whole pill toggles the meal, and turns green while ticked.
                            <label className="flex h-9 cursor-pointer items-center gap-2 rounded-full border border-ink-200 bg-card pr-3.5 pl-2.5 text-sm font-medium text-ink-600 transition-colors duration-200 hover:border-forest-300 has-data-[state=checked]:border-forest-300 has-data-[state=checked]:bg-forest-50 has-data-[state=checked]:text-forest-800">
                              <Checkbox
                                id={`days.${index}.${meal.name}`}
                                checked={tick.value}
                                onCheckedChange={(checked) => tick.onChange(checked === true)}
                              />
                              {meal.label}
                            </label>
                          )}
                        />
                      ))}
                    </div>
                  </div>
                </li>
              )
            })}
          </ol>
        )}

        {/* The end of the timeline: a dashed disc and the button that adds the next day. */}
        <div className={cn('grid items-center gap-x-3 sm:gap-x-4', fields.length > 0 && 'grid-cols-[2rem_1fr] sm:grid-cols-[2.5rem_1fr]')}>
          {fields.length > 0 && (
            <span aria-hidden className="flex size-8 items-center justify-center rounded-full border-2 border-dashed border-forest-300 text-forest-600 sm:size-10">
              <PlusIcon className="size-4" />
            </span>
          )}
          <Button type="button" variant="outline" className="w-fit" disabled={fields.length >= maxItineraryDays} onClick={() => append(emptyDay)}>
            <PlusIcon />
            Add day {fields.length + 1}
          </Button>
        </div>

        {errors.days?.message && <p className="text-sm font-medium text-destructive">{errors.days.message}</p>}
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
      </section>

      {/* Floats at the bottom of the screen while the days scroll, so Save is always one click away. */}
      <div className="sticky bottom-4 z-10 flex flex-wrap items-center justify-end gap-2 justify-self-end rounded-2xl bg-card/90 p-2 shadow-pop ring-1 ring-ink-200/80 backdrop-blur-md">
        {isDirty && (
          <span className="flex items-center gap-2 px-2 text-sm font-medium text-sun-700">
            <span aria-hidden className="size-2 rounded-full bg-sun-500" />
            Unsaved changes
          </span>
        )}
        <Button type="button" variant="outline" disabled={!isDirty || isSubmitting} onClick={() => form.reset()}>
          Undo changes
        </Button>
        <Button type="submit" disabled={!isDirty || isSubmitting}>
          {isSubmitting && <Spinner />}
          Save itinerary
        </Button>
      </div>
    </form>
  )
}

/** A small icon button on a day card, with a tooltip; "danger" turns it terracotta on hover. */
function DayAction({ label, tip, disabled, danger, onClick, children }: { label: string; tip: string; disabled?: boolean; danger?: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={label}
          disabled={disabled}
          onClick={onClick}
          className={cn('text-ink-400', danger ? 'hover:bg-clay-50 hover:text-clay-600' : 'hover:bg-forest-50 hover:text-forest-700')}
        >
          {children}
        </Button>
      </TooltipTrigger>
      <TooltipContent>{tip}</TooltipContent>
    </Tooltip>
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
