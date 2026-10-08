import { zodResolver } from '@hookform/resolvers/zod'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { slugify } from '@/shared/lib/slug'
import { ImageGalleryUpload } from '../../components/ImageGalleryUpload'
import {
  countriesQueryOptions,
  destinationKeys,
  destinationsApi,
  maxDestinationImages,
  type AdminDestination,
  type DestinationRequest,
} from '../api/destinations.api'
import { destinationSchema, type DestinationInput } from '../schemas/destination.schema'

type DestinationFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Present = edit this one; absent = add a new one. */
  destination?: AdminDestination
}

/** Add / edit a destination in a pop-up, without leaving the table. */
export function DestinationFormDialog({ open, onOpenChange, destination }: DestinationFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>{destination ? `Edit ${destination.name}` : 'Add destination'}</DialogTitle>
          <DialogDescription>A place tours go to, like Cox's Bazar or Bali.</DialogDescription>
        </DialogHeader>
        {/* Inside DialogContent = created fresh every time the dialog opens,
            so the fields always start from the right destination. */}
        <DestinationForm destination={destination} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

function DestinationForm({ destination, onDone }: { destination?: AdminDestination; onDone: () => void }) {
  const queryClient = useQueryClient()
  const countries = useQuery(countriesQueryOptions)
  const [formError, setFormError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)

  const form = useForm<DestinationInput>({
    resolver: zodResolver(destinationSchema),
    defaultValues: toFormValues(destination),
  })
  const { errors, isSubmitting } = form.formState

  // useWatch (not form.watch): re-renders only this component, only when these change.
  const [name, slug] = useWatch({ control: form.control, name: ['name', 'slug'] })
  const finalSlug = slugify(slug || name)

  // Most destinations are in Bangladesh - preselect it for a NEW one.
  const bangladesh = countries.data?.find((c) => c.isoCode === 'BD')
  useEffect(() => {
    if (!destination && bangladesh && !form.getValues('countryId')) {
      form.setValue('countryId', String(bangladesh.id))
    }
  }, [destination, bangladesh, form])

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const body = toRequest(values)
      if (destination) await destinationsApi.update(destination.id, body)
      else await destinationsApi.create(body)
      // Every cached page/filter of the table is now out of date.
      await queryClient.invalidateQueries({ queryKey: destinationKeys.all })
      onDone()
      // The dialog closes, so a toast is the "it worked".
      notify.success(destination ? 'Destination saved' : 'Destination added', { description: values.name })
    } catch (error) {
      setFormError(
        applyServerErrors(form, error, {
          destination_slug_taken: 'slug',
          country_not_found: 'countryId',
          image_not_found: 'images',
        }),
      )
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      {/* Row by row (not two separate columns), so fields side by side
          always start at the same height. */}
      <div className="grid gap-5 sm:grid-cols-2">
        <TextField label="Name" autoFocus error={errors.name?.message} {...form.register('name')} />

        <FormField label="Country" htmlFor="countryId" error={errors.countryId?.message}>
          <Controller
            control={form.control}
            name="countryId"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange} disabled={countries.isPending}>
                <SelectTrigger id="countryId" className="w-full" aria-invalid={errors.countryId ? true : undefined}>
                  <SelectValue placeholder={countries.isPending ? 'Loading countries…' : 'Choose a country'} />
                </SelectTrigger>
                <SelectContent position="popper" className="max-h-72">
                  {bangladesh && <SelectItem value={String(bangladesh.id)}>{bangladesh.name}</SelectItem>}
                  <SelectSeparator />
                  {countries.data
                    ?.filter((c) => c.id !== bangladesh?.id)
                    .map((c) => (
                      <SelectItem key={c.id} value={String(c.id)}>
                        {c.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            )}
          />
        </FormField>

        <TextField
          label="URL name (slug)"
          placeholder="Empty = made from the name"
          error={errors.slug?.message}
          hint={finalSlug ? `Address: /destinations/${finalSlug}` : undefined}
          {...form.register('slug')}
        />

        <TextField
          label="Sort order"
          type="number"
          min={0}
          placeholder="Empty = at the end"
          hint="Lower numbers come first. Each number is used once: pick a taken one and that destination moves down a place."
          error={errors.sortOrder?.message}
          // An empty box is null ("put it at the end"), not 0 or NaN.
          {...form.register('sortOrder', { setValueAs: (v: string | number | null) => (v === '' || v === null ? null : Number(v)) })}
        />
      </div>

      <FormField label="Summary" htmlFor="summary" error={errors.summary?.message} hint="One or two sentences for the destination card.">
        <Textarea id="summary" rows={3} aria-invalid={errors.summary ? true : undefined} {...form.register('summary')} />
      </FormField>

      <Controller
        control={form.control}
        name="isFeatured"
        render={({ field }) => (
          // The whole card is the click target; it turns green while ticked.
          <label
            htmlFor="isFeatured"
            className="flex cursor-pointer items-start gap-3 rounded-2xl border border-ink-200 bg-card p-4 shadow-soft transition-colors duration-200 hover:border-forest-300 has-data-[state=checked]:border-forest-300 has-data-[state=checked]:bg-forest-50/60"
          >
            <Checkbox id="isFeatured" className="mt-0.5" checked={field.value} onCheckedChange={(checked) => field.onChange(checked === true)} />
            <span className="grid gap-0.5">
              <span className="text-sm font-semibold text-ink-900">Show on home page</span>
              <span className="text-[0.8125rem] text-ink-500">Feature it among the destinations on the website's home page.</span>
            </span>
          </label>
        )}
      />

      {/* Full width: room for a row of thumbnails */}
      <FormField label="Photos" htmlFor="images" error={errors.images?.message}>
        <Controller
          control={form.control}
          name="images"
          render={({ field }) => (
            <ImageGalleryUpload
              id="images"
              value={field.value}
              onChange={field.onChange}
              max={maxDestinationImages}
              onBusyChange={setUploading}
              invalid={!!errors.images}
            />
          )}
        />
      </FormField>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting || uploading}>
          {isSubmitting && <Spinner />}
          {destination ? 'Save changes' : 'Add destination'}
        </Button>
      </DialogFooter>
    </form>
  )
}

function toFormValues(d?: AdminDestination): DestinationInput {
  return {
    name: d?.name ?? '',
    slug: d?.slug ?? '',
    countryId: d ? String(d.countryId) : '',
    summary: d?.summary ?? '',
    images: d?.images.map((image) => ({ id: image.fileId, url: image.url })) ?? [],
    isFeatured: d?.isFeatured ?? false,
    sortOrder: d?.sortOrder ?? null, // a new destination: empty = at the end
  }
}

/** Form values -> API body. Empty text becomes null ("not set"), as the API expects. */
function toRequest(values: DestinationInput): DestinationRequest {
  return {
    countryId: Number(values.countryId),
    name: values.name,
    slug: values.slug || null,
    summary: values.summary || null,
    imageFileIds: values.images.map((image) => image.id),
    isFeatured: values.isFeatured,
    sortOrder: values.sortOrder,
  }
}
