import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { SearchIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { toAppError } from '@/shared/api/problem'
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

const columnCount = 6
const statuses = [1, 2, 3, 4, 5, 6, 7] as const satisfies readonly PaymentStatus[]
const providers = [1, 3] as const satisfies readonly PaymentProvider[] // Stripe is Phase 2

/** Admin → Payments: every attempt, online and at the office (17-day plan, Day 12). */
export function AdminPaymentsPage() {
  useDocumentMeta({ title: 'Payments' })
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

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Payments</h1>
        <p className="text-muted-foreground">Every payment attempt, newest first - SSLCommerz and money taken at the office.</p>
      </div>

      <div className="flex flex-wrap gap-2">
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Payment no., booking no. or transaction id…"
            className="pl-8"
            aria-label="Search payments"
          />
        </div>
        <Select value={params.status ? String(params.status) : 'all'} onValueChange={(v) => list.update({ status: v === 'all' ? null : v })}>
          <SelectTrigger className="w-44" aria-label="Filter by status">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {statuses.map((s) => (
              <SelectItem key={s} value={String(s)}>
                {paymentStatusLabels[s]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={params.provider ? String(params.provider) : 'all'} onValueChange={(v) => list.update({ provider: v === 'all' ? null : v })}>
          <SelectTrigger className="w-40" aria-label="Filter by channel">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Online and manual</SelectItem>
            {providers.map((p) => (
              <SelectItem key={p} value={String(p)}>
                {paymentProviderLabels[p]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-lg border">
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
                  No payments found.
                </TableCell>
              </TableRow>
            )}
            {data?.items.map((p) => (
              <TableRow key={p.paymentNo}>
                <TableCell className="font-mono">{p.paymentNo}</TableCell>
                <TableCell>
                  <Link to={`/admin/bookings/${encodeURIComponent(p.bookingNo)}`} className="font-mono hover:underline">
                    {p.bookingNo}
                  </Link>
                </TableCell>
                <TableCell>
                  {paymentProviderLabels[p.provider]}
                  {p.method && <div className="text-xs text-muted-foreground">{p.method}</div>}
                  {p.reference && <div className="font-mono text-xs text-muted-foreground">{p.reference}</div>}
                </TableCell>
                <TableCell className="whitespace-nowrap">{formatDateTime(p.paidAtUtc ?? p.initiatedAtUtc)}</TableCell>
                <TableCell className="text-right tabular-nums">{formatTaka(p.amount)}</TableCell>
                <TableCell>
                  <Badge variant={p.status === 3 ? 'default' : p.status === 4 ? 'destructive' : 'outline'}>{paymentStatusLabels[p.status]}</Badge>
                  {p.failureReason && <div className="max-w-56 truncate text-xs text-muted-foreground">{p.failureReason}</div>}
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
