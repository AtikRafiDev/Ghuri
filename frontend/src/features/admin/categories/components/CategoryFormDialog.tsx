import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { slugify } from '@/shared/lib/slug'
import { categoriesApi, categoryKeys, type AdminCategory, type CategoryRequest } from '../api/categories.api'
import { categoryIcons } from '../categoryIcons'
import { categorySchema, type CategoryInput } from '../schemas/category.schema'
import { CategoryIcon } from './CategoryIcon'

type CategoryFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Present = edit this one; absent = add a new one. */
  category?: AdminCategory
}

export function CategoryFormDialog({ open, onOpenChange, category }: CategoryFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{category ? `Edit ${category.name}` : 'Add category'}</DialogTitle>
          <DialogDescription>A tour type customers can filter by, like Beach or Honeymoon.</DialogDescription>
        </DialogHeader>
        <CategoryForm category={category} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

function CategoryForm({ category, onDone }: { category?: AdminCategory; onDone: () => void }) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<CategoryInput>({
    resolver: zodResolver(categorySchema),
    defaultValues: {
      name: category?.name ?? '',
      slug: category?.slug ?? '',
      icon: category?.icon ?? '',
      sortOrder: category?.sortOrder ?? 0,
    },
  })
  const { errors, isSubmitting } = form.formState
  const [name, slug, icon] = useWatch({ control: form.control, name: ['name', 'slug', 'icon'] })
  const finalSlug = slugify(slug || name)

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    const body: CategoryRequest = {
      name: values.name,
      slug: values.slug || null,
      icon: values.icon || null,
      sortOrder: values.sortOrder,
    }
    try {
      if (category) await categoriesApi.update(category.id, body)
      else await categoriesApi.create(body)
      await queryClient.invalidateQueries({ queryKey: categoryKeys.all })
      onDone()
      // The dialog closes, so a toast is the "it worked".
      notify.success(category ? 'Category saved' : 'Category added', { description: values.name })
    } catch (error) {
      setFormError(applyServerErrors(form, error, { category_name_taken: 'name', category_slug_taken: 'slug' }))
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <div className="grid gap-5 sm:grid-cols-2">
        <TextField label="Name" autoFocus error={errors.name?.message} {...form.register('name')} />
        <TextField
          label="URL name (slug)"
          placeholder="Empty = made from the name"
          error={errors.slug?.message}
          hint={finalSlug ? `Address: /tours?category=${finalSlug}` : undefined}
          {...form.register('slug')}
        />
      </div>

      <FormField label="Icon" htmlFor="icon-none" error={errors.icon?.message}>
        <Controller
          control={form.control}
          name="icon"
          render={({ field }) => (
            // A grid of buttons behaves like radio buttons: exactly one is chosen.
            // auto-fill: as many equal squares per row as fit the dialog's width.
            <div role="radiogroup" aria-label="Icon" className="grid grid-cols-[repeat(auto-fill,minmax(2.5rem,1fr))] gap-2">
              {['', ...Object.keys(categoryIcons)].map((iconName) => (
                <button
                  key={iconName || 'none'}
                  id={iconName ? undefined : 'icon-none'}
                  type="button"
                  role="radio"
                  aria-checked={field.value === iconName}
                  aria-label={iconName || 'No icon'}
                  title={iconName || 'No icon'}
                  onClick={() => field.onChange(iconName)}
                  className={cn(
                    'flex aspect-square items-center justify-center rounded-xl border border-ink-200 bg-card text-ink-500 shadow-soft transition-[background-color,border-color,color,translate] duration-200 outline-none hover:-translate-y-0.5 hover:border-forest-300 hover:bg-forest-50 hover:text-forest-700 focus-visible:border-forest-500 focus-visible:ring-4 focus-visible:ring-forest-500/15',
                    field.value === iconName && 'border-primary bg-primary text-white hover:border-primary hover:bg-primary hover:text-white',
                  )}
                >
                  {iconName ? <CategoryIcon name={iconName} className="size-[18px]" /> : <span className="text-[0.6875rem] font-semibold">None</span>}
                </button>
              ))}
            </div>
          )}
        />
      </FormField>

      <div className="grid gap-5 sm:grid-cols-2">
        <TextField
          label="Sort order"
          type="number"
          min={0}
          hint="Lower numbers come first."
          error={errors.sortOrder?.message}
          {...form.register('sortOrder', { valueAsNumber: true })}
        />
        {/* The chosen icon and name together, as a chip - the same height as the field beside it. */}
        <div className="grid content-start gap-2">
          <span className="text-[0.8125rem] leading-none font-semibold text-ink-700">Preview</span>
          <span className="inline-flex h-10 w-fit max-w-full items-center gap-2 rounded-full border border-ink-200 bg-card pr-4 pl-1.5 text-sm font-semibold text-ink-900 shadow-soft">
            <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-forest-50 text-forest-700">
              <CategoryIcon name={icon || null} className="size-4" />
            </span>
            <span className="truncate">{name.trim() || 'Category name'}</span>
          </span>
        </div>
      </div>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          {category ? 'Save changes' : 'Add category'}
        </Button>
      </DialogFooter>
    </form>
  )
}
