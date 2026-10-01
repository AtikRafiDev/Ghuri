import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BanIcon, PencilIcon, PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
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
    <Card>
      <CardHeader className="flex flex-wrap items-start justify-between gap-4">
        <div className="grid gap-1.5">
          <CardTitle>Departures</CardTitle>
          <CardDescription>
            Each date has its own prices and seats. The cheapest open date is the package's "from" price
            {pkg.priceFrom > 0 && ` - now ${formatTaka(pkg.priceFrom)}`}.
          </CardDescription>
        </div>
        <Button onClick={() => setForm({ open: true })}>
          <PlusIcon />
          Add departure
        </Button>
      </CardHeader>
      <CardContent>
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Dates</TableHead>
                <TableHead className="text-right">Adult / child / infant</TableHead>
                <TableHead>Seats</TableHead>
                <TableHead>Booking closes</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-20 text-right">
                  <span className="sr-only">Actions</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isPending && (
                <TableRow>
                  <TableCell colSpan={columnCount}>
                    <Skeleton className="h-10 w-full" />
                  </TableCell>
                </TableRow>
              )}

              {isError && (
                <TableRow>
                  <TableCell colSpan={columnCount} className="py-8 text-center">
                    <p className="text-destructive">{error.message}</p>
                    <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                      Try again
                    </Button>
                  </TableCell>
                </TableRow>
              )}

              {data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={columnCount} className="py-8 text-center text-muted-foreground">
                    No dates yet. A fixed package needs at least one open date before it can be published.
                  </TableCell>
                </TableRow>
              )}

              {data?.map((d) => {
                // Past dates and finished trips are history: shown, not changed.
                const editable = !d.isPast && d.status !== DepartureStatus.Cancelled && d.status !== DepartureStatus.Completed
                return (
                  <TableRow key={d.id} className={cn(d.isPast && 'text-muted-foreground')}>
                    <TableCell>
                      <div className="font-medium">{formatDate(d.startDate)}</div>
                      <div className="text-xs text-muted-foreground">to {formatDate(d.endDate)}</div>
                    </TableCell>
                    <TableCell className="text-right tabular-nums">
                      <div>{formatTaka(d.adultPrice)}</div>
                      <div className="text-xs text-muted-foreground">
                        {formatTaka(d.childPrice)} / {d.infantPrice === 0 ? 'free' : formatTaka(d.infantPrice)}
                      </div>
                    </TableCell>
                    <TableCell>
                      <SeatsBar left={d.seatsLeft} total={d.totalSeats} />
                    </TableCell>
                    <TableCell className="text-sm">
                      {d.bookingCutoffDays} {d.bookingCutoffDays === 1 ? 'day' : 'days'} before
                    </TableCell>
                    <TableCell>
                      {d.isPast ? (
                        <Badge variant="outline">Departed</Badge>
                      ) : (
                        <Badge variant={d.status === DepartureStatus.Open ? 'default' : 'secondary'}>
                          {departureStatusLabels[d.status]}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {editable && (
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="icon-sm" aria-label={`Edit ${formatDate(d.startDate)}`} onClick={() => setForm({ open: true, departure: d })}>
                            <PencilIcon />
                          </Button>
                          {d.status === DepartureStatus.Open && (
                            <Button
                              variant="ghost"
                              size="icon-sm"
                              aria-label={`Close ${formatDate(d.startDate)}`}
                              title="Close - stop new bookings"
                              onClick={() => setToClose({ open: true, departure: d })}
                            >
                              <BanIcon />
                            </Button>
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
      </CardContent>

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
        onConfirm={async () => {
          if (!toClose.departure) return
          await packagesApi.closeDeparture(toClose.departure.id)
          await queryClient.invalidateQueries({ queryKey: packageKeys.all })
        }}
      />
    </Card>
  )
}

/** "12 of 20 left" with a small bar - red when almost full. */
function SeatsBar({ left, total }: { left: number; total: number }) {
  const booked = total - left
  const percent = total > 0 ? Math.round((booked / total) * 100) : 0
  return (
    <div className="grid w-28 gap-1">
      <span className="text-sm tabular-nums">
        {left} of {total} left
      </span>
      <div className="h-1.5 overflow-hidden rounded-full bg-muted" aria-hidden>
        <div className={cn('h-full rounded-full', percent >= 80 ? 'bg-destructive' : 'bg-primary')} style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}
