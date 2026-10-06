import { useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusIcon, TagsIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { RowActions } from '../../components/RowActions'
import { usedByPackagesReason } from '../../deleteBlockedReason'
import { categoriesApi, categoryKeys, type AdminCategory } from '../api/categories.api'
import { CategoryFormDialog } from '../components/CategoryFormDialog'
import { CategoryIcon } from '../components/CategoryIcon'

const columnCount = 4

/** Admin → Categories: tour types, add / edit / delete (14-day plan, Day 3). A short list, so no paging. */
export function AdminCategoriesPage() {
  useDocumentMeta({ title: 'Categories' })
  const queryClient = useQueryClient()
  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: categoryKeys.all,
    queryFn: categoriesApi.list,
  })

  const [form, setForm] = useState<{ open: boolean; category?: AdminCategory }>({ open: false })
  const [toDelete, setToDelete] = useState<{ open: boolean; category?: AdminCategory }>({ open: false })

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Categories"
        description="Tour types customers filter by - Beach, Hill, Honeymoon…"
        actions={
          <Button onClick={() => setForm({ open: true })}>
            <PlusIcon />
            Add category
          </Button>
        }
      />

      {data?.length === 0 ? (
        <EmptyState icon={TagsIcon} title="No categories yet" text="Add the tour types customers can filter by, like Beach or Honeymoon.">
          <Button onClick={() => setForm({ open: true })}>
            <PlusIcon />
            Add category
          </Button>
        </EmptyState>
      ) : (
        <div className="overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Category</TableHead>
                <TableHead className="w-28 text-right">Order</TableHead>
                <TableHead className="w-28 text-right">Packages</TableHead>
                <TableHead className="w-24 text-right">
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isPending && <SkeletonRows />}

              {isError && (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={columnCount} className="py-12 text-center">
                    <p className="text-sm font-medium text-destructive">{toAppError(error).message}</p>
                    <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                      Try again
                    </Button>
                  </TableCell>
                </TableRow>
              )}

              {data?.map((c) => (
                <TableRow key={c.id}>
                  <TableCell>
                    <div className="flex items-center gap-3.5">
                      <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-700 ring-1 ring-forest-100">
                        <CategoryIcon name={c.icon} className="size-[18px]" />
                      </span>
                      <span className="grid min-w-0 gap-0.5">
                        <span className="font-semibold text-ink-900">{c.name}</span>
                        <span className="text-xs text-ink-500">/{c.slug}</span>
                      </span>
                    </div>
                  </TableCell>
                  <TableCell className="nums text-right text-ink-600">{c.sortOrder}</TableCell>
                  <TableCell className="nums text-right font-semibold text-ink-900">{c.packageCount}</TableCell>
                  <TableCell className="text-right">
                    <RowActions
                      name={c.name}
                      onEdit={() => setForm({ open: true, category: c })}
                      onDelete={() => setToDelete({ open: true, category: c })}
                      deleteBlockedReason={usedByPackagesReason(c.packageCount)}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <CategoryFormDialog
        open={form.open}
        category={form.category}
        onOpenChange={(open) => setForm((current) => ({ ...current, open }))}
      />

      <ConfirmDeleteDialog
        open={toDelete.open}
        onOpenChange={(open) => setToDelete((current) => ({ ...current, open }))}
        title={`Delete ${toDelete.category?.name ?? 'category'}?`}
        description="It disappears from the website's filters and the admin list."
        successMessage="Category deleted"
        onConfirm={async () => {
          if (!toDelete.category) return
          await categoriesApi.remove(toDelete.category.id)
          await queryClient.invalidateQueries({ queryKey: categoryKeys.all })
        }}
      />
    </div>
  )
}

/** First load: rows shaped like the real ones (icon tile + two lines, two numbers), so nothing jumps. */
function SkeletonRows() {
  return Array.from({ length: 5 }, (_, i) => (
    <TableRow key={i} className="hover:bg-transparent" aria-hidden>
      <TableCell>
        <div className="flex items-center gap-3.5">
          <Skeleton className="size-10 rounded-xl" />
          <div className="grid gap-1.5">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-3 w-16" />
          </div>
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto h-4 w-6" />
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto h-4 w-6" />
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto h-9 w-20 rounded-xl" />
      </TableCell>
    </TableRow>
  ))
}
