import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { ImageIcon, PencilIcon, PlusIcon, SearchIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import {
  destinationKeys,
  destinationsApi,
  type AdminDestination,
  type DestinationListParams,
  type DestinationScope,
} from '../api/destinations.api'
import { DestinationFormDialog } from '../components/DestinationFormDialog'

const columnCount = 7

/** Admin → Destinations: search, filter, page, add / edit / delete (14-day plan, Day 3). */
export function AdminDestinationsPage() {
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

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Destinations</h1>
          <p className="text-muted-foreground">Places tours go to - inside Bangladesh and abroad.</p>
        </div>
        <Button onClick={() => setForm({ open: true })}>
          <PlusIcon />
          Add destination
        </Button>
      </div>

      <div className="flex flex-wrap gap-2">
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={searchText}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Search destination or country…"
            className="pl-8"
            aria-label="Search destinations"
          />
        </div>
        <Select
          value={params.scope ?? 'all'}
          onValueChange={(value) => updateParams({ scope: value === 'all' ? null : value, page: null })}
        >
          <SelectTrigger className="w-44" aria-label="Filter by scope">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All destinations</SelectItem>
            <SelectItem value="national">National (Bangladesh)</SelectItem>
            <SelectItem value="international">International</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-20">Photo</TableHead>
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
          <TableBody className={isFetching && !isPending ? 'opacity-60 transition-opacity' : undefined}>
            {isPending &&
              Array.from({ length: 5 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={columnCount}>
                    <Skeleton className="h-10 w-full" />
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

            {data?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center text-muted-foreground">
                  {hasFilters ? 'No destination matches these filters.' : 'No destinations yet - add the first one.'}
                  {(hasFilters || params.page > 1) && (
                    <div>
                      <Button
                        variant="link"
                        onClick={() => {
                          setSearchText('')
                          updateParams({ q: null, scope: null, page: null })
                        }}
                      >
                        Show all
                      </Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            )}

            {data?.items.map((d) => (
              <TableRow key={d.id}>
                <TableCell>
                  {d.images.length > 0 ? (
                    // The cover, plus how many photos the gallery has.
                    <div className="relative h-10 w-16">
                      <img src={d.images[0].url} alt="" loading="lazy" className="size-full rounded-md object-cover" />
                      {d.images.length > 1 && (
                        <span className="absolute right-0.5 bottom-0.5 rounded bg-background/85 px-1 text-[10px] font-medium tabular-nums">
                          {d.images.length}
                        </span>
                      )}
                    </div>
                  ) : (
                    <div className="flex h-10 w-16 items-center justify-center rounded-md bg-muted">
                      <ImageIcon className="size-4 text-muted-foreground" />
                    </div>
                  )}
                </TableCell>
                <TableCell>
                  <div className="font-medium">{d.name}</div>
                  <div className="text-xs text-muted-foreground">/{d.slug}</div>
                </TableCell>
                <TableCell>
                  <div>{d.countryName}</div>
                  <Badge variant={d.isInternational ? 'secondary' : 'outline'}>
                    {d.isInternational ? 'International' : 'National'}
                  </Badge>
                </TableCell>
                <TableCell>{d.isFeatured ? <Badge>Featured</Badge> : <span className="text-muted-foreground">-</span>}</TableCell>
                <TableCell className="text-right tabular-nums">{d.sortOrder}</TableCell>
                <TableCell className="text-right tabular-nums">{d.packageCount}</TableCell>
                <TableCell className="text-right">
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" size="icon-sm" aria-label={`Edit ${d.name}`} onClick={() => setForm({ open: true, destination: d })}>
                      <PencilIcon />
                    </Button>
                    {d.packageCount > 0 ? (
                      // A disabled button gets no mouse events, so the tooltip hangs on a wrapper.
                      <Tooltip>
                        <TooltipTrigger asChild>
                          <span tabIndex={0}>
                            <Button variant="ghost" size="icon-sm" disabled aria-label={`Delete ${d.name}`}>
                              <Trash2Icon />
                            </Button>
                          </span>
                        </TooltipTrigger>
                        <TooltipContent>Used by {d.packageCount} package(s) - can't delete</TooltipContent>
                      </Tooltip>
                    ) : (
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Delete ${d.name}`}
                        onClick={() => setToDelete({ open: true, destination: d })}
                      >
                        <Trash2Icon />
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {data && data.totalCount > 0 && (
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
          <span>
            {(data.page - 1) * data.pageSize + 1}–{Math.min(data.page * data.pageSize, data.totalCount)} of {data.totalCount}
          </span>
          <div className="flex items-center gap-2">
            <span>
              Page {data.page} of {data.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={params.page <= 1}
              onClick={() => updateParams({ page: params.page > 2 ? String(params.page - 1) : null })}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={params.page >= data.totalPages}
              onClick={() => updateParams({ page: String(params.page + 1) })}
            >
              Next
            </Button>
          </div>
        </div>
      )}

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
        onConfirm={async () => {
          if (!toDelete.destination) return
          await destinationsApi.remove(toDelete.destination.id)
          await queryClient.invalidateQueries({ queryKey: destinationKeys.all })
        }}
      />
    </div>
  )
}

function toScope(value: string | null): DestinationScope | null {
  return value === 'national' || value === 'international' ? value : null
}
