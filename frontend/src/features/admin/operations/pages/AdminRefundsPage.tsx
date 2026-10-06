import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { SearchIcon } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { toAppError } from '@/shared/api/problem'
import { formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ActionDialog } from '../../components/ActionDialog'
import { ListFooter } from '../../components/ListFooter'
import { useListParams } from '../../components/useListParams'
import {
  moneyRoles,
  operationsApi,
  operationsKeys,
  refundStatusLabels,
  type AdminRefundListItem,
  type RefundListParams,
} from '../api/operations.api'

const columnCount = 6

/**
 * Admin → Refunds (17-day plan, Day 12: "refunds to process"). "To process"
 * = money still owed, oldest first. Accounts sends it (bKash, bank) and
 * records the reference - "Mark refunded"; or rejects it with a reason.
 */
export function AdminRefundsPage() {
  useDocumentMeta({ title: 'Refunds' })
  const { hasAnyRole } = useAuth()
  const canAct = hasAnyRole(moneyRoles)
  const queryClient = useQueryClient()
  const list = useListParams()
  const params: RefundListParams = { open: list.get('all') !== '1', search: list.search, page: list.page }
  const [acting, setActing] = useState<{ refund: AdminRefundListItem; action: 'complete' | 'reject' } | null>(null)

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: operationsKeys.refunds(params),
    queryFn: () => operationsApi.refunds(params),
    placeholderData: keepPreviousData,
  })
  const refresh = () => queryClient.invalidateQueries({ queryKey: operationsKeys.all })

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Refunds</h1>
        <p className="text-muted-foreground">
          Money owed back to customers. Send it the way they paid, then record the transaction id.
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Tabs value={params.open ? 'open' : 'all'} onValueChange={(v) => list.update({ all: v === 'all' ? '1' : null })}>
          <TabsList>
            <TabsTrigger value="open">To process</TabsTrigger>
            <TabsTrigger value="all">All refunds</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative min-w-60 flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Refund no., booking no. or name…"
            className="pl-8"
            aria-label="Search refunds"
          />
        </div>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Refund</TableHead>
              <TableHead>Customer</TableHead>
              <TableHead>Why</TableHead>
              <TableHead className="text-right">Amount</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">
                <span className="sr-only">Actions</span>
              </TableHead>
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
                  {params.open ? 'Nothing to refund right now.' : 'No refunds found.'}
                </TableCell>
              </TableRow>
            )}
            {data?.items.map((r) => (
              <TableRow key={r.refundNo}>
                <TableCell>
                  <span className="font-mono font-medium">{r.refundNo}</span>
                  <div className="text-xs text-muted-foreground">
                    <Link to={`/admin/bookings/${encodeURIComponent(r.bookingNo)}`} className="font-mono hover:underline">
                      {r.bookingNo}
                    </Link>{' '}
                    · {formatDateTime(r.requestedAtUtc)} · {r.requestedByName ?? 'System'}
                  </div>
                </TableCell>
                <TableCell>
                  {r.contactName}
                  <div className="text-xs text-muted-foreground">{r.contactPhone}</div>
                  <div className="text-xs text-muted-foreground">Paid by {r.paymentMethod ?? 'unknown'}</div>
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
                <TableCell className="text-right">
                  {canAct && r.status === 1 && (
                    <div className="flex justify-end gap-1">
                      <Button size="sm" onClick={() => setActing({ refund: r, action: 'complete' })}>
                        Mark refunded
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => setActing({ refund: r, action: 'reject' })}>
                        Reject
                      </Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <ListFooter data={data} onPage={(page) => list.update({ page: page > 1 ? String(page) : null })} />

      {acting?.action === 'complete' && (
        <CompleteDialog refund={acting.refund} onClose={() => setActing(null)} onDone={refresh} />
      )}
      {acting?.action === 'reject' && <RejectDialog refund={acting.refund} onClose={() => setActing(null)} onDone={refresh} />}
    </div>
  )
}

type DialogProps = { refund: AdminRefundListItem; onClose: () => void; onDone: () => Promise<void> }

function CompleteDialog({ refund, onClose, onDone }: DialogProps) {
  const [reference, setReference] = useState('')
  return (
    <ActionDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Mark ${refund.refundNo} as refunded`}
      description={
        <>
          Send <strong>{formatTaka(refund.amount)}</strong> to {refund.contactName} ({refund.contactPhone}) first - they paid by{' '}
          {refund.paymentMethod ?? 'an unknown method'}. Then record the transaction id of the money you sent.
        </>
      }
      submitLabel="Mark refunded"
      onSubmit={async () => {
        await operationsApi.completeRefund(refund.refundNo, reference)
        await onDone()
      }}
    >
      <div className="grid gap-1.5">
        <Label htmlFor="refund-reference">Transaction id of the refund</Label>
        <Input id="refund-reference" maxLength={100} value={reference} onChange={(e) => setReference(e.target.value)} autoFocus />
      </div>
    </ActionDialog>
  )
}

function RejectDialog({ refund, onClose, onDone }: DialogProps) {
  const [reason, setReason] = useState('')
  return (
    <ActionDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Reject ${refund.refundNo}?`}
      description={`Only if ${formatTaka(refund.amount)} is NOT owed after all - e.g. it was already given back another way.`}
      submitLabel="Reject refund"
      destructive
      onSubmit={async () => {
        await operationsApi.rejectRefund(refund.refundNo, reason)
        await onDone()
      }}
    >
      <div className="grid gap-1.5">
        <Label htmlFor="reject-reason">Reason</Label>
        <Textarea id="reject-reason" maxLength={300} value={reason} onChange={(e) => setReason(e.target.value)} />
      </div>
    </ActionDialog>
  )
}
