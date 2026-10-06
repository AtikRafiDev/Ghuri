import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ImageIcon, ImagesIcon, MapPinIcon, PlusIcon, SearchIcon, StarIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import { ListFooter } from '../../components/ListFooter'
import { RowActions } from '../../components/RowActions'
import { usedByPackagesReason } from '../../deleteBlockedReason'
import {
  destinationKeys,
  destinationsApi,
  type AdminDestination,
  type DestinationListParams,
  type DestinationScope,
} from '../api/destinations.api'
import { DestinationFormDialog } from '../components/DestinationFormDialog'

const columnCount = 6

/** Admin → Destinations: search, filter, page, add / edit / delete (14-day plan, Day 3). */
export function AdminDestinationsPage() {
  useDocumentMeta({ title: 'Destinations' })
  const queryClient = useQueryClient()

  // Filters live in the URL (blueprint 13.2): refresh, back button and a
  // shared link all keep them. ?q=bazar&scope=national&page=2
  const [searchParams, setSearchParams] = useSearchParams()
  const params: DestinationListParams = {
    search: searchParams.get('q') ?? '',
    scope: toScope(searchParams.get('scope')),
    page: Math.max(1, Number(searchParams.get('page')) || 1),
  }

  const updateParams = (changes: Record<string, string | null>) =>
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current)
        for (const [key, value] of Object.entries(changes)) {
          if (value) next.set(key, value)
          else next.delete(key)
        }
        return next
      },
      { replace: true }, // typing a search shouldn't fill the back-button history
    )

  // The search box updates instantly; the URL (and so the API call) only
  // after 300 ms without typing - not one request per keystroke.
  const [searchText, setSearchText] = useState(params.search)
  const searchTimer = useRef<number | undefined>(undefined)
  useEffect(() => () => window.clearTimeout(searchTimer.current), [])
  const onSearchChange = (text: string) => {
    setSearchText(text)
    window.clearTimeout(searchTimer.current)
    searchTimer.current = window.setTimeout(() => updateParams({ q: text.trim() || null, page: null }), 300)
  }

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: destinationKeys.list(params),
    queryFn: () => destinationsApi.list(params),
    // Keep showing the current page while the next one loads, instead of
    // flashing an empty table.
    placeholderData: keepPreviousData,
  })

  const [form, setForm] = useState<{ open: boolean; destination?: AdminDestination }>({ open: false })
  const [toDelete, setToDelete] = useState<{ open: boolean; destination?: AdminDestination }>({ open: false })

  const hasFilters = params.search !== '' || params.scope !== null
  const showAll = () => {
    setSearchText('')
    updateParams({ q: null, scope: null, page: null })
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Destinations"
        description="Places tours go to - inside Bangladesh and abroad."
        actions={
          <Button onClick={() => setForm({ open: true })}>
            <PlusIcon />
            Add destination
          </Button>
        }
      />

      {/* One aligned row from "sm" up; on a phone the search and the dropdown stack. */}
      <div className="grid gap-3 sm:flex sm:flex-wrap sm:items-center">
        <div className="relative sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={searchText}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Search destination or country…"
            className="pl-10"
            aria-label="Search destinations"
          />
        </div>
        <Select
          value={params.scope ?? 'all'}
          onValueChange={(value) => updateParams({ scope: value === 'all' ? null : value, page: null })}
        >
          <SelectTrigger className="w-full sm:w-56" aria-label="Filter by scope">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
            <SelectItem value="all">All destinations</SelectItem>
            <SelectItem value="national">National (Bangladesh)</SelectItem>
            <SelectItem value="international">International</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {data?.items.length === 0 ? (
        hasFilters || params.page > 1 ? (
          <EmptyState
            icon={SearchIcon}
            title={hasFilters ? 'No destination matches these filters' : 'Nothing on this page'}
            text={hasFilters ? 'Try another word, or clear the filters to see every destination.' : 'This page is past the end of the list.'}
          >
            <Button variant="outline" onClick={showAll}>
              Show all destinations
            </Button>
          </EmptyState>
        ) : (
          <EmptyState icon={MapPinIcon} title="No destinations yet" text="Add the places your tours go to - Cox's Bazar, Sylhet, Bali…">
            <Button onClick={() => setForm({ open: true })}>
              <PlusIcon />
              Add destination
            </Button>
          </EmptyState>
        )
      ) : (
        <div className="overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Destination</TableHead>
                <TableHead>Country</TableHead>
                <TableHead>Home page</TableHead>
                <TableHead className="text-right">Order</TableHead>
                <TableHead className="text-right">Packages</TableHead>
                <TableHead className="w-24 text-right">
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody className={cn('transition-opacity duration-300', isFetching && !isPending && 'opacity-60')}>
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

              {data?.items.map((d) => (
                <TableRow key={d.id}>
                  <TableCell>
                    <div className="flex items-center gap-3.5">
                      {d.images.length > 0 ? (
                        // The cover, plus how many photos the gallery has.
                        <span className="relative block h-12 w-18 shrink-0 overflow-hidden rounded-xl bg-ink-100 ring-1 ring-ink-200/70">
                          <img src={d.images[0].url} alt="" loading="lazy" className="size-full object-cover" />
                          {d.images.length > 1 && (
                            <span className="nums absolute right-1 bottom-1 inline-flex items-center gap-0.5 rounded-md bg-forest-950/70 px-1 py-px text-[0.625rem] font-semibold text-white backdrop-blur-sm">
                              <ImagesIcon className="size-2.5" aria-hidden />
                              {d.images.length}
                              <span className="sr-only"> photos</span>
                            </span>
                          )}
                        </span>
                      ) : (
                        <span className="flex h-12 w-18 shrink-0 items-center justify-center rounded-xl bg-ink-100 text-ink-400 ring-1 ring-ink-200/70">
                          <ImageIcon className="size-4" />
                        </span>
                      )}
                      <span className="grid min-w-0 gap-0.5">
                        <span className="font-semibold text-ink-900">{d.name}</span>
                        <span className="text-xs text-ink-500">/{d.slug}</span>
                      </span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="grid justify-items-start gap-1">
                      <span className="text-ink-700">{d.countryName}</span>
                      <Badge variant={d.isInternational ? 'info' : 'neutral'}>{d.isInternational ? 'International' : 'National'}</Badge>
                    </div>
                  </TableCell>
                  <TableCell>
                    {d.isFeatured ? (
                      <Badge variant="warning">
                        <StarIcon className="fill-current" aria-hidden />
                        Featured
                      </Badge>
                    ) : (
                      <span className="text-ink-300">—</span>
                    )}
                  </TableCell>
                  <TableCell className="nums text-right text-ink-600">{d.sortOrder}</TableCell>
                  <TableCell className="nums text-right font-semibold text-ink-900">{d.packageCount}</TableCell>
                  <TableCell className="text-right">
                    <RowActions
                      name={d.name}
                      onEdit={() => setForm({ open: true, destination: d })}
                      onDelete={() => setToDelete({ open: true, destination: d })}
                      deleteBlockedReason={usedByPackagesReason(d.packageCount)}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <ListFooter data={data} onPage={(page) => updateParams({ page: page > 1 ? String(page) : null })} />

      <DestinationFormDialog
        open={form.open}
        destination={form.destination}
        // Only "open" flips on close: the dialog keeps its title while it animates out.
        onOpenChange={(open) => setForm((current) => ({ ...current, open }))}
      />

      <ConfirmDeleteDialog
        open={toDelete.open}
        onOpenChange={(open) => setToDelete((current) => ({ ...current, open }))}
        title={`Delete ${toDelete.destination?.name ?? 'destination'}?`}
        description="It disappears from the website and admin lists. Existing bookings are not affected."
        successMessage="Destination deleted"
        onConfirm={async () => {
          if (!toDelete.destination) return
          await destinationsApi.remove(toDelete.destination.id)
          await queryClient.invalidateQueries({ queryKey: destinationKeys.all })
        }}
      />
    </div>
  )
}

/** First load: rows shaped like the real ones (photo + two lines, country + badge, numbers), so nothing jumps. */
function SkeletonRows() {
  return Array.from({ length: 5 }, (_, i) => (
    <TableRow key={i} className="hover:bg-transparent" aria-hidden>
      <TableCell>
        <div className="flex items-center gap-3.5">
          <Skeleton className="h-12 w-18 rounded-xl" />
          <div className="grid gap-1.5">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-3 w-24" />
          </div>
        </div>
      </TableCell>
      <TableCell>
        <div className="grid gap-1.5">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-6 w-20 rounded-full" />
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="h-6 w-20 rounded-full" />
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

function toScope(value: string | null): DestinationScope | null {
  return value === 'national' || value === 'international' ? value : null
}
