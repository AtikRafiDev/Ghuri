import { useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { RowActions } from '../../components/RowActions'
import { usedByPackagesReason } from '../../deleteBlockedReason'
import { categoriesApi, categoryKeys, type AdminCategory } from '../api/categories.api'
import { CategoryFormDialog } from '../components/CategoryFormDialog'
import { CategoryIcon } from '../components/CategoryIcon'

const columnCount = 5

/** Admin → Categories: tour types, add / edit / delete (14-day plan, Day 3). A short list, so no paging. */
export function AdminCategoriesPage() {
  const queryClient = useQueryClient()
  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: categoryKeys.all,
    queryFn: categoriesApi.list,
  })

  const [form, setForm] = useState<{ open: boolean; category?: AdminCategory }>({ open: false })
  const [toDelete, setToDelete] = useState<{ open: boolean; category?: AdminCategory }>({ open: false })

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Categories</h1>
          <p className="text-muted-foreground">Tour types customers filter by - Beach, Hill, Honeymoon…</p>
        </div>
        <Button onClick={() => setForm({ open: true })}>
          <PlusIcon />
          Add category
        </Button>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-14">Icon</TableHead>
              <TableHead>Category</TableHead>
              <TableHead className="text-right">Order</TableHead>
              <TableHead className="text-right">Packages</TableHead>
              <TableHead className="w-24 text-right">
                <span className="sr-only">Actions</span>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isPending &&
              Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={columnCount}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {isError && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center">
                  <p className="text-destructive">{error.message}</p>
                  <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                    Try again
                  </Button>
                </TableCell>
              </TableRow>
            )}

            {data?.length === 0 && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center text-muted-foreground">
                  No categories yet - add the first one.
                </TableCell>
              </TableRow>
            )}

            {data?.map((c) => (
              <TableRow key={c.id}>
                <TableCell>
                  <div className="flex size-8 items-center justify-center rounded-md bg-muted">
                    <CategoryIcon name={c.icon} className="size-4" />
                  </div>
                </TableCell>
                <TableCell>
                  <div className="font-medium">{c.name}</div>
                  <div className="text-xs text-muted-foreground">/{c.slug}</div>
                </TableCell>
                <TableCell className="text-right tabular-nums">{c.sortOrder}</TableCell>
                <TableCell className="text-right tabular-nums">{c.packageCount}</TableCell>
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
        onConfirm={async () => {
          if (!toDelete.category) return
          await categoriesApi.remove(toDelete.category.id)
          await queryClient.invalidateQueries({ queryKey: categoryKeys.all })
        }}
      />
    </div>
  )
}
