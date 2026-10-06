import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { SearchIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { BookingStatus } from '@/features/booking/api/bookings.api'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { toAppError } from '@/shared/api/problem'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ListFooter } from '../../components/ListFooter'
import { oneOf, useListParams } from '../../components/useListParams'
import {
  bookingStatusLabels,
  bookingTypeLabels,
  operationsApi,
  operationsKeys,
  type BookingListParams,
  type BookingType,
} from '../api/operations.api'

const columnCount = 7
const statuses = [1, 2, 3, 4, 5, 6] as const satisfies readonly BookingStatus[]
const types = [1, 2, 3] as const satisfies readonly BookingType[]

/** Admin → Bookings: find any booking (17-day plan, Day 12). ?q=…&status=2&type=1&page=2 */
export function AdminBookingsPage() {
  useDocumentMeta({ title: 'Bookings' })
  const list = useListParams()
  const params: BookingListParams = {
    search: list.search,
    status: oneOf(list.get('status'), statuses),
    type: oneOf(list.get('type'), types),
    tripFrom: list.get('from'),
    tripTo: list.get('to'),
    page: list.page,
  }

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: operationsKeys.bookings(params),
    queryFn: () => operationsApi.bookings(params),
    placeholderData: keepPreviousData,
  })
  const hasFilters = params.search !== '' || params.status !== null || params.type !== null || params.tripFrom !== null || params.tripTo !== null

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Bookings</h1>
        <p className="text-muted-foreground">Every booking, newest first. Open one to take a payment, cancel it or send its documents.</p>
      </div>

      <div className="flex flex-wrap gap-2">
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Booking no. (TB100001), name or mobile…"
            className="pl-8"
            aria-label="Search bookings"
          />
        </div>
        <Select value={params.status ? String(params.status) : 'all'} onValueChange={(v) => list.update({ status: v === 'all' ? null : v })}>
          <SelectTrigger className="w-48" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {statuses.map((s) => (
              <SelectItem key={s} value={String(s)}>
                {bookingStatusLabels[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={params.type ? String(params.type) : 'all'} onValueChange={(v) => list.update({ type: v === 'all' ? null : v })}>
          <SelectTrigger className="w-44" aria-label="Filter by type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All types</SelectItem>
            {types.map((t) => (
              <SelectItem key={t} value={String(t)}>
                {bookingTypeLabels[t]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          type="date"
          className="w-40"
          aria-label="Trips starting from"
          value={params.tripFrom ?? ''}
          onChange={(e) => list.update({ from: e.target.value || null })}
        />
        <Input
          type="date"
          className="w-40"
          aria-label="Trips starting until"
          value={params.tripTo ?? ''}
          onChange={(e) => list.update({ to: e.target.value || null })}
        />
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Booking</TableHead>
              <TableHead>Trip</TableHead>
              <TableHead>Contact</TableHead>
              <TableHead>Dates</TableHead>
              <TableHead className="text-right">People</TableHead>
              <TableHead className="text-right">Paid / total</TableHead>
              <TableHead>Status</TableHead>
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
                  {hasFilters ? 'No booking matches these filters.' : 'No bookings yet.'}
                  {hasFilters && (
                    <div>
                      <Button variant="link" onClick={list.clear}>
                        Show all
                      </Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            )}

            {data?.items.map((b) => (
              <TableRow key={b.bookingNo}>
                <TableCell>
                  <Link to={`/admin/bookings/${encodeURIComponent(b.bookingNo)}`} className="font-mono font-medium hover:underline">
                    {b.bookingNo}
                  </Link>
                  <div className="text-xs text-muted-foreground">{formatDateTime(b.bookedAtUtc)}</div>
                </TableCell>
                <TableCell>
                  {b.packageTitle ?? 'Custom trip'}
                  <div className="text-xs text-muted-foreground">{bookingTypeLabels[b.bookingType]}</div>
                </TableCell>
                <TableCell>
                  {b.contactName}
                  <div className="text-xs text-muted-foreground">{b.contactPhone}</div>
                </TableCell>
                <TableCell className="whitespace-nowrap">
                  {formatDate(b.startDate)}
                  <div className="text-xs text-muted-foreground">to {formatDate(b.endDate)}</div>
                </TableCell>
                <TableCell className="text-right tabular-nums">{b.travellers}</TableCell>
                <TableCell className="text-right whitespace-nowrap tabular-nums">
                  {formatTaka(b.paidAmount)}
                  <div className="text-xs text-muted-foreground">of {formatTaka(b.totalAmount)}</div>
                </TableCell>
                <TableCell>
                  <BookingStatusBadge status={b.status} />
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
