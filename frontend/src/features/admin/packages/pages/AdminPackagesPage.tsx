import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ImageIcon, PencilIcon, PlusIcon, SearchIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import {
  describeDuration,
  formatTaka,
  packageKeys,
  packagesApi,
  PackageStatus,
  packageStatusLabels,
  PricingMode,
  type PackageListParams,
} from '../api/packages.api'
import { PackageStatusBadge } from '../components/PackageStatusBadge'

const columnCount = 7

/** Admin → Packages: search, filter, page; add / edit open their own page (17-day plan, Day 4). */
export function AdminPackagesPage() {
  // Filters live in the URL (blueprint 13.2): refresh, back button and a
  // shared link all keep them. ?q=beach&status=1&mode=2&page=2
  const [searchParams, setSearchParams] = useSearchParams()
  const params: PackageListParams = {
    search: searchParams.get('q') ?? '',
    status: toStatus(searchParams.get('status')),
    pricingMode: toMode(searchParams.get('mode')),
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
    queryKey: packageKeys.list(params),
    queryFn: () => packagesApi.list(params),
    // Keep showing the current page while the next one loads.
    placeholderData: keepPreviousData,
  })

  const hasFilters = params.search !== '' || params.status !== null || params.pricingMode !== null

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Packages</h1>
          <p className="text-muted-foreground">Tours you sell - on set dates, or as a flexible stay.</p>
        </div>
        <Button asChild>
          <Link to="/admin/packages/new">
            <PlusIcon />
            Add package
          </Link>
        </Button>
      </div>

      <div className="flex flex-wrap gap-2">
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={searchText}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Search title or code (PKG1001)…"
            className="pl-8"
            aria-label="Search packages"
          />
        </div>
        <Select
          value={params.status ? String(params.status) : 'all'}
          onValueChange={(value) => updateParams({ status: value === 'all' ? null : value, page: null })}
        >
          <SelectTrigger className="w-40" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {Object.values(PackageStatus).map((status) => (
              <SelectItem key={status} value={String(status)}>
                {packageStatusLabels[status]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={params.pricingMode ? String(params.pricingMode) : 'all'}
          onValueChange={(value) => updateParams({ mode: value === 'all' ? null : value, page: null })}
        >
          <SelectTrigger className="w-44" aria-label="Filter by pricing">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Fixed and flexible</SelectItem>
            <SelectItem value={String(PricingMode.FixedDepartures)}>Fixed departures</SelectItem>
            <SelectItem value={String(PricingMode.FlexibleStay)}>Flexible stay</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-20">Photo</TableHead>
              <TableHead>Package</TableHead>
              <TableHead>Destination</TableHead>
              <TableHead>Pricing</TableHead>
              <TableHead className="text-right">From (per adult)</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-16 text-right">
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
                  {hasFilters ? 'No package matches these filters.' : 'No packages yet - add the first one.'}
                  {(hasFilters || params.page > 1) && (
                    <div>
                      <Button
                        variant="link"
                        onClick={() => {
                          setSearchText('')
                          updateParams({ q: null, status: null, mode: null, page: null })
                        }}
                      >
                        Show all
                      </Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            )}

            {data?.items.map((p) => (
              <TableRow key={p.id}>
                <TableCell>
                  {p.coverImageUrl ? (
                    <img src={p.coverImageUrl} alt="" loading="lazy" className="h-10 w-16 rounded-md object-cover" />
                  ) : (
                    <div className="flex h-10 w-16 items-center justify-center rounded-md bg-muted">
                      <ImageIcon className="size-4 text-muted-foreground" />
                    </div>
                  )}
                </TableCell>
                <TableCell>
                  <Link to={`/admin/packages/${p.id}`} className="font-medium hover:underline">
                    {p.title}
                  </Link>
                  <div className="text-xs text-muted-foreground">
                    {p.packageCode}
                    {p.isFeatured && ' · featured'}
                  </div>
                </TableCell>
                <TableCell>{p.destinationName}</TableCell>
                <TableCell>
                  <Badge variant={p.pricingMode === PricingMode.FlexibleStay ? 'secondary' : 'outline'}>
                    {p.pricingMode === PricingMode.FlexibleStay ? 'Flexible stay' : 'Fixed departures'}
                  </Badge>
                  <div className="text-xs text-muted-foreground">{describeDuration(p)}</div>
                </TableCell>
                <TableCell className="text-right tabular-nums">
                  {p.priceFrom > 0 ? (
                    formatTaka(p.priceFrom)
                  ) : (
                    // A fixed package gets its prices from departures (Day 5).
                    <span className="text-xs text-muted-foreground">No departures yet</span>
                  )}
                </TableCell>
                <TableCell>
                  <PackageStatusBadge status={p.status} />
                </TableCell>
                <TableCell className="text-right">
                  <Button asChild variant="ghost" size="icon-sm" aria-label={`Edit ${p.title}`}>
                    <Link to={`/admin/packages/${p.id}`}>
                      <PencilIcon />
                    </Link>
                  </Button>
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
    </div>
  )
}

function toStatus(value: string | null): PackageStatus | null {
  const n = Number(value)
  return n === 1 || n === 2 || n === 3 ? n : null
}

function toMode(value: string | null): PricingMode | null {
  const n = Number(value)
  return n === 1 || n === 2 ? n : null
}
