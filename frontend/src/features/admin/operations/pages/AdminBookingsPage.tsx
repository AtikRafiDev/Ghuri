import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CalendarCheckIcon, CalendarRangeIcon, SearchIcon, SearchXIcon, XIcon } from 'lucide-react'
import type { MouseEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { fieldClasses, Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { BookingStatus } from '@/features/booking/api/bookings.api'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
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

// The two date boxes inside the "Trip starts" field: no border of their own (the
// field around them has it), grey "mm/dd/yyyy" until a date is picked.
const dateBox =
  'h-full min-w-0 flex-1 bg-transparent px-1.5 text-sm text-ink-900 outline-none data-empty:text-ink-400 sm:w-[8.25rem] sm:flex-none [&::-webkit-calendar-picker-indicator]:cursor-pointer [&::-webkit-calendar-picker-indicator]:opacity-50 hover:[&::-webkit-calendar-picker-indicator]:opacity-100'

/** Admin → Bookings: find any booking (17-day plan, Day 12). ?q=…&status=2&type=1&page=2 */
export function AdminBookingsPage() {
  useDocumentMeta({ title: 'Bookings' })
  const navigate = useNavigate()
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

  // A click anywhere on a row opens the booking - except on the link itself,
  // or when the click ended a text selection (copying a booking number).
  const openRow = (bookingNo: string) => (event: MouseEvent) => {
    if ((event.target as HTMLElement).closest('a, button') || window.getSelection()?.toString()) return
    navigate(`/admin/bookings/${encodeURIComponent(bookingNo)}`)
  }

  return (
    <div className="grid gap-6">
      <PageHeader title="Bookings" description="Every booking, newest first. Open one to take a payment, cancel it or send its documents." />

      {/* Filters: every control is h-10, so they line up edge to edge and wrap cleanly on a phone. */}
      <div className="flex min-w-0 animate-fade-up flex-wrap items-center gap-3">
        <div className="relative w-full sm:w-auto sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Booking no., name or mobile…"
            className="pl-10"
            aria-label="Search bookings"
          />
        </div>
        <Select value={params.status ? String(params.status) : 'all'} onValueChange={(v) => list.update({ status: v === 'all' ? null : v })}>
          <SelectTrigger className="min-w-0 flex-1 sm:w-44 sm:flex-none" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
            <SelectItem value="all">All statuses</SelectItem>
            {statuses.map((s) => (
              <SelectItem key={s} value={String(s)}>
                {bookingStatusLabels[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={params.type ? String(params.type) : 'all'} onValueChange={(v) => list.update({ type: v === 'all' ? null : v })}>
          <SelectTrigger className="min-w-0 flex-1 sm:w-40 sm:flex-none" aria-label="Filter by type">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
            <SelectItem value="all">All types</SelectItem>
            {types.map((t) => (
              <SelectItem key={t} value={String(t)}>
                {bookingTypeLabels[t]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {/* One field holding both ends of the range, so "from" and "until" read as a pair. */}
        <div
          role="group"
          aria-label="Trip start date range"
          className={cn(fieldClasses, 'flex h-10 items-center pr-1.5 pl-3.5 focus-within:border-forest-500 focus-within:ring-4 focus-within:ring-forest-500/15 sm:w-auto')}
        >
          <CalendarRangeIcon className="size-4 shrink-0 text-ink-400" />
          <span className="ml-2 hidden text-sm whitespace-nowrap text-ink-500 sm:inline">Trip starts</span>
          <input
            type="date"
            className={cn(dateBox, 'ml-1')}
            aria-label="Trips starting from"
            data-empty={!params.tripFrom || undefined}
            value={params.tripFrom ?? ''}
            onChange={(e) => list.update({ from: e.target.value || null })}
          />
          <span aria-hidden className="text-ink-300">
            –
          </span>
          <input
            type="date"
            className={dateBox}
            aria-label="Trips starting until"
            data-empty={!params.tripTo || undefined}
            value={params.tripTo ?? ''}
            onChange={(e) => list.update({ to: e.target.value || null })}
          />
        </div>
        {hasFilters && (
          <Button variant="ghost" onClick={list.clear} className="text-ink-500">
            <XIcon />
            Clear
          </Button>
        )}
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
        hasFilters ? (
          <EmptyState icon={SearchXIcon} title="No booking matches these filters" text="Try another name or number, or widen the dates.">
            <Button variant="outline" onClick={list.clear}>
              Show all bookings
            </Button>
          </EmptyState>
        ) : (
          <EmptyState icon={CalendarCheckIcon} title="No bookings yet" text="Bookings made on the website, and custom trips customers accept, show up here." />
        )
      ) : (
        (isPending || data) && (
          <div className="animate-fade-up overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
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
              <TableBody aria-busy={isPending || undefined} className={cn('transition-opacity duration-300', isFetching && !isPending && 'opacity-60')}>
                {/* First load: rows of shimmer in the table's own shape, so nothing jumps when the bookings arrive. */}
                {isPending &&
                  Array.from({ length: 6 }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      {Array.from({ length: columnCount }, (_, j) => (
                        <TableCell key={j}>
                          <Skeleton className={cn('h-4', j === 4 || j === 5 ? 'ml-auto w-14' : 'w-28')} />
                          <Skeleton className={cn('mt-2 h-3', j === 4 || j === 5 ? 'ml-auto w-10' : 'w-20')} />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {data?.items.map((b) => (
                  <TableRow key={b.bookingNo} onClick={openRow(b.bookingNo)} className="cursor-pointer">
                    <TableCell>
                      <Link
                        to={`/admin/bookings/${encodeURIComponent(b.bookingNo)}`}
                        className="font-mono text-[0.8125rem] font-semibold text-ink-900 underline-offset-4 transition-colors hover:text-forest-700 hover:underline"
                      >
                        {b.bookingNo}
                      </Link>
                      <div className="text-xs text-ink-500">{formatDateTime(b.bookedAtUtc)}</div>
                    </TableCell>
                    <TableCell>
                      <div className="font-medium text-ink-900">{b.packageTitle ?? 'Custom trip'}</div>
                      <div className="text-xs text-ink-500">{bookingTypeLabels[b.bookingType]}</div>
                    </TableCell>
                    <TableCell>
                      <div className="font-medium text-ink-900">{b.contactName}</div>
                      <div className="nums text-xs text-ink-500">{b.contactPhone}</div>
                    </TableCell>
                    <TableCell>
                      <div className="text-ink-900">{formatDate(b.startDate)}</div>
                      <div className="text-xs text-ink-500">to {formatDate(b.endDate)}</div>
                    </TableCell>
                    <TableCell className="nums text-right font-medium text-ink-900">{b.travellers}</TableCell>
                    <TableCell className="nums text-right">
                      <div className={cn('font-semibold', b.paidAmount === 0 ? 'text-ink-400' : 'text-ink-900')}>{formatTaka(b.paidAmount)}</div>
                      <div className="text-xs text-ink-500">of {formatTaka(b.totalAmount)}</div>
                    </TableCell>
                    <TableCell>
                      <BookingStatusBadge status={b.status} />
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
