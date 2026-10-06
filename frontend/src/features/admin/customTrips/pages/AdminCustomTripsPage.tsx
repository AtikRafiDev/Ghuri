import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CheckCheckIcon, RouteIcon, SearchIcon, SearchXIcon } from 'lucide-react'
import type { MouseEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { tripStatusLabels, type TripStatus } from '@/features/trips/api/trips.api'
import { TripStatusBadge } from '@/features/trips/components/TripStatusBadge'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ListFooter } from '../../components/ListFooter'
import { oneOf, useListParams } from '../../components/useListParams'
import { adminTripKeys, adminTripsApi, type TripQueueParams } from '../api/adminTrips.api'

const columnCount = 6
const statuses = [1, 2, 3, 4, 5, 6, 7] as const satisfies readonly TripStatus[]

/**
 * Admin → Custom trips (17-day plan, Day 15: "custom-trip queue (filter by
 * status)"). Opens on "Waiting" - oldest first, the 24-hour target in view.
 * ?status=all shows everything, newest first.
 */
export function AdminCustomTripsPage() {
  useDocumentMeta({ title: 'Custom trips' })
  const navigate = useNavigate()
  const list = useListParams()
  const rawStatus = list.get('status')
  const params: TripQueueParams = {
    status: rawStatus === 'all' ? null : (oneOf(rawStatus, statuses) ?? 1),
    search: list.search,
    page: list.page,
  }

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: adminTripKeys.queue(params),
    queryFn: () => adminTripsApi.queue(params),
    placeholderData: keepPreviousData,
  })

  // A click anywhere on a row opens the request - except on the link itself,
  // or when the click ended a text selection (copying a trip number).
  const openRow = (tripNo: string) => (event: MouseEvent) => {
    if ((event.target as HTMLElement).closest('a, button') || window.getSelection()?.toString()) return
    navigate(`/admin/custom-trips/${encodeURIComponent(tripNo)}`)
  }

  return (
    <div className="grid gap-6">
      <PageHeader title="Custom trips" description="Requests customers designed themselves. Target: a quote within 24 hours." />

      {/* Filters: the tabs are trimmed to h-10 so they line up with the search box; on a phone they scroll sideways. */}
      <div className="flex min-w-0 animate-fade-up flex-wrap items-center gap-3">
        <Tabs
          value={params.status === null ? 'all' : String(params.status)}
          onValueChange={(v) => list.update({ status: v === '1' ? null : v })}
          className="min-w-0"
        >
          <TabsList className="group-data-horizontal/tabs:h-10">
            <TabsTrigger value="1">Waiting</TabsTrigger>
            <TabsTrigger value="2">Quoted</TabsTrigger>
            <TabsTrigger value="3">Accepted</TabsTrigger>
            <TabsTrigger value="4">Paid</TabsTrigger>
            <TabsTrigger value="all">All</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative w-full sm:w-auto sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Trip no. (CT1001), name or mobile…"
            className="pl-10"
            aria-label="Search custom trips"
          />
        </div>
      </div>

      {isError && (
        <div className="grid justify-items-start gap-3">
          <FormAlert kind="error">{toAppError(error).message}</FormAlert>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Try again
          </Button>
        </div>
      )}

      {data?.items.length === 0 ? (
        params.search ? (
          <EmptyState icon={SearchXIcon} title="No request matches your search" text="Try another trip number, name or mobile number.">
            <Button variant="outline" onClick={() => list.onSearchChange('')}>
              Clear the search
            </Button>
          </EmptyState>
        ) : params.status === 1 ? (
          <EmptyState icon={CheckCheckIcon} title="All quoted" text="No requests waiting - every customer has a price." />
        ) : (
          <EmptyState icon={RouteIcon} title={`No ${params.status ? tripStatusLabels[params.status].toLowerCase() : ''} requests`} text="Requests move through these tabs as customers accept and pay." />
        )
      ) : (
        (isPending || data) && (
          <div className="animate-fade-up overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Request</TableHead>
                  <TableHead>Route</TableHead>
                  <TableHead>Customer</TableHead>
                  <TableHead>Dates</TableHead>
                  <TableHead className="text-right">Quote</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody aria-busy={isPending || undefined} className={cn('transition-opacity duration-300', isFetching && !isPending && 'opacity-60')}>
                {/* First load: rows of shimmer in the table's own shape, so nothing jumps when the requests arrive. */}
                {isPending &&
                  Array.from({ length: 5 }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      {Array.from({ length: columnCount }, (_, j) => (
                        <TableCell key={j}>
                          <Skeleton className={cn('h-4', j === 4 ? 'ml-auto w-16' : j === 1 ? 'w-40' : 'w-24')} />
                          {j !== 1 && j !== 5 && <Skeleton className={cn('mt-2 h-3', j === 4 ? 'ml-auto w-12' : 'w-20')} />}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {data?.items.map((t) => (
                  <TableRow key={t.tripNo} onClick={openRow(t.tripNo)} className="cursor-pointer">
                    <TableCell>
                      <Link
                        to={`/admin/custom-trips/${encodeURIComponent(t.tripNo)}`}
                        className="font-mono text-[0.8125rem] font-semibold text-ink-900 underline-offset-4 transition-colors hover:text-forest-700 hover:underline"
                      >
                        {t.tripNo}
                      </Link>
                      <div className="text-xs text-ink-500">{formatDateTime(t.submittedAtUtc)}</div>
                    </TableCell>
                    <TableCell className="max-w-64 min-w-44 font-medium whitespace-normal text-ink-900">{t.route}</TableCell>
                    <TableCell>
                      <div className="font-medium text-ink-900">{t.contactName}</div>
                      <div className="nums text-xs text-ink-500">
                        {t.contactPhone} · {t.people} people
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="text-ink-900">{formatDate(t.startDate)}</div>
                      <div className="text-xs text-ink-500">{t.totalNights} nights</div>
                    </TableCell>
                    <TableCell className="nums text-right">
                      {t.quoteTotal !== null ? (
                        <div className="font-semibold text-ink-900">{formatTaka(t.quoteTotal)}</div>
                      ) : (
                        <div className="text-ink-400">Not quoted</div>
                      )}
                      {t.quoteExpiresAtUtc && t.status === 2 && <div className="text-xs text-ink-500">until {formatDateTime(t.quoteExpiresAtUtc)}</div>}
                    </TableCell>
                    <TableCell>
                      <TripStatusBadge status={t.status} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )
      )}

      <ListFooter data={data} onPage={(page) => list.update({ page: page > 1 ? String(page) : null })} />
    </div>
  )
}
