import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCheckIcon, SearchIcon, SearchXIcon, Undo2Icon } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { TextField } from '@/shared/components/TextField'
import { formatDateTime } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { ActionDialog } from '../../components/ActionDialog'
import { ListFooter } from '../../components/ListFooter'
import { useListParams } from '../../components/useListParams'
import { moneyRoles, operationsApi, operationsKeys, type AdminRefundListItem, type RefundListParams } from '../api/operations.api'
import { RefundStatusBadge } from '../components/MoneyStatusBadges'

const columnCount = 6

/**
 * Admin → Refunds (17-day plan, Day 12: "refunds to process"). "To process"
 * = money still owed, oldest first. Paid through SSLCommerz → "Refund via
 * SSLCommerz" sends it back the way the customer paid, and it completes by
 * itself when SSLCommerz is done. Otherwise Accounts sends it (bKash, bank)
 * and records the reference - "Mark refunded". Or rejects it with a reason.
 */
export function AdminRefundsPage() {
  useDocumentMeta({ title: 'Refunds' })
  const { hasAnyRole } = useAuth()
  const canAct = hasAnyRole(moneyRoles)
  const queryClient = useQueryClient()
  const list = useListParams()
  const params: RefundListParams = { open: list.get('all') !== '1', search: list.search, page: list.page }
  const [acting, setActing] = useState<{ refund: AdminRefundListItem; action: 'sslcommerz' | 'complete' | 'reject' } | null>(null)

  const { data, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: operationsKeys.refunds(params),
    queryFn: () => operationsApi.refunds(params),
    placeholderData: keepPreviousData,
  })
  const refresh = () => queryClient.invalidateQueries({ queryKey: operationsKeys.all })

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Refunds"
        description="Money owed back to customers. Online payments go back through SSLCommerz; for the rest, send it the way they paid and record the transaction id."
      />

      {/* Filters: the tabs are trimmed to h-10 so they line up with the search box. */}
      <div className="flex min-w-0 animate-fade-up flex-wrap items-center gap-3">
        <Tabs value={params.open ? 'open' : 'all'} onValueChange={(v) => list.update({ all: v === 'all' ? '1' : null })} className="min-w-0">
          <TabsList className="group-data-horizontal/tabs:h-10">
            <TabsTrigger value="open">To process</TabsTrigger>
            <TabsTrigger value="all">All refunds</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative w-full sm:w-auto sm:min-w-56 sm:flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
          <Input
            value={list.searchText}
            onChange={(e) => list.onSearchChange(e.target.value)}
            placeholder="Refund no., booking no. or name…"
            className="pl-10"
            aria-label="Search refunds"
          />
        </div>
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
        params.search ? (
          <EmptyState icon={SearchXIcon} title="No refund matches your search" text="Try another refund or booking number, or a customer's name.">
            <Button variant="outline" onClick={() => list.onSearchChange('')}>
              Clear the search
            </Button>
          </EmptyState>
        ) : params.open ? (
          <EmptyState icon={CheckCheckIcon} title="All caught up" text="Nothing to refund right now. Refunds appear here when a booking is cancelled after paying." />
        ) : (
          <EmptyState icon={Undo2Icon} title="No refunds yet" text="Refunds appear here when a booking is cancelled after paying." />
        )
      ) : (
        (isPending || data) && (
          <div className="animate-fade-up overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
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
              <TableBody aria-busy={isPending || undefined} className={cn('transition-opacity duration-300', isFetching && !isPending && 'opacity-60')}>
                {/* First load: rows of shimmer in the table's own shape, so nothing jumps when the refunds arrive. */}
                {isPending &&
                  Array.from({ length: 4 }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      {Array.from({ length: columnCount }, (_, j) => (
                        <TableCell key={j}>
                          <Skeleton className={cn('h-4', j === 3 ? 'ml-auto w-16' : j === 5 ? 'ml-auto h-9 w-32 rounded-lg' : 'w-24')} />
                          {j < 4 && <Skeleton className={cn('mt-2 h-3', j === 3 ? 'ml-auto w-8' : 'w-20')} />}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {data?.items.map((r) => (
                  <TableRow key={r.refundNo}>
                    <TableCell>
                      <div className="font-mono text-[0.8125rem] font-semibold text-ink-900">{r.refundNo}</div>
                      <div className="text-xs text-ink-500">
                        for{' '}
                        <Link to={`/admin/bookings/${encodeURIComponent(r.bookingNo)}`} className="font-mono font-medium text-forest-700 underline-offset-4 hover:underline">
                          {r.bookingNo}
                        </Link>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="font-medium text-ink-900">{r.contactName}</div>
                      <div className="nums text-xs text-ink-500">{r.contactPhone}</div>
                      <div className="text-xs text-ink-500">Paid by {r.paymentMethod ?? 'unknown'}</div>
                    </TableCell>
                    <TableCell className="max-w-64 min-w-52 whitespace-normal">
                      <div className="text-ink-900">{r.reason}</div>
                      <div className="text-xs text-ink-500">
                        {formatDateTime(r.requestedAtUtc)} · {r.requestedByName ?? 'System'}
                      </div>
                    </TableCell>
                    <TableCell className="nums text-right">
                      <div className="font-semibold text-ink-900">{formatTaka(r.amount)}</div>
                      <div className="text-xs text-ink-500">{r.refundPercent}%</div>
                    </TableCell>
                    <TableCell>
                      <RefundStatusBadge status={r.status} />
                      {r.reference && <div className="mt-1 font-mono text-xs text-ink-500">{r.reference}</div>}
                      {r.rejectReason && <div className="mt-1 max-w-48 text-xs whitespace-normal text-ink-500">{r.rejectReason}</div>}
                    </TableCell>
                    <TableCell className="text-right">
                      {/* To process, or failed at SSLCommerz: still owed - send it (again), by hand, or reject it. */}
                      {canAct && (r.status === 1 || r.status === 6) && (
                        <div className="flex flex-wrap justify-end gap-1.5">
                          {r.paymentProvider === 1 && (
                            <Button size="sm" onClick={() => setActing({ refund: r, action: 'sslcommerz' })}>
                              {r.status === 6 ? 'Try SSLCommerz again' : 'Refund via SSLCommerz'}
                            </Button>
                          )}
                          <Button size="sm" variant={r.paymentProvider === 1 ? 'outline' : 'default'} onClick={() => setActing({ refund: r, action: 'complete' })}>
                            Mark refunded
                          </Button>
                          <Button size="sm" variant="ghost" className="hover:bg-clay-50 hover:text-clay-700" onClick={() => setActing({ refund: r, action: 'reject' })}>
                            Reject
                          </Button>
                        </div>
                      )}
                      {/* With SSLCommerz: nothing to do but wait - or ask now. */}
                      {canAct && r.status === 4 && <CheckNowButton refund={r} onDone={refresh} />}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )
      )}

      <ListFooter data={data} onPage={(page) => list.update({ page: page > 1 ? String(page) : null })} />

      {acting?.action === 'sslcommerz' && (
        <SslCommerzDialog refund={acting.refund} onClose={() => setActing(null)} onDone={refresh} />
      )}
      {acting?.action === 'complete' && (
        <CompleteDialog refund={acting.refund} onClose={() => setActing(null)} onDone={refresh} />
      )}
      {acting?.action === 'reject' && <RejectDialog refund={acting.refund} onClose={() => setActing(null)} onDone={refresh} />}
    </div>
  )
}

type DialogProps = { refund: AdminRefundListItem; onClose: () => void; onDone: () => Promise<void> }

function SslCommerzDialog({ refund, onClose, onDone }: DialogProps) {
  return (
    <ActionDialog
      open
      onOpenChange={(open) => !open && onClose()}
      title={`Refund ${refund.refundNo} through SSLCommerz?`}
      description={
        <>
          SSLCommerz sends <strong>{formatTaka(refund.amount)}</strong> back to {refund.contactName}, to the{' '}
          {refund.paymentMethod ?? 'account'} they paid with. You don't send anything yourself.
        </>
      }
      submitLabel={`Refund ${formatTaka(refund.amount)}`}
      successMessage="Refund sent to SSLCommerz"
      successDescription={`${refund.refundNo} completes here by itself once SSLCommerz has sent it.`}
      onSubmit={async () => {
        try {
          await operationsApi.refundThroughSslCommerz(refund.refundNo)
        } finally {
          // Even when SSLCommerz refused: the refund is now saved as Failed, with the reason.
          await onDone()
        }
      }}
    >
      <p className="text-sm text-ink-500">
        It shows as “With SSLCommerz” until SSLCommerz confirms the money is back. This page asks every 15 minutes - or press Check now.
      </p>
    </ActionDialog>
  )
}

/** Asks SSLCommerz right now how a refund it's sending is going, and says so in a toast. */
function CheckNowButton({ refund, onDone }: { refund: AdminRefundListItem; onDone: () => Promise<void> }) {
  const [pending, setPending] = useState(false)

  const check = async () => {
    setPending(true)
    try {
      const { status } = await operationsApi.checkSslCommerzRefund(refund.refundNo)
      if (status === 5) notify.success('Refund done', { description: `${formatTaka(refund.amount)} is back with ${refund.contactName}.` })
      else if (status === 6) notify.warning("SSLCommerz didn't send this refund", { description: 'It is back on the list: try again, or send it by hand.' })
      else notify.info('Still on its way', { description: 'SSLCommerz is still sending it. This page asks again every 15 minutes.' })
      await onDone()
    } catch (error) {
      notify.error(error)
    } finally {
      setPending(false)
    }
  }

  return (
    <Button size="sm" variant="outline" disabled={pending} onClick={check}>
      {pending && <Spinner />}
      Check now
    </Button>
  )
}

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
      successMessage="Refund recorded"
      successDescription={`${formatTaka(refund.amount)} back to ${refund.contactName} (${refund.refundNo}).`}
      onSubmit={async () => {
        await operationsApi.completeRefund(refund.refundNo, reference)
        await onDone()
      }}
    >
      <TextField
        id="refund-reference"
        label="Transaction id of the refund"
        maxLength={100}
        value={reference}
        onChange={(e) => setReference(e.target.value)}
        autoFocus
      />
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
      successMessage="Refund rejected"
      successDescription={`${refund.refundNo} is closed - nothing will be sent.`}
      onSubmit={async () => {
        await operationsApi.rejectRefund(refund.refundNo, reason)
        await onDone()
      }}
    >
      <FormField label="Reason" htmlFor="reject-reason">
        <Textarea id="reject-reason" maxLength={300} value={reason} onChange={(e) => setReason(e.target.value)} />
      </FormField>
    </ActionDialog>
  )
}
