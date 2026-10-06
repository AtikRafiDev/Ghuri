import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ImageIcon, PackageIcon, PencilIcon, PlusIcon, SearchIcon, StarIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { PageHeader } from '@/shared/components/PageHeader'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ListFooter } from '../../components/ListFooter'
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

const columnCount = 6

/** Admin → Packages: search, filter, page; add / edit open their own page (17-day plan, Day 4). */
export function AdminPackagesPage() {
  useDocumentMeta({ title: 'Packages' })

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
  const showAll = () => {
    setSearchText('')
    updateParams({ q: null, status: null, mode: null, page: null })
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Packages"
        description="Tours you sell - on set dates, or as a flexible stay."
        actions={
          <Button asChild>
            <Link to="/admin/packages/new">
              <PlusIcon />
              Add package
            </Link>
          </Button>
        }
      />

      {/* Filter bar: on a phone the search takes the first row and the two
          dropdowns share the second; from "sm" up it's one aligned row. */}
      <div className="grid grid-cols-2 gap-3 sm:flex sm:flex-wrap sm:items-center">
        <div className="relative col-span-2 sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={searchText}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Search title or code (PKG1001)…"
            className="pl-10"
            aria-label="Search packages"
          />
        </div>
        <Select
          value={params.status ? String(params.status) : 'all'}
          onValueChange={(value) => updateParams({ status: value === 'all' ? null : value, page: null })}
        >
          <SelectTrigger className="w-full sm:w-44" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
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
          <SelectTrigger className="w-full sm:w-48" aria-label="Filter by pricing">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
            <SelectItem value="all">Fixed and flexible</SelectItem>
            <SelectItem value={String(PricingMode.FixedDepartures)}>Fixed departures</SelectItem>
            <SelectItem value={String(PricingMode.FlexibleStay)}>Flexible stay</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {data?.items.length === 0 ? (
        hasFilters || params.page > 1 ? (
          <EmptyState
            icon={SearchIcon}
            title={hasFilters ? 'No package matches these filters' : 'Nothing on this page'}
            text={hasFilters ? 'Try another word, or clear the filters to see every package.' : 'This page is past the end of the list.'}
          >
            <Button variant="outline" onClick={showAll}>
              Show all packages
            </Button>
          </EmptyState>
        ) : (
          <EmptyState icon={PackageIcon} title="No packages yet" text="Add the first tour - it starts as a draft, so nobody sees it until it's published.">
            <Button asChild>
              <Link to="/admin/packages/new">
                <PlusIcon />
                Add package
              </Link>
            </Button>
          </EmptyState>
        )
      ) : (
        <div className="overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
          <Table>
            <TableHeader>
              <TableRow>
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

              {data?.items.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>
                    <Link to={`/admin/packages/${p.id}`} className="group flex items-center gap-3.5">
                      {p.coverImageUrl ? (
                        <span className="block h-12 w-18 shrink-0 overflow-hidden rounded-xl bg-ink-100 ring-1 ring-ink-200/70">
                          <img
                            src={p.coverImageUrl}
                            alt=""
                            loading="lazy"
                            className="size-full object-cover transition-transform duration-500 group-hover:scale-105"
                          />
                        </span>
                      ) : (
                        <span className="flex h-12 w-18 shrink-0 items-center justify-center rounded-xl bg-ink-100 text-ink-400 ring-1 ring-ink-200/70">
                          <ImageIcon className="size-4" />
                        </span>
                      )}
                      <span className="grid min-w-0 gap-0.5">
                        <span className="font-semibold text-ink-900 transition-colors group-hover:text-forest-700">{p.title}</span>
                        <span className="flex items-center gap-2 text-xs text-ink-500">
                          <span className="font-mono">{p.packageCode}</span>
                          {p.isFeatured && (
                            <span className="inline-flex items-center gap-1 font-semibold text-sun-700">
                              <StarIcon className="size-3 fill-sun-500 text-sun-500" aria-hidden />
                              Featured
                            </span>
                          )}
                        </span>
                      </span>
                    </Link>
                  </TableCell>
                  <TableCell className="text-ink-700">{p.destinationName}</TableCell>
                  <TableCell>
                    <div className="grid justify-items-start gap-1">
                      <Badge variant={p.pricingMode === PricingMode.FlexibleStay ? 'info' : 'neutral'}>
                        {p.pricingMode === PricingMode.FlexibleStay ? 'Flexible stay' : 'Fixed departures'}
                      </Badge>
                      <span className="nums text-xs text-ink-500">{describeDuration(p)}</span>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    {p.priceFrom > 0 ? (
                      <span className="nums font-semibold text-ink-900">{formatTaka(p.priceFrom)}</span>
                    ) : (
                      // A fixed package gets its prices from departures (Day 5).
                      <span className="text-xs text-ink-400">No departures yet</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <PackageStatusBadge status={p.status} />
                  </TableCell>
                  <TableCell className="text-right">
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button asChild variant="ghost" size="icon-sm" className="text-ink-400 hover:bg-forest-50 hover:text-forest-700">
                          <Link to={`/admin/packages/${p.id}`} aria-label={`Edit ${p.title}`}>
                            <PencilIcon />
                          </Link>
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent>Edit</TooltipContent>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <ListFooter data={data} onPage={(page) => updateParams({ page: page > 1 ? String(page) : null })} />
    </div>
  )
}

/** First load: rows shaped like the real ones (photo + two lines, badges, price), so nothing jumps. */
function SkeletonRows() {
  return Array.from({ length: 5 }, (_, i) => (
    <TableRow key={i} className="hover:bg-transparent" aria-hidden>
      <TableCell>
        <div className="flex items-center gap-3.5">
          <Skeleton className="h-12 w-18 rounded-xl" />
          <div className="grid gap-1.5">
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-3 w-20" />
          </div>
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-24" />
      </TableCell>
      <TableCell>
        <div className="grid gap-1.5">
          <Skeleton className="h-6 w-28 rounded-full" />
          <Skeleton className="h-3 w-20" />
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto h-4 w-16" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-6 w-20 rounded-full" />
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto size-9 rounded-xl" />
      </TableCell>
    </TableRow>
  ))
}

function toStatus(value: string | null): PackageStatus | null {
  const n = Number(value)
  return n === 1 || n === 2 || n === 3 ? n : null
}

function toMode(value: string | null): PricingMode | null {
  const n = Number(value)
  return n === 1 || n === 2 ? n : null
}
