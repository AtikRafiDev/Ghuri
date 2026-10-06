import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon, ArrowRightIcon, HourglassIcon, ReceiptTextIcon, SearchXIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { BookingStatus } from '@/features/booking/api/bookings.api'
import { BookingStatusBadge } from '@/features/booking/components/BookingStatusBadge'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { formatDate, formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { bookingStatusLabels, bookingTypeLabels, operationsApi, operationsKeys, paymentProviderLabels, type AdminBooking } from '../api/operations.api'
import { BookingActions } from '../components/BookingActions'
import { PaymentStatusBadge, RefundStatusBadge } from '../components/MoneyStatusBadges'

const travellerTypes = { 1: 'Adult', 2: 'Child', 3: 'Infant' } as const

// The colour of each step on the status timeline - the same meaning as the status pills.
const stepTones: Record<BookingStatus, string> = {
  1: 'bg-sun-500 ring-sun-100',
  2: 'bg-forest-500 ring-forest-100',
  3: 'bg-forest-400 ring-forest-100',
  4: 'bg-ink-400 ring-ink-100',
  5: 'bg-clay-500 ring-clay-100',
  6: 'bg-ink-300 ring-ink-100',
}

/** "2 adults · 1 child" - only the kinds of traveller that are there. */
function describePeople(adults: number, children: number, infants: number): string {
  const part = (n: number, one: string, many: string) => (n > 0 ? `${n} ${n === 1 ? one : many}` : null)
  return [part(adults, 'adult', 'adults'), part(children, 'child', 'children'), part(infants, 'infant', 'infants')].filter(Boolean).join(' · ')
}

const backLink = (
  <Button asChild variant="ghost" size="sm" className="-ml-3 text-ink-500">
    <Link to="/admin/bookings">
      <ArrowLeftIcon className="group-hover/button:-translate-x-0.5" />
      All bookings
    </Link>
  </Button>
)

/** Admin → Bookings → one booking: everything about it, and what can be done (17-day plan, Day 12). */
export function AdminBookingPage() {
  const { bookingNo = '' } = useParams()
  useDocumentMeta({ title: `Booking ${bookingNo}` })
  const booking = useQuery({ queryKey: operationsKeys.booking(bookingNo), queryFn: () => operationsApi.booking(bookingNo) })

  if (booking.isPending) return <PageSpinner />
  if (booking.isError) {
    const error = toAppError(booking.error)
    return (
      <div className="grid gap-6">
        <PageHeader eyebrow={backLink} title={<span className="font-mono">{bookingNo}</span>} />
        {error.status === 404 ? (
          <EmptyState icon={SearchXIcon} title={`Booking ${bookingNo} doesn't exist`} text="Check the number, or find the booking in the list.">
            <Button asChild variant="outline">
              <Link to="/admin/bookings">All bookings</Link>
            </Button>
          </EmptyState>
        ) : (
          <div className="grid justify-items-start gap-3">
            <FormAlert kind="error">{error.message}</FormAlert>
            <Button variant="outline" size="sm" onClick={() => booking.refetch()}>
              Try again
            </Button>
          </div>
        )}
      </div>
    )
  }

  return <Details booking={booking.data} />
}

function Details({ booking: b }: { booking: AdminBooking }) {
  const paidPercent = b.totalAmount > 0 ? Math.min(100, Math.round((b.paidAmount / b.totalAmount) * 100)) : 0
  const lastStep = b.history.length - 1

  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow={backLink}
        title={<span className="font-mono tracking-tight">{b.bookingNo}</span>}
        titleAside={<BookingStatusBadge status={b.status} />}
        description={
          <>
            {b.packageTitle ?? 'Custom trip'} · {bookingTypeLabels[b.bookingType]} · booked {formatDateTime(b.bookedAtUtc)}
          </>
        }
        actions={<BookingActions booking={b} />}
      />

      {/* A state that must stay in view, not a pop-up: this booking is over. */}
      {b.cancelReason && (
        <FormAlert kind="error">
          Cancelled{b.cancelledAtUtc ? ` ${formatDateTime(b.cancelledAtUtc)}` : ''}: {b.cancelReason}
        </FormAlert>
      )}

      <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,23rem)]">
        {/* Main column: the trip, who's going, and the money's paper trail. On a phone both columns
            dissolve (display: contents) into one list, re-ordered so the money comes right after the trip. */}
        <div className="stagger contents lg:grid lg:min-w-0 lg:gap-5">
          <Panel className="order-1" title="Trip" subtitle={`${b.packageTitle ?? 'Custom trip'} · ${bookingTypeLabels[b.bookingType]}`}>
            <Facts>
              <Fact label="Dates">
                <span className="flex flex-wrap items-center gap-x-2">
                  {formatDate(b.startDate)}
                  <ArrowRightIcon aria-hidden className="size-3.5 text-ink-400" />
                  <span className="sr-only">to</span>
                  {formatDate(b.endDate)}
                </span>
              </Fact>
              <Fact label="Nights">{b.nights === 0 ? 'Day trip' : b.nights}</Fact>
              <Fact label="People">{describePeople(b.adults, b.children, b.infants)}</Fact>
              <Fact label="Request">
                {b.specialRequest ? <span className="whitespace-pre-line">{b.specialRequest}</span> : <span className="font-normal text-ink-400">None</span>}
              </Fact>
            </Facts>
          </Panel>

          <Panel className="order-3" title="Travellers" subtitle={`${b.travellers.length} ${b.travellers.length === 1 ? 'person' : 'people'} on this booking`}>
            <ul className="-my-2 divide-y divide-ink-100">
              {b.travellers.map((t, i) => (
                <li key={i} className="flex items-center gap-3 py-2.5">
                  <UserAvatar name={t.fullName} size="sm" />
                  <span className="grid min-w-0 flex-1">
                    <span className="truncate text-sm font-medium text-ink-900">{t.fullName}</span>
                    {t.phone && <span className="nums text-xs text-ink-500">{t.phone}</span>}
                  </span>
                  {t.isLead && <Badge variant="success">Lead</Badge>}
                  <span className="w-12 text-right text-xs font-medium text-ink-500">{travellerTypes[t.type]}</span>
                </li>
              ))}
            </ul>
          </Panel>

          <Panel className="order-5" title="Payments" subtitle="Every attempt, online and at the office" flush>
            {b.payments.length === 0 ? (
              <p className="flex items-center gap-2 border-t border-ink-100 px-5 py-6 text-sm text-ink-500 sm:px-6">
                <ReceiptTextIcon className="size-4 text-ink-400" />
                No payment attempts yet.
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="sm:pl-6">Payment</TableHead>
                    <TableHead>How</TableHead>
                    <TableHead className="text-right">Amount</TableHead>
                    <TableHead className="sm:pr-6">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {b.payments.map((p) => (
                    <TableRow key={p.paymentNo}>
                      <TableCell className="sm:pl-6">
                        <div className="font-mono text-[0.8125rem] font-semibold text-ink-900">{p.paymentNo}</div>
                        <div className="text-xs text-ink-500">{formatDateTime(p.paidAtUtc ?? p.initiatedAtUtc)}</div>
                      </TableCell>
                      <TableCell>
                        <div className="font-medium text-ink-900">{paymentProviderLabels[p.provider]}</div>
                        {(p.method || p.reference) && (
                          <div className="text-xs text-ink-500">
                            {p.method}
                            {p.method && p.reference && ' · '}
                            {p.reference && <span className="font-mono">{p.reference}</span>}
                          </div>
                        )}
                      </TableCell>
                      <TableCell className="nums text-right font-semibold text-ink-900">{formatTaka(p.amount)}</TableCell>
                      <TableCell className="sm:pr-6">
                        <PaymentStatusBadge status={p.status} />
                        {p.failureReason && <div className="mt-1 text-xs text-clay-600">{p.failureReason}</div>}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Panel>

          {b.refunds.length > 0 && (
            <Panel
              className="order-6"
              title="Refunds"
              subtitle="Money owed back on this booking"
              flush
              aside={
                b.refunds.some((r) => r.status === 1) && (
                  <Button asChild variant="outline" size="sm">
                    <Link to={`/admin/refunds?q=${encodeURIComponent(b.bookingNo)}`}>
                      Process in Refunds
                      <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
                    </Link>
                  </Button>
                )
              }
            >
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="sm:pl-6">Refund</TableHead>
                    <TableHead>Why</TableHead>
                    <TableHead className="text-right">Amount</TableHead>
                    <TableHead className="sm:pr-6">Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {b.refunds.map((r) => (
                    <TableRow key={r.refundNo}>
                      <TableCell className="sm:pl-6">
                        <div className="font-mono text-[0.8125rem] font-semibold text-ink-900">{r.refundNo}</div>
                        <div className="text-xs text-ink-500">
                          {r.requestedByName ?? 'System'} · {formatDateTime(r.requestedAtUtc)}
                        </div>
                      </TableCell>
                      <TableCell className="max-w-64 whitespace-normal text-ink-900">{r.reason}</TableCell>
                      <TableCell className="nums text-right">
                        <div className="font-semibold text-ink-900">{formatTaka(r.amount)}</div>
                        <div className="text-xs text-ink-500">{r.refundPercent}%</div>
                      </TableCell>
                      <TableCell className="sm:pr-6">
                        <RefundStatusBadge status={r.status} />
                        {r.reference && <div className="mt-1 font-mono text-xs text-ink-500">{r.reference}</div>}
                        {r.rejectReason && <div className="mt-1 max-w-48 text-xs whitespace-normal text-ink-500">{r.rejectReason}</div>}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Panel>
          )}
        </div>

        {/* Side column: the money at a glance, who to call, and how the booking got here. */}
        <div className="stagger contents lg:grid lg:min-w-0 lg:gap-5">
          <Panel className="order-2" title="Money" subtitle={b.paidAmount >= b.totalAmount ? 'Paid in full' : `${paidPercent}% paid so far`}>
            <div
              role="progressbar"
              aria-label="Paid so far"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={paidPercent}
              className="h-2 overflow-hidden rounded-full bg-ink-100"
            >
              <div
                className="h-full origin-left animate-[grow-x_0.9s_var(--ease-out-expo)_both] rounded-full bg-gradient-to-r from-forest-400 to-forest-600"
                style={{ width: `${paidPercent}%` }}
              />
            </div>
            <dl className="grid gap-2.5 text-sm">
              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-ink-500">Total</dt>
                <dd className="nums font-semibold text-ink-900">{formatTaka(b.totalAmount)}</dd>
              </div>
              <div className="flex items-baseline justify-between gap-6">
                <dt className="text-ink-500">Paid</dt>
                <dd className="nums font-semibold text-forest-700">{formatTaka(b.paidAmount)}</dd>
              </div>
              <div className="flex items-baseline justify-between gap-6 border-t border-ink-100 pt-2.5">
                <dt className="font-semibold text-ink-900">Still due</dt>
                <dd className={cn('nums text-base font-bold', b.amountDue > 0 ? 'text-clay-600' : 'text-ink-900')}>{formatTaka(b.amountDue)}</dd>
              </div>
            </dl>
            {b.holdExpiresAtUtc && (
              <p className="flex items-center gap-2 rounded-xl bg-sun-50 px-3 py-2.5 text-sm font-medium text-sun-700 ring-1 ring-sun-100 ring-inset">
                <HourglassIcon className="size-4 shrink-0" />
                Seats held until {formatDateTime(b.holdExpiresAtUtc)}
              </p>
            )}
          </Panel>

          <Panel className="order-4" title="Customer">
            <div className="flex items-center gap-3">
              <UserAvatar name={b.contactName} size="lg" />
              <span className="grid min-w-0">
                <span className="truncate font-semibold text-ink-900">{b.contactName}</span>
                <span className="text-xs text-ink-500">Contact for this trip</span>
              </span>
            </div>
            <Facts>
              <Fact label="Mobile">
                <a href={`tel:${b.contactPhone}`} className="nums hover:text-forest-700 hover:underline">
                  {b.contactPhone}
                </a>
              </Fact>
              <Fact label="Email">
                {b.contactEmail ? (
                  <a href={`mailto:${b.contactEmail}`} className="break-all hover:text-forest-700 hover:underline">
                    {b.contactEmail}
                  </a>
                ) : (
                  <span className="font-normal text-ink-400">-</span>
                )}
              </Fact>
            </Facts>
            <div className="grid gap-3 border-t border-ink-100 pt-4">
              <p className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Account</p>
              <Facts>
                <Fact label="Name">{b.customer.fullName}</Fact>
                <Fact label="Mobile">
                  <span className="nums">{b.customer.phone}</span>
                </Fact>
                <Fact label="Email">{b.customer.email ? <span className="break-all">{b.customer.email}</span> : <span className="font-normal text-ink-400">-</span>}</Fact>
              </Facts>
            </div>
          </Panel>

          <Panel className="order-7" title="Status history" subtitle="Oldest first">
            <ol className="grid gap-5">
              {b.history.map((h, i) => (
                <li key={i} className="relative grid grid-cols-[auto_minmax(0,1fr)] gap-x-3.5">
                  {/* The dot, and the line down to the next step. */}
                  <span aria-hidden className="relative flex w-3 justify-center self-stretch">
                    <span className={cn('mt-1 size-3 shrink-0 rounded-full ring-4', stepTones[h.to], i === lastStep && 'ring-[5px]')} />
                    {i < lastStep && <span className="absolute top-5 -bottom-4 w-px bg-ink-200" />}
                  </span>
                  <div className="grid gap-0.5 text-sm">
                    <span className="text-ink-900">
                      <strong className="font-semibold">{bookingStatusLabels[h.to]}</strong>
                      {h.from && <span className="text-ink-500"> from {bookingStatusLabels[h.from].toLowerCase()}</span>}
                    </span>
                    <span className="text-xs text-ink-500">
                      {formatDateTime(h.changedAtUtc)} · {h.changedByName ?? 'System'}
                    </span>
                    {h.note && <span className="mt-1 rounded-lg bg-ink-50 px-2.5 py-1.5 text-xs text-ink-600 ring-1 ring-ink-100 ring-inset">{h.note}</span>}
                  </div>
                </li>
              ))}
            </ol>
          </Panel>
        </div>
      </div>
    </div>
  )
}

/**
 * One white panel on the page: title + subtitle on the left, an optional
 * button on the right. flush = no inner padding below the header, so a
 * table can run edge to edge. className carries its phone order (order-n),
 * which the wide two-column layout ignores.
 */
function Panel({
  title,
  subtitle,
  aside,
  flush,
  className,
  children,
}: {
  title: string
  subtitle?: string
  aside?: ReactNode
  flush?: boolean
  className?: string
  children: ReactNode
}) {
  return (
    <section
      className={cn('grid min-w-0 content-start rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80 lg:order-none', flush ? 'overflow-hidden' : 'gap-4 p-5 sm:p-6', className)}
    >
      <header className={cn('flex items-center justify-between gap-4', flush && 'px-5 py-5 sm:px-6')}>
        <div className="grid min-w-0 gap-0.5">
          <h2 className="text-base font-bold text-ink-900">{title}</h2>
          {subtitle && <p className="text-xs text-ink-500">{subtitle}</p>}
        </div>
        {aside && <div className="shrink-0">{aside}</div>}
      </header>
      {children}
    </section>
  )
}

/** Label / value pairs whose values line up in one clean column. */
function Facts({ children }: { children: ReactNode }) {
  return <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-6 gap-y-2.5 text-sm">{children}</dl>
}

function Fact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="text-ink-500">{label}</dt>
      <dd className="font-medium text-ink-900">{children}</dd>
    </>
  )
}
