import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDaysIcon, FileTextIcon, ListChecksIcon, LockIcon, MoonIcon, SearchIcon, WalletIcon, type LucideIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
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
    try {
      const body = toRequest(values)
      if (pkg) {
        await packagesApi.update(pkg.id, body)
        await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        notify.success('Changes saved', { description: values.title })
      } else {
        const id = await packagesApi.create(body)
        await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        // Open the new package's own page (replace: "back" goes to the list, not an empty form).
        // That page says "created" with a toast.
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

      {/* 1. What it is. Fields go row by row, so side-by-side boxes always start level. */}
      <FormSection icon={FileTextIcon} title="Basics" description="What customers see first on cards and at the top of the package page.">
        <div className="grid gap-5 md:grid-cols-2">
          <TextField label="Title" autoFocus={!pkg} error={errors.title?.message} {...form.register('title')} />
          <TextField
            label="URL name (slug)"
            placeholder="Empty = made from the title"
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
                        {d.isInternational && <span className="text-ink-400"> · {d.countryName}</span>}
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
                  <SelectContent position="popper">
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
        <FormField label="Summary" htmlFor="summary" error={errors.summary?.message} hint="One or two sentences for the package card.">
          <Textarea id="summary" rows={2} aria-invalid={errors.summary ? true : undefined} {...form.register('summary')} />
        </FormField>
        <FormField label="Description" htmlFor="description" error={errors.description?.message} hint="The full story on the package page. Optional.">
          <Textarea id="description" rows={6} aria-invalid={errors.description ? true : undefined} {...form.register('description')} />
        </FormField>
      </FormSection>

      {/* 2. How it's sold */}
      <FormSection icon={WalletIcon} title="Pricing" description="Sold on set dates, or the customer picks the start date and how many nights.">
        <Controller
          control={form.control}
          name="pricingMode"
          render={({ field }) => (
            <div role="radiogroup" aria-label="Pricing mode" className="grid gap-3 sm:grid-cols-2">
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
          <p className="flex items-center gap-2 text-sm text-ink-500">
            <LockIcon className="size-3.5 shrink-0" />
            The mode can only be changed while the package is a draft - it has already been live.
          </p>
        )}
        {errors.pricingMode && <p className="text-sm font-medium text-destructive">{errors.pricingMode.message}</p>}

        {pricingMode === 'fixed' ? (
          <div className="grid grid-cols-2 gap-5 lg:grid-cols-4">
            <TextField label="Days" inputMode="numeric" className="nums" error={errors.durationDays?.message} {...form.register('durationDays')} />
            <TextField label="Nights" inputMode="numeric" className="nums" error={errors.durationNights?.message} {...form.register('durationNights')} />
          </div>
        ) : (
          <>
            {/* Row 1: how long and how early; row 2: what it costs. */}
            <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
              <TextField label="Minimum nights" inputMode="numeric" className="nums" error={errors.minNights?.message} {...form.register('minNights')} />
              <TextField label="Maximum nights" inputMode="numeric" className="nums" error={errors.maxNights?.message} {...form.register('maxNights')} />
              <TextField
                label="Book ahead (days)"
                inputMode="numeric"
                className="nums"
                hint="Time to confirm the hotel."
                error={errors.minLeadDays?.message}
                {...form.register('minLeadDays')}
              />
              <TextField
                label="Base price (৳)"
                inputMode="decimal"
                className="nums"
                hint="Per adult, for the minimum nights."
                error={errors.basePrice?.message}
                {...form.register('basePrice')}
              />
              <TextField
                label="Extra night (৳)"
                inputMode="decimal"
                className="nums"
                hint="Per adult, each night more."
                error={errors.extraNightPrice?.message}
                {...form.register('extraNightPrice')}
              />
            </div>
            <FlexiblePricePreview control={form.control} />
          </>
        )}
      </FormSection>

      {/* 3. What's in it */}
      <FormSection icon={ListChecksIcon} title="Content" description={'Categories help customers find it; inclusions answer "what do I get?".'}>
        <FormField label="Categories" htmlFor="categoryIds" error={errors.categoryIds?.message}>
          <Controller
            control={form.control}
            name="categoryIds"
            render={({ field }) => (
              <div id="categoryIds" className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-4">
                {categories.isPending && <span className="text-sm text-ink-500">Loading categories…</span>}
                {categories.data?.length === 0 && <span className="text-sm text-ink-500">No categories yet.</span>}
                {categories.data?.map((c) => {
                  const checked = field.value.includes(c.id)
                  return (
                    // The whole chip is the click target; it turns green while ticked.
                    <label
                      key={c.id}
                      className="flex h-11 min-w-0 cursor-pointer items-center gap-2.5 rounded-xl border border-ink-200 bg-card px-3 text-sm font-medium text-ink-700 shadow-soft transition-colors duration-200 hover:border-forest-300 hover:bg-forest-50/40 has-data-[state=checked]:border-forest-500 has-data-[state=checked]:bg-forest-50 has-data-[state=checked]:text-forest-800"
                    >
                      <Checkbox
                        checked={checked}
                        onCheckedChange={(on) =>
                          field.onChange(on === true ? [...field.value, c.id] : field.value.filter((id) => id !== c.id))
                        }
                      />
                      <CategoryIcon name={c.icon} className="size-4 shrink-0 opacity-70" />
                      <span className="truncate">{c.name}</span>
                    </label>
                  )
                })}
              </div>
            )}
          />
        </FormField>

        <div className="grid gap-5 md:grid-cols-2">
          <FormField label="Included" htmlFor="inclusions" error={errors.inclusions?.message} hint="One point per line, e.g. Hotel stay.">
            <Textarea id="inclusions" rows={5} aria-invalid={errors.inclusions ? true : undefined} {...form.register('inclusions')} />
          </FormField>
          <FormField label="Not included" htmlFor="exclusions" error={errors.exclusions?.message} hint="One point per line, e.g. Air fare.">
            <Textarea id="exclusions" rows={5} aria-invalid={errors.exclusions ? true : undefined} {...form.register('exclusions')} />
          </FormField>
        </div>

        <div className="grid gap-5 md:grid-cols-[1fr_12rem]">
          <FormField label="Terms and policy" htmlFor="termsAndPolicy" error={errors.termsAndPolicy?.message} hint="Optional. Shown on the package page.">
            <Textarea id="termsAndPolicy" rows={3} aria-invalid={errors.termsAndPolicy ? true : undefined} {...form.register('termsAndPolicy')} />
          </FormField>
          <TextField label="Minimum age" inputMode="numeric" className="nums" hint="Empty = any age." error={errors.minAge?.message} {...form.register('minAge')} />
        </div>
      </FormSection>

      {/* 4. Where it shows */}
      <FormSection icon={SearchIcon} title="Display and SEO" description="Where it's promoted, and how its page shows up on Google.">
        <Controller
          control={form.control}
          name="isFeatured"
          render={({ field }) => (
            <label
              htmlFor="isFeatured"
              className="flex cursor-pointer items-start gap-3 rounded-2xl border border-ink-200 bg-card p-4 shadow-soft transition-colors duration-200 hover:border-forest-300 has-data-[state=checked]:border-forest-300 has-data-[state=checked]:bg-forest-50/60"
            >
              <Checkbox id="isFeatured" className="mt-0.5" checked={field.value} onCheckedChange={(checked) => field.onChange(checked === true)} />
              <span className="grid gap-0.5">
                <span className="text-sm font-semibold text-ink-900">Show on the home page (featured)</span>
                <span className="text-[0.8125rem] text-ink-500">Featured packages are picked out on the website's home page.</span>
              </span>
            </label>
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
      </FormSection>

      {/* Floats at the bottom of the screen while the long form scrolls, so Save is always one click away. */}
      <div className="sticky bottom-4 z-10 flex gap-2 justify-self-end rounded-2xl bg-card/90 p-2 shadow-pop ring-1 ring-ink-200/80 backdrop-blur-md">
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

/** One titled block of the form: an icon tile, the title and a line about it, then its fields. */
function FormSection({ icon: Icon, title, description, children }: { icon: LucideIcon; title: string; description: string; children: ReactNode }) {
  return (
    <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <header className="flex items-center gap-3.5">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-700 ring-1 ring-forest-100">
          <Icon className="size-[18px]" />
        </span>
        <div className="grid min-w-0 gap-0.5">
          <h2 className="text-base font-bold text-ink-900">{title}</h2>
          <p className="text-sm text-ink-500">{description}</p>
        </div>
      </header>
      {children}
    </section>
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

/** One of the two big Fixed / Flexible choices - a card with a radio dot on the right. */
function ModeOption({ selected, disabled, onSelect, icon, title, text }: ModeOptionProps) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      disabled={disabled}
      onClick={onSelect}
      className={cn(
        'flex items-start gap-3.5 rounded-2xl border p-4 text-left shadow-soft transition-[border-color,background-color,box-shadow] duration-200 outline-none focus-visible:ring-4 focus-visible:ring-forest-500/15',
        selected ? 'border-forest-500 bg-forest-50/70' : 'border-ink-200 bg-card',
        !disabled && !selected && 'hover:border-forest-300 hover:bg-forest-50/40',
        disabled && !selected && 'opacity-50',
        disabled && 'cursor-not-allowed',
      )}
    >
      <span
        className={cn(
          'flex size-10 shrink-0 items-center justify-center rounded-xl transition-colors duration-200 [&_svg]:size-5',
          selected ? 'bg-primary text-white' : 'bg-ink-100 text-ink-500',
        )}
      >
        {icon}
      </span>
      <span className="grid flex-1 gap-0.5">
        <span className="font-semibold text-ink-900">{title}</span>
        <span className="text-sm text-ink-500">{text}</span>
      </span>
      <span
        aria-hidden
        className={cn(
          'mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full border-2 transition-colors duration-200',
          selected ? 'border-forest-600 bg-forest-600' : 'border-ink-300 bg-card',
        )}
      >
        {selected && <span className="size-2 animate-scale-in rounded-full bg-white" />}
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
    <div className="flex flex-wrap items-center gap-x-3 gap-y-2 rounded-2xl bg-forest-50 px-4 py-3 ring-1 ring-forest-100">
      <span className="text-sm font-semibold text-forest-800">Customers pay per adult:</span>
      <ul className="flex flex-wrap gap-2">
        {nights.map((n) => (
          <li key={n} className="nums inline-flex items-center gap-1.5 rounded-full bg-card px-3 py-1 text-sm text-ink-600 ring-1 ring-forest-100">
            {n} {n === 1 ? 'night' : 'nights'} <span className="font-semibold text-ink-900">{formatTaka(base + (n - min) * extra)}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
