import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDaysIcon, LockIcon, MoonIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { slugify } from '@/shared/lib/slug'
import { CategoryIcon } from '../../categories/components/CategoryIcon'
import {
  categoryOptionsQuery,
  destinationOptionsQuery,
  formatTaka,
  packageKeys,
  packagesApi,
  PackageStatus,
  tourTypeLabels,
  TourType,
  type AdminPackage,
} from '../api/packages.api'
import { packageSchema, toFormValues, toRequest, type PackageInput } from '../schemas/package.schema'

/** The package's form fields - used to add a new package (no pkg) and to edit one. */
export function PackageForm({ pkg }: { pkg?: AdminPackage }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const destinations = useQuery(destinationOptionsQuery)
  const categories = useQuery(categoryOptionsQuery)
  const [formError, setFormError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const form = useForm<PackageInput>({ resolver: zodResolver(packageSchema), defaultValues: toFormValues(pkg) })
  const { errors, isSubmitting } = form.formState

  // useWatch (not form.watch): re-renders only this component, only when these change.
  const [title, slug, pricingMode] = useWatch({ control: form.control, name: ['title', 'slug', 'pricingMode'] })
  const finalSlug = slugify(slug || title)

  // Same rule as the API: once a package has been live, customers may have
  // booked it the old way, so the mode can only change while it's a draft.
  const modeLocked = pkg !== undefined && pkg.status !== PackageStatus.Draft

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setSaved(false)
    try {
      const body = toRequest(values)
      if (pkg) {
        await packagesApi.update(pkg.id, body)
        await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        setSaved(true)
      } else {
        const id = await packagesApi.create(body)
        await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        // Open the new package's own page (replace: "back" goes to the list, not an empty form).
        navigate(`/admin/packages/${id}`, { replace: true, state: { created: true } })
      }
    } catch (error) {
      setFormError(
        applyServerErrors(form, error, {
          package_slug_taken: 'slug',
          destination_not_found: 'destinationId',
          category_not_found: 'categoryIds',
          package_pricing_mode_locked: 'pricingMode',
        }),
      )
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-6">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      {/* 1. What it is */}
      <Card>
        <CardHeader>
          <CardTitle>Basics</CardTitle>
          <CardDescription>What customers see first on cards and at the top of the package page.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <div className="grid content-start gap-4">
            <TextField label="Title" autoFocus={!pkg} error={errors.title?.message} {...form.register('title')} />
            <TextField
              label="URL name (slug)"
              placeholder="Leave empty to make it from the title"
              error={errors.slug?.message}
              hint={finalSlug ? `Address: /packages/${finalSlug}` : undefined}
              {...form.register('slug')}
            />
            <FormField label="Destination" htmlFor="destinationId" error={errors.destinationId?.message}>
              <Controller
                control={form.control}
                name="destinationId"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange} disabled={destinations.isPending}>
                    <SelectTrigger id="destinationId" className="w-full" aria-invalid={errors.destinationId ? true : undefined}>
                      <SelectValue placeholder={destinations.isPending ? 'Loading destinations…' : 'Choose a destination'} />
                    </SelectTrigger>
                    <SelectContent position="popper" className="max-h-72">
                      {destinations.data?.map((d) => (
                        <SelectItem key={d.id} value={d.id}>
                          {d.name}
                          {d.isInternational && <span className="text-muted-foreground"> · {d.countryName}</span>}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
            <FormField label="Tour type" htmlFor="tourType">
              <Controller
                control={form.control}
                name="tourType"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="tourType" className="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {Object.values(TourType).map((type) => (
                        <SelectItem key={type} value={String(type)}>
                          {tourTypeLabels[type]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          </div>
          <div className="grid content-start gap-4">
            <FormField label="Summary" htmlFor="summary" error={errors.summary?.message} hint="One or two sentences for the package card.">
              <Textarea id="summary" rows={3} aria-invalid={errors.summary ? true : undefined} {...form.register('summary')} />
            </FormField>
            <FormField label="Description" htmlFor="description" error={errors.description?.message} hint="The full story on the package page. Optional.">
              <Textarea id="description" rows={7} aria-invalid={errors.description ? true : undefined} {...form.register('description')} />
            </FormField>
          </div>
        </CardContent>
      </Card>

      {/* 2. How it's sold */}
      <Card>
        <CardHeader>
          <CardTitle>Pricing</CardTitle>
          <CardDescription>Sold on set dates, or the customer picks the start date and how many nights.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <Controller
            control={form.control}
            name="pricingMode"
            render={({ field }) => (
              <div role="radiogroup" aria-label="Pricing mode" className="grid gap-2 sm:grid-cols-2">
                <ModeOption
                  selected={field.value === 'fixed'}
                  disabled={modeLocked}
                  onSelect={() => field.onChange('fixed')}
                  icon={<CalendarDaysIcon />}
                  title="Fixed departures"
                  text="Set dates, each with its own price and seats. Dates are added after saving."
                />
                <ModeOption
                  selected={field.value === 'flexible'}
                  disabled={modeLocked}
                  onSelect={() => field.onChange('flexible')}
                  icon={<MoonIcon />}
                  title="Flexible stay"
                  text="Any start date; the price grows with the number of nights."
                />
              </div>
            )}
          />
          {modeLocked && (
            <p className="flex items-center gap-1.5 text-sm text-muted-foreground">
              <LockIcon className="size-3.5" />
              The mode can only be changed while the package is a draft - it has already been live.
            </p>
          )}
          {errors.pricingMode && <p className="text-sm text-destructive">{errors.pricingMode.message}</p>}

          {pricingMode === 'fixed' ? (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <TextField label="Days" inputMode="numeric" error={errors.durationDays?.message} {...form.register('durationDays')} />
              <TextField label="Nights" inputMode="numeric" error={errors.durationNights?.message} {...form.register('durationNights')} />
            </div>
          ) : (
            <>
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
                <TextField label="Minimum nights" inputMode="numeric" error={errors.minNights?.message} {...form.register('minNights')} />
                <TextField label="Maximum nights" inputMode="numeric" error={errors.maxNights?.message} {...form.register('maxNights')} />
                <TextField
                  label="Base price (৳)"
                  inputMode="decimal"
                  hint="Per adult, for the minimum nights."
                  error={errors.basePrice?.message}
                  {...form.register('basePrice')}
                />
                <TextField
                  label="Extra night (৳)"
                  inputMode="decimal"
                  hint="Per adult, each night more."
                  error={errors.extraNightPrice?.message}
                  {...form.register('extraNightPrice')}
                />
                <TextField
                  label="Book ahead (days)"
                  inputMode="numeric"
                  hint="Time to confirm the hotel."
                  error={errors.minLeadDays?.message}
                  {...form.register('minLeadDays')}
                />
              </div>
              <FlexiblePricePreview control={form.control} />
            </>
          )}
        </CardContent>
      </Card>

      {/* 3. What's in it */}
      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
          <CardDescription>Categories help customers find it; inclusions answer "what do I get?".</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <FormField label="Categories" htmlFor="categoryIds" error={errors.categoryIds?.message}>
            <Controller
              control={form.control}
              name="categoryIds"
              render={({ field }) => (
                <div id="categoryIds" className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
                  {categories.isPending && <span className="text-sm text-muted-foreground">Loading categories…</span>}
                  {categories.data?.length === 0 && <span className="text-sm text-muted-foreground">No categories yet.</span>}
                  {categories.data?.map((c) => {
                    const checked = field.value.includes(c.id)
                    return (
                      <label key={c.id} className="flex items-center gap-2 rounded-md border px-2 py-1.5 text-sm has-[:checked]:border-primary">
                        <Checkbox
                          checked={checked}
                          onCheckedChange={(on) =>
                            field.onChange(on === true ? [...field.value, c.id] : field.value.filter((id) => id !== c.id))
                          }
                        />
                        <CategoryIcon name={c.icon} className="size-4 text-muted-foreground" />
                        {c.name}
                      </label>
                    )
                  })}
                </div>
              )}
            />
          </FormField>

          <div className="grid gap-4 md:grid-cols-2">
            <FormField label="Included" htmlFor="inclusions" error={errors.inclusions?.message} hint="One point per line, e.g. Hotel stay.">
              <Textarea id="inclusions" rows={5} aria-invalid={errors.inclusions ? true : undefined} {...form.register('inclusions')} />
            </FormField>
            <FormField label="Not included" htmlFor="exclusions" error={errors.exclusions?.message} hint="One point per line, e.g. Air fare.">
              <Textarea id="exclusions" rows={5} aria-invalid={errors.exclusions ? true : undefined} {...form.register('exclusions')} />
            </FormField>
          </div>

          <div className="grid gap-4 md:grid-cols-[1fr_12rem]">
            <FormField label="Terms and policy" htmlFor="termsAndPolicy" error={errors.termsAndPolicy?.message} hint="Optional. Shown on the package page.">
              <Textarea id="termsAndPolicy" rows={3} aria-invalid={errors.termsAndPolicy ? true : undefined} {...form.register('termsAndPolicy')} />
            </FormField>
            <TextField label="Minimum age" inputMode="numeric" hint="Empty = any age." error={errors.minAge?.message} {...form.register('minAge')} />
          </div>
        </CardContent>
      </Card>

      {/* 4. Where it shows */}
      <Card>
        <CardHeader>
          <CardTitle>Display and SEO</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 md:grid-cols-2">
          <Controller
            control={form.control}
            name="isFeatured"
            render={({ field }) => (
              <div className="flex items-center gap-2 md:col-span-2">
                <Checkbox id="isFeatured" checked={field.value} onCheckedChange={(checked) => field.onChange(checked === true)} />
                <Label htmlFor="isFeatured">Show on the home page (featured)</Label>
              </div>
            )}
          />
          <TextField
            label="SEO title"
            placeholder={title ? `${title} | Ghuri` : undefined}
            error={errors.seoTitle?.message}
            hint="The blue link text on Google. Empty = the title."
            {...form.register('seoTitle')}
          />
          <FormField label="SEO description" htmlFor="seoDescription" error={errors.seoDescription?.message} hint="The grey text under the link on Google.">
            <Textarea id="seoDescription" rows={2} aria-invalid={errors.seoDescription ? true : undefined} {...form.register('seoDescription')} />
          </FormField>
        </CardContent>
      </Card>

      {saved && <FormAlert kind="success">Changes saved.</FormAlert>}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={() => navigate('/admin/packages')} disabled={isSubmitting}>
          {pkg ? 'Back to list' : 'Cancel'}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          {pkg ? 'Save changes' : 'Add package'}
        </Button>
      </div>
    </form>
  )
}

type ModeOptionProps = {
  selected: boolean
  disabled: boolean
  onSelect: () => void
  icon: ReactNode
  title: string
  text: string
}

/** One of the two big Fixed / Flexible choices. */
function ModeOption({ selected, disabled, onSelect, icon, title, text }: ModeOptionProps) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      disabled={disabled}
      onClick={onSelect}
      className={cn(
        'flex gap-3 rounded-lg border p-3 text-left transition-colors [&_svg]:size-5 [&_svg]:shrink-0',
        selected ? 'border-primary bg-primary/5' : 'hover:bg-muted',
        disabled && !selected && 'opacity-50',
        disabled && 'cursor-not-allowed',
      )}
    >
      <span className={selected ? 'text-primary' : 'text-muted-foreground'}>{icon}</span>
      <span className="grid gap-0.5">
        <span className="font-medium">{title}</span>
        <span className="text-sm text-muted-foreground">{text}</span>
      </span>
    </button>
  )
}

/**
 * Shows what customers will pay, so the admin can check the numbers:
 * base price covers the minimum nights, each extra night adds its price.
 * The Day 6 PriceCalculator on the API uses the same formula.
 */
function FlexiblePricePreview({ control }: { control: ReturnType<typeof useForm<PackageInput>>['control'] }) {
  const [minText, maxText, baseText, extraText] = useWatch({
    control,
    name: ['minNights', 'maxNights', 'basePrice', 'extraNightPrice'],
  })
  const [min, max, base, extra] = [Number(minText), Number(maxText), Number(baseText), Number(extraText)]
  const ready =
    [minText, maxText, baseText, extraText].every((t) => t !== '') &&
    Number.isInteger(min) && Number.isInteger(max) && min >= 1 && max >= min && base > 0 && extra >= 0
  if (!ready) return null

  // Shortest, one more, and longest stay - enough to see the pattern.
  const nights = [...new Set([min, min + 1, max])].filter((n) => n <= max)
  return (
    <p className="rounded-md bg-muted px-3 py-2 text-sm">
      <span className="font-medium">Customers pay per adult: </span>
      {nights.map((n, i) => (
        <span key={n}>
          {i > 0 && ' · '}
          {n} nights {formatTaka(base + (n - min) * extra)}
        </span>
      ))}
    </p>
  )
}
