import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BanIcon, CalendarPlusIcon, PencilIcon, PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { ConfirmDeleteDialog } from '../../components/ConfirmDeleteDialog'
import {
  DepartureStatus,
  departureStatusLabels,
  formatDate,
  formatTaka,
  packageKeys,
  packagesApi,
  type AdminDeparture,
  type AdminPackage,
} from '../api/packages.api'
import { DepartureFormDialog } from './DepartureFormDialog'

const columnCount = 6

/** Each status in its tone: open = green (selling), closed = amber (paused), cancelled = terracotta, completed = grey. */
const statusVariant = {
  [DepartureStatus.Open]: 'success',
  [DepartureStatus.Closed]: 'warning',
  [DepartureStatus.Cancelled]: 'danger',
  [DepartureStatus.Completed]: 'neutral',
} as const

/** Fixed packages only: the dated trips customers book, each with its own prices and seats. */
export function PackageDeparturesTab({ pkg }: { pkg: AdminPackage }) {
  const queryClient = useQueryClient()
  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: packageKeys.departures(pkg.id),
    queryFn: () => packagesApi.departures(pkg.id),
  })

  const [form, setForm] = useState<{ open: boolean; departure?: AdminDeparture }>({ open: false })
  const [toClose, setToClose] = useState<{ open: boolean; departure?: AdminDeparture }>({ open: false })

  return (
    <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div className="grid min-w-0 gap-0.5">
          <h2 className="text-base font-bold text-ink-900">Departures</h2>
          <p className="text-sm text-ink-500">
            Each date has its own prices and seats. The cheapest open date is the package's "from" price
            {pkg.priceFrom > 0 && (
              <>
                {' '}
                - now <span className="nums font-semibold text-ink-900">{formatTaka(pkg.priceFrom)}</span>
              </>
            )}
            .
          </p>
        </div>
        <Button onClick={() => setForm({ open: true })}>
          <PlusIcon />
          Add departure
        </Button>
      </header>

      {data?.length === 0 ? (
        <EmptyState
          icon={CalendarPlusIcon}
          title="No dates yet"
          text="A fixed package needs at least one open date before it can be published."
          className="py-10"
        >
          <Button variant="outline" onClick={() => setForm({ open: true })}>
            <PlusIcon />
            Add the first date
          </Button>
        </EmptyState>
      ) : (
        <div className="overflow-hidden rounded-2xl ring-1 ring-ink-200/80">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Dates</TableHead>
                <TableHead className="text-right">Adult / child / infant</TableHead>
                <TableHead>Seats</TableHead>
                <TableHead>Booking closes</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-24 text-right">
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isPending && <SkeletonRows />}

              {isError && (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={columnCount} className="py-10 text-center">
                    <p className="text-sm font-medium text-destructive">{toAppError(error).message}</p>
                    <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                      Try again
                    </Button>
                  </TableCell>
                </TableRow>
              )}

              {data?.map((d) => {
                // Past dates and finished trips are history: shown, not changed.
                const editable = !d.isPast && d.status !== DepartureStatus.Cancelled && d.status !== DepartureStatus.Completed
                return (
                  <TableRow key={d.id} className={cn(d.isPast && 'bg-ink-50/50 text-ink-500')}>
                    <TableCell>
                      <div className={cn('font-semibold', d.isPast ? 'text-ink-500' : 'text-ink-900')}>{formatDate(d.startDate)}</div>
                      <div className="text-xs text-ink-500">to {formatDate(d.endDate)}</div>
                    </TableCell>
                    <TableCell className="nums text-right">
                      <div className={cn('font-semibold', d.isPast ? 'text-ink-500' : 'text-ink-900')}>{formatTaka(d.adultPrice)}</div>
                      <div className="text-xs text-ink-500">
                        {formatTaka(d.childPrice)} / {d.infantPrice === 0 ? 'free' : formatTaka(d.infantPrice)}
                      </div>
                    </TableCell>
                    <TableCell>
                      <SeatsMeter left={d.seatsLeft} total={d.totalSeats} muted={d.isPast} />
                    </TableCell>
                    <TableCell className="text-ink-600">
                      <span className="nums">{d.bookingCutoffDays}</span> {d.bookingCutoffDays === 1 ? 'day' : 'days'} before
                    </TableCell>
                    <TableCell>
                      {d.isPast ? (
                        <Badge variant="neutral" dot>
                          Departed
                        </Badge>
                      ) : (
                        <Badge variant={statusVariant[d.status]} dot>
                          {departureStatusLabels[d.status]}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {editable && (
                        <div className="flex justify-end gap-1">
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="icon-sm"
                                aria-label={`Edit ${formatDate(d.startDate)}`}
                                className="text-ink-400 hover:bg-forest-50 hover:text-forest-700"
                                onClick={() => setForm({ open: true, departure: d })}
                              >
                                <PencilIcon />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>Edit</TooltipContent>
                          </Tooltip>
                          {d.status === DepartureStatus.Open && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  variant="ghost"
                                  size="icon-sm"
                                  aria-label={`Close ${formatDate(d.startDate)}`}
                                  className="text-ink-400 hover:bg-clay-50 hover:text-clay-600"
                                  onClick={() => setToClose({ open: true, departure: d })}
                                >
                                  <BanIcon />
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>Close - stop new bookings</TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <DepartureFormDialog
        open={form.open}
        onOpenChange={(open) => setForm((current) => ({ ...current, open }))}
        pkg={pkg}
        departure={form.departure}
      />

      <ConfirmDeleteDialog
        open={toClose.open}
        onOpenChange={(open) => setToClose((current) => ({ ...current, open }))}
        title={`Close ${toClose.departure ? formatDate(toClose.departure.startDate) : 'this departure'}?`}
        description="No new bookings will be taken for this date. Customers who already booked keep their seats. This can't be undone."
        confirmLabel="Close departure"
        successMessage="Departure closed"
        onConfirm={async () => {
          if (!toClose.departure) return
          await packagesApi.closeDeparture(toClose.departure.id)
          await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        }}
      />
    </section>
  )
}

/**
 * "12 of 20 left" over a small meter of the seats already booked - green,
 * turning amber once 80% are gone, so nearly-full dates stand out.
 */
function SeatsMeter({ left, total, muted }: { left: number; total: number; muted: boolean }) {
  const booked = total - left
  const percent = total > 0 ? Math.round((booked / total) * 100) : 0
  const nearlyFull = percent >= 80
  return (
    <div className="grid w-32 gap-1.5">
      <span className={cn('nums text-xs font-semibold', muted ? 'text-ink-500' : nearlyFull ? 'text-sun-700' : 'text-ink-900')}>
        {left} of {total} left
      </span>
      <div className="h-1.5 overflow-hidden rounded-full bg-ink-100" aria-hidden>
        <div
          className={cn('h-full rounded-full transition-[width] duration-700 ease-(--ease-out-expo)', muted ? 'bg-ink-300' : nearlyFull ? 'bg-sun-500' : 'bg-forest-500')}
          style={{ width: `${percent}%` }}
        />
      </div>
    </div>
  )
}

/** While the dates load: three rows the shape of real ones. */
function SkeletonRows() {
  return Array.from({ length: 3 }, (_, i) => (
    <TableRow key={i} className="hover:bg-transparent" aria-hidden>
      <TableCell>
        <div className="grid gap-1.5">
          <Skeleton className="h-4 w-28" />
          <Skeleton className="h-3 w-24" />
        </div>
      </TableCell>
      <TableCell>
        <div className="grid justify-items-end gap-1.5">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-3 w-24" />
        </div>
      </TableCell>
      <TableCell>
        <div className="grid w-32 gap-1.5">
          <Skeleton className="h-3 w-full" />
          <Skeleton className="h-1.5 w-full rounded-full" />
        </div>
      </TableCell>
      <TableCell>
        <Skeleton className="h-4 w-24" />
      </TableCell>
      <TableCell>
        <Skeleton className="h-6 w-16 rounded-full" />
      </TableCell>
      <TableCell>
        <Skeleton className="ml-auto h-9 w-20 rounded-xl" />
      </TableCell>
    </TableRow>
  ))
}
