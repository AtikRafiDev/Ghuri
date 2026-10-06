import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CreditCardIcon, SearchIcon, SearchXIcon, XIcon } from 'lucide-react'
import type { MouseEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ListFooter } from '../../components/ListFooter'
import { oneOf, useListParams } from '../../components/useListParams'
import {
  operationsApi,
  operationsKeys,
  paymentProviderLabels,
  paymentStatusLabels,
  type PaymentListParams,
  type PaymentProvider,
  type PaymentStatus,
} from '../api/operations.api'
import { PaymentStatusBadge } from '../components/MoneyStatusBadges'

const columnCount = 6
const statuses = [1, 2, 3, 4, 5, 6, 7] as const satisfies readonly PaymentStatus[]
const providers = [1, 3] as const satisfies readonly PaymentProvider[] // Stripe is Phase 2

/** Admin → Payments: every attempt, online and at the office (17-day plan, Day 12). */
export function AdminPaymentsPage() {
  useDocumentMeta({ title: 'Payments' })
  const navigate = useNavigate()
  const list = useListParams()
  const params: PaymentListParams = {
    search: list.search,
    status: oneOf(list.get('status'), statuses),
    provider: oneOf(list.get('provider'), providers),
    page: list.page,
  }

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: operationsKeys.payments(params),
    queryFn: () => operationsApi.payments(params),
    placeholderData: keepPreviousData,
  })
  const hasFilters = params.search !== '' || params.status !== null || params.provider !== null

  // A payment has no page of its own - a click on its row opens the booking it paid for
  // (but not a click on the booking link itself, or one that ended a text selection).
  const openRow = (bookingNo: string) => (event: MouseEvent) => {
    if ((event.target as HTMLElement).closest('a, button') || window.getSelection()?.toString()) return
    navigate(`/admin/bookings/${encodeURIComponent(bookingNo)}`)
  }

  return (
    <div className="grid gap-6">
      <PageHeader title="Payments" description="Every payment attempt, newest first - SSLCommerz and money taken at the office." />

      {/* Filters: every control is h-10, so they line up edge to edge and wrap cleanly on a phone. */}
      <div className="flex min-w-0 animate-fade-up flex-wrap items-center gap-3">
        <div className="relative w-full sm:w-auto sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Payment no., booking no. or transaction id…"
            className="pl-10"
            aria-label="Search payments"
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
                {paymentStatusLabels[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={params.provider ? String(params.provider) : 'all'} onValueChange={(v) => list.update({ provider: v === 'all' ? null : v })}>
          <SelectTrigger className="min-w-0 flex-[1.3] sm:w-48 sm:flex-none" aria-label="Filter by channel">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper">
            <SelectItem value="all">Online and manual</SelectItem>
            {providers.map((p) => (
              <SelectItem key={p} value={String(p)}>
                {paymentProviderLabels[p]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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
          <EmptyState icon={SearchXIcon} title="No payment matches these filters" text="Try another number, or show every status and channel.">
            <Button variant="outline" onClick={list.clear}>
              Show all payments
            </Button>
          </EmptyState>
        ) : (
          <EmptyState icon={CreditCardIcon} title="No payments yet" text="Online payments and money recorded at the office show up here." />
        )
      ) : (
        (isPending || data) && (
          <div className="animate-fade-up overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Payment</TableHead>
                  <TableHead>Booking</TableHead>
                  <TableHead>How</TableHead>
                  <TableHead>When</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody aria-busy={isPending || undefined} className={cn('transition-opacity duration-300', isFetching && !isPending && 'opacity-60')}>
                {/* First load: rows of shimmer in the table's own shape, so nothing jumps when the payments arrive. */}
                {isPending &&
                  Array.from({ length: 6 }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      {Array.from({ length: columnCount }, (_, j) => (
                        <TableCell key={j}>
                          <Skeleton className={cn('h-4', j === 4 ? 'ml-auto w-16' : 'w-24')} />
                          {j === 2 && <Skeleton className="mt-2 h-3 w-16" />}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {data?.items.map((p) => (
                  <TableRow key={p.paymentNo} onClick={openRow(p.bookingNo)} className="cursor-pointer">
                    <TableCell className="font-mono text-[0.8125rem] font-semibold text-ink-900">{p.paymentNo}</TableCell>
                    <TableCell>
                      <Link
                        to={`/admin/bookings/${encodeURIComponent(p.bookingNo)}`}
                        className="font-mono text-[0.8125rem] font-medium text-forest-700 underline-offset-4 hover:underline"
                      >
                        {p.bookingNo}
                      </Link>
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
                    <TableCell className="text-ink-700">{formatDateTime(p.paidAtUtc ?? p.initiatedAtUtc)}</TableCell>
                    <TableCell className="nums text-right font-semibold text-ink-900">{formatTaka(p.amount)}</TableCell>
                    <TableCell>
                      <PaymentStatusBadge status={p.status} />
                      {p.failureReason && <div className="mt-1 max-w-56 truncate text-xs text-clay-600">{p.failureReason}</div>}
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
