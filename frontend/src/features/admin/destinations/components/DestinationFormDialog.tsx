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
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
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
    <form onSubmit={onSubmit} noValidate className="grid gap-6">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <div className="grid gap-6 md:grid-cols-2">
        {/* Left: what customers read */}
        <div className="grid content-start gap-4">
          <TextField label="Name" autoFocus error={errors.name?.message} {...form.register('name')} />

          <TextField
            label="URL name (slug)"
            placeholder="Leave empty to make it from the name"
            error={errors.slug?.message}
            hint={finalSlug ? `Address: /destinations/${finalSlug}` : undefined}
            {...form.register('slug')}
          />

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

          <FormField label="Summary" htmlFor="summary" error={errors.summary?.message} hint="One or two sentences for the destination card.">
            <Textarea id="summary" rows={4} aria-invalid={errors.summary ? true : undefined} {...form.register('summary')} />
          </FormField>
        </div>

        {/* Right: photo, display and SEO */}
        <div className="grid content-start gap-4">
          <div className="grid grid-cols-2 items-end gap-4">
            <TextField
              label="Sort order"
              type="number"
              min={0}
              error={errors.sortOrder?.message}
              {...form.register('sortOrder', { valueAsNumber: true })}
            />
            <Controller
              control={form.control}
              name="isFeatured"
              render={({ field }) => (
                <div className="flex h-8 items-center gap-2">
                  <Checkbox id="isFeatured" checked={field.value} onCheckedChange={(checked) => field.onChange(checked === true)} />
                  <Label htmlFor="isFeatured">Show on home page</Label>
                </div>
              )}
            />
          </div>

          <TextField
            label="SEO title"
            placeholder={name ? `${name} Tour Packages | Ghuri` : undefined}
            error={errors.seoTitle?.message}
            hint="The blue link text on Google. Empty = the name."
            {...form.register('seoTitle')}
          />
          <FormField label="SEO description" htmlFor="seoDescription" error={errors.seoDescription?.message} hint="The grey text under the link on Google.">
            <Textarea id="seoDescription" rows={3} aria-invalid={errors.seoDescription ? true : undefined} {...form.register('seoDescription')} />
          </FormField>
        </div>
      </div>

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
    sortOrder: d?.sortOrder ?? 0,
    seoTitle: d?.seoTitle ?? '',
    seoDescription: d?.seoDescription ?? '',
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
    seoTitle: values.seoTitle || null,
    seoDescription: values.seoDescription || null,
  }
}
