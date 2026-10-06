import { useMutation, useQueryClient } from '@tanstack/react-query'
import { BanIcon, FileDownIcon, HandCoinsIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useAuth } from '@/features/auth/useAuth'
import { toAppError } from '@/shared/api/problem'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'
import { saveFile } from '@/shared/lib/saveFile'
import { ActionDialog } from '../../components/ActionDialog'
import { cancelRoles, manualPaymentMethods, moneyRoles, operationsApi, operationsKeys, type AdminBooking } from '../api/operations.api'

/**
 * What staff can do with one booking (17-day plan, Day 12): take a payment,
 * cancel for the agency, download its PDFs. Buttons show only for the roles
 * the API allows (ManageMoney / CancelBookings) - the API checks again.
 * Sits in the page header's action area; a failed download pops up as a toast.
 */
export function BookingActions({ booking }: { booking: AdminBooking }) {
  const { hasAnyRole } = useAuth()
  const queryClient = useQueryClient()
  const [dialog, setDialog] = useState<'pay' | 'cancel' | null>(null)
  const refresh = () => queryClient.invalidateQueries({ queryKey: operationsKeys.all })

  const download = useMutation({
    mutationFn: (kind: 'invoice' | 'voucher') => operationsApi.document(booking.bookingNo, kind),
    onSuccess: (blob, kind) => saveFile(blob, `${kind === 'voucher' ? 'Voucher' : 'Invoice'}-${booking.bookingNo}.pdf`),
    onError: (error, kind) =>
      notify.error(`Couldn't download the ${kind === 'voucher' ? 'e-voucher' : 'invoice'}`, { description: toAppError(error).message }),
  })

  const canPay = booking.canRecordPayment && hasAnyRole(moneyRoles)
  const canCancel = booking.canCancel && hasAnyRole(cancelRoles)
  const hasVoucher = booking.status === 2 || booking.status === 4
  const hasInvoice = booking.paidAmount > 0

  return (
    <div className="flex flex-wrap items-center gap-2">
      {hasVoucher && (
        <Button variant="outline" disabled={download.isPending} onClick={() => download.mutate('voucher')}>
          {download.isPending && download.variables === 'voucher' ? <Spinner /> : <FileDownIcon />}
          E-voucher
        </Button>
      )}
      {hasInvoice && (
        <Button variant="outline" disabled={download.isPending} onClick={() => download.mutate('invoice')}>
          {download.isPending && download.variables === 'invoice' ? <Spinner /> : <FileDownIcon />}
          Invoice
        </Button>
      )}
      {canCancel && (
        <Button variant="outline" className="text-clay-600 hover:border-clay-300 hover:bg-clay-50 hover:text-clay-700" onClick={() => setDialog('cancel')}>
          <BanIcon />
          Cancel booking
        </Button>
      )}
      {canPay && (
        <Button onClick={() => setDialog('pay')}>
          <HandCoinsIcon />
          Record payment
        </Button>
      )}

      <RecordPaymentDialog booking={booking} open={dialog === 'pay'} onOpenChange={(o) => setDialog(o ? 'pay' : null)} onDone={refresh} />
      <AgencyCancelDialog booking={booking} open={dialog === 'cancel'} onOpenChange={(o) => setDialog(o ? 'cancel' : null)} onDone={refresh} />
    </div>
  )
}

type DialogProps = { booking: AdminBooking; open: boolean; onOpenChange: (open: boolean) => void; onDone: () => Promise<void> }

/** Money paid at the office or straight to the agency's bank / bKash - confirms the booking like an online payment. */
function RecordPaymentDialog({ booking, open, onOpenChange, onDone }: DialogProps) {
  const [method, setMethod] = useState('1')
  const [reference, setReference] = useState('')
  const needsReference = method !== '1' && method !== '7' // cash and "other" may have none

  return (
    <ActionDialog
      open={open}
      onOpenChange={onOpenChange}
      title={`Record payment for ${booking.bookingNo}`}
      description={
        <>
          The customer paid <strong>{formatTaka(booking.amountDue)}</strong> outside the website. Saving confirms the booking and
          emails the voucher.{booking.status === 6 && ' This booking had expired - its seats are taken again if they are still free.'}
        </>
      }
      submitLabel={`Record ${formatTaka(booking.amountDue)}`}
      successMessage="Payment recorded"
      successDescription={`${formatTaka(booking.amountDue)} for ${booking.bookingNo} - the voucher is on its way to the customer.`}
      onSubmit={async () => {
        await operationsApi.recordPayment(booking.bookingNo, {
          amount: booking.amountDue,
          method: Number(method),
          reference: reference.trim() || null,
        })
        setReference('')
        await onDone()
      }}
    >
      <div className="grid gap-5">
        <FormField label="How did they pay?" htmlFor="pay-method">
          <Select value={method} onValueChange={setMethod}>
            <SelectTrigger id="pay-method" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent position="popper">
              {manualPaymentMethods.map((m) => (
                <SelectItem key={m.value} value={String(m.value)}>
                  {m.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FormField>
        <TextField
          id="pay-reference"
          label={`Transaction id / receipt no.${needsReference ? '' : ' (optional)'}`}
          maxLength={100}
          value={reference}
          onChange={(e) => setReference(e.target.value)}
        />
      </div>
    </ActionDialog>
  )
}

/** Cancel for the agency - always a full refund of what was paid. */
function AgencyCancelDialog({ booking, open, onOpenChange, onDone }: DialogProps) {
  const [reason, setReason] = useState('')

  return (
    <ActionDialog
      open={open}
      onOpenChange={onOpenChange}
      title={`Cancel ${booking.bookingNo}?`}
      description={
        booking.paidAmount > 0 ? (
          <>
            The customer gets <strong>everything back: {formatTaka(booking.paidAmount)}</strong> - a refund appears under Refunds for
            Accounts to send. The seats are released.
          </>
        ) : (
          'Nothing was paid, so nothing is refunded. The seats are released.'
        )
      }
      submitLabel="Cancel booking"
      destructive
      successMessage="Booking cancelled"
      successDescription={
        booking.paidAmount > 0 ? `A ${formatTaka(booking.paidAmount)} refund is waiting under Refunds.` : 'The seats are released.'
      }
      onSubmit={async () => {
        await operationsApi.cancel(booking.bookingNo, reason)
        setReason('')
        await onDone()
      }}
    >
      <FormField label="Reason (the customer sees it)" htmlFor="cancel-reason">
        <Textarea id="cancel-reason" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
      </FormField>
    </ActionDialog>
  )
}
