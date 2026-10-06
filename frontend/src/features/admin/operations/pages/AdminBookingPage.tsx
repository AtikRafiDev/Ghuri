import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { toAppError } from '@/shared/api/problem'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import {
  bookingStatusLabels,
  bookingTypeLabels,
  operationsApi,
  operationsKeys,
  paymentProviderLabels,
  paymentStatusLabels,
  refundStatusLabels,
  type AdminBooking,
} from '../api/operations.api'
import { BookingActions } from '../components/BookingActions'

/** Admin → Bookings → one booking: everything about it, and what can be done (17-day plan, Day 12). */
export function AdminBookingPage() {
  const { bookingNo = '' } = useParams()
  useDocumentMeta({ title: `Booking ${bookingNo}` })
  const booking = useQuery({ queryKey: operationsKeys.booking(bookingNo), queryFn: () => operationsApi.booking(bookingNo) })

  if (booking.isPending) return <PageSpinner />
  if (booking.isError) {
    const error = toAppError(booking.error)
    return (
      <div className="grid justify-items-start gap-3">
        <p className="text-destructive">{error.status === 404 ? `Booking ${bookingNo} doesn't exist.` : error.message}</p>
        <Button asChild variant="outline">
          <Link to="/admin/bookings">All bookings</Link>
        </Button>
      </div>
    )
  }

  return <Details booking={booking.data} />
}

function Details({ booking: b }: { booking: AdminBooking }) {
  return (
    <div className="grid gap-6">
      <Link to="/admin/bookings" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-4" /> All bookings
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="grid gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="font-mono text-2xl font-semibold">{b.bookingNo}</h1>
            <BookingStatusBadge status={b.status} />
          </div>
          <p className="text-muted-foreground">
            {b.packageTitle ?? 'Custom trip'} · {bookingTypeLabels[b.bookingType]} · booked {formatDateTime(b.bookedAtUtc)}
          </p>
        </div>
      </div>

      <BookingActions booking={b} />

      <div className="grid gap-4 md:grid-cols-2">
        <Section title="Trip">
          <Facts
            rows={[
              ['Dates', `${formatDate(b.startDate)} → ${formatDate(b.endDate)}`],
              ['Nights', b.nights === 0 ? 'Day trip' : String(b.nights)],
              ['People', `${b.adults} adult(s), ${b.children} child(ren), ${b.infants} infant(s)`],
              ['Request', b.specialRequest ?? '-'],
              ...(b.cancelReason ? [['Cancelled', `${b.cancelReason}${b.cancelledAtUtc ? ` (${formatDateTime(b.cancelledAtUtc)})` : ''}`] as [string, string]] : []),
            ]}
          />
        </Section>
        <Section title="Money">
          <Facts
            rows={[
              ['Total', formatTaka(b.totalAmount)],
              ['Paid', formatTaka(b.paidAmount)],
              ['Still due', formatTaka(b.amountDue)],
              ...(b.holdExpiresAtUtc ? [['Seats held until', formatDateTime(b.holdExpiresAtUtc)] as [string, string]] : []),
            ]}
          />
        </Section>
        <Section title="Contact">
          <Facts
            rows={[
              ['Name', b.contactName],
              ['Mobile', b.contactPhone],
              ['Email', b.contactEmail ?? '-'],
            ]}
          />
        </Section>
        <Section title="Account">
          <Facts
            rows={[
              ['Name', b.customer.fullName],
              ['Mobile', b.customer.phone],
              ['Email', b.customer.email ?? '-'],
            ]}
          />
        </Section>
      </div>

      <Section title="Travellers">
        <ul className="grid gap-1 text-sm">
          {b.travellers.map((t, i) => (
            <li key={i} className="flex justify-between gap-2">
              <span>
                {t.fullName}
                {t.isLead && <span className="text-muted-foreground"> · lead</span>}
              </span>
              <span className="text-muted-foreground">{t.type === 1 ? 'Adult' : t.type === 2 ? 'Child' : 'Infant'}</span>
            </li>
          ))}
        </ul>
      </Section>

      <Section title="Payments">
        {b.payments.length === 0 ? (
          <p className="text-sm text-muted-foreground">No payment attempts yet.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Payment</TableHead>
                <TableHead>How</TableHead>
                <TableHead>When</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {b.payments.map((p) => (
                <TableRow key={p.paymentNo}>
                  <TableCell className="font-mono">{p.paymentNo}</TableCell>
                  <TableCell>
                    {paymentProviderLabels[p.provider]}
                    {p.method && <div className="text-xs text-muted-foreground">{p.method}</div>}
                    {p.reference && <div className="font-mono text-xs text-muted-foreground">{p.reference}</div>}
                  </TableCell>
                  <TableCell className="whitespace-nowrap">{formatDateTime(p.paidAtUtc ?? p.initiatedAtUtc)}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatTaka(p.amount)}</TableCell>
                  <TableCell>
                    <Badge variant={p.status === 3 ? 'default' : 'outline'}>{paymentStatusLabels[p.status]}</Badge>
                    {p.failureReason && <div className="text-xs text-muted-foreground">{p.failureReason}</div>}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Section>

      {b.refunds.length > 0 && (
        <Section title="Refunds">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Refund</TableHead>
                <TableHead>Why</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {b.refunds.map((r) => (
                <TableRow key={r.refundNo}>
                  <TableCell>
                    <span className="font-mono">{r.refundNo}</span>
                    <div className="text-xs text-muted-foreground">
                      {r.requestedByName ?? 'System'} · {formatDateTime(r.requestedAtUtc)}
                    </div>
                  </TableCell>
                  <TableCell className="max-w-72 text-sm whitespace-normal">{r.reason}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {formatTaka(r.amount)}
                    <div className="text-xs text-muted-foreground">{r.refundPercent}%</div>
                  </TableCell>
                  <TableCell>
                    <Badge variant={r.status === 1 ? 'destructive' : 'outline'}>{refundStatusLabels[r.status]}</Badge>
                    {r.reference && <div className="font-mono text-xs text-muted-foreground">{r.reference}</div>}
                    {r.rejectReason && <div className="text-xs text-muted-foreground">{r.rejectReason}</div>}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          {b.refunds.some((r) => r.status === 1) && (
            <Link to={`/admin/refunds?q=${encodeURIComponent(b.bookingNo)}`} className="text-sm font-medium underline underline-offset-4">
              Process in Refunds
            </Link>
          )}
        </Section>
      )}

      <Section title="History">
        <ol className="grid gap-2 text-sm">
          {b.history.map((h, i) => (
            <li key={i} className="grid gap-0.5">
              <span>
                {h.from ? `${bookingStatusLabels[h.from]} → ` : ''}
                <strong>{bookingStatusLabels[h.to]}</strong>
                <span className="text-muted-foreground"> · {formatDateTime(h.changedAtUtc)} · {h.changedByName ?? 'System'}</span>
              </span>
              {h.note && <span className="text-muted-foreground">{h.note}</span>}
            </li>
          ))}
        </ol>
      </Section>
    </div>
  )
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="grid content-start gap-3 rounded-xl border bg-card p-4">
      <h2 className="font-semibold">{title}</h2>
      {children}
    </section>
  )
}

function Facts({ rows }: { rows: [string, string][] }) {
  return (
    <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-1 text-sm">
      {rows.map(([label, value]) => (
        <div key={label} className="contents">
          <dt className="text-muted-foreground">{label}</dt>
          <dd className="whitespace-pre-line">{value}</dd>
        </div>
      ))}
    </dl>
  )
}
