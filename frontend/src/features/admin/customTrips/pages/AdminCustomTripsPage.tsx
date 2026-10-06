import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { SearchIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { tripStatusLabels, type TripStatus } from '@/features/trips/api/trips.api'
import { TripStatusBadge } from '@/features/trips/components/TripStatusBadge'
import { toAppError } from '@/shared/api/problem'
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

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Custom trips</h1>
        <p className="text-muted-foreground">Requests customers designed themselves. Target: a quote within 24 hours.</p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Tabs
          value={params.status === null ? 'all' : String(params.status)}
          onValueChange={(v) => list.update({ status: v === '1' ? null : v })}
        >
          <TabsList>
            <TabsTrigger value="1">Waiting</TabsTrigger>
            <TabsTrigger value="2">Quoted</TabsTrigger>
            <TabsTrigger value="3">Accepted</TabsTrigger>
            <TabsTrigger value="4">Paid</TabsTrigger>
            <TabsTrigger value="all">All</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Trip no. (CT1001), name or mobile…"
            className="pl-8"
            aria-label="Search custom trips"
          />
        </div>
      </div>

      <div className="rounded-lg border">
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
          <TableBody className={isFetching && !isPending ? 'opacity-60 transition-opacity' : undefined}>
            {isPending &&
              Array.from({ length: 4 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={columnCount}>
                    <Skeleton className="h-10 w-full" />
                  </TableCell>
                </TableRow>
              ))}
            {isError && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center">
                  <p className="text-destructive">{toAppError(error).message}</p>
                  <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                    Try again
                  </Button>
                </TableCell>
              </TableRow>
            )}
            {data?.items.length === 0 && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center text-muted-foreground">
                  {params.status === 1 ? 'No requests waiting - all quoted.' : `No ${params.status ? tripStatusLabels[params.status].toLowerCase() : ''} requests.`}
                </TableCell>
              </TableRow>
            )}
            {data?.items.map((t) => (
              <TableRow key={t.tripNo}>
                <TableCell>
                  <Link to={`/admin/custom-trips/${encodeURIComponent(t.tripNo)}`} className="font-mono font-medium hover:underline">
                    {t.tripNo}
                  </Link>
                  <div className="text-xs text-muted-foreground">{formatDateTime(t.submittedAtUtc)}</div>
                </TableCell>
                <TableCell className="max-w-64 whitespace-normal">{t.route}</TableCell>
                <TableCell>
                  {t.contactName}
                  <div className="text-xs text-muted-foreground">
                    {t.contactPhone} · {t.people} people
                  </div>
                </TableCell>
                <TableCell className="whitespace-nowrap">
                  {formatDate(t.startDate)}
                  <div className="text-xs text-muted-foreground">{t.totalNights} nights</div>
                </TableCell>
                <TableCell className="text-right tabular-nums">
                  {t.quoteTotal !== null ? formatTaka(t.quoteTotal) : '-'}
                  {t.quoteExpiresAtUtc && t.status === 2 && (
                    <div className="text-xs text-muted-foreground">until {formatDateTime(t.quoteExpiresAtUtc)}</div>
                  )}
                </TableCell>
                <TableCell>
                  <TripStatusBadge status={t.status} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <ListFooter data={data} onPage={(page) => list.update({ page: page > 1 ? String(page) : null })} />
    </div>
  )
}
