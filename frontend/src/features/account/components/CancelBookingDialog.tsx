import { useMutation, useQueryClient } from '@tanstack/react-query'
import { CalendarXIcon } from 'lucide-react'
import { useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { bookingKeys, bookingsApi, type MyBooking } from '@/features/booking/api/bookings.api'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { formatTaka } from '@/shared/lib/format'
import { notify } from '@/shared/lib/notify'

type CancelBookingDialogProps = {
  booking: MyBooking
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * "Cancel this booking?" - says exactly what comes back BEFORE the customer
 * confirms (the API's cancellation quote, the same numbers the refund will
 * use). Once it's done, a toast says what happened and the page reloads the
 * booking (which then shows "Cancelled" and the refund).
 */
export function CancelBookingDialog({ booking, open, onOpenChange }: CancelBookingDialogProps) {
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')
  const { refundAmount, refundPercent, daysBeforeStart } = booking.cancellation
  const paid = booking.paidAmount > 0

  const cancel = useMutation({
    mutationFn: () => bookingsApi.cancel(booking.bookingNo, reason.trim() || null),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: bookingKeys.all }) // this booking, the list, the seats
      onOpenChange(false)
      notify.success(`Booking ${result.bookingNo} cancelled`, {
        description: result.refundNo
          ? `Refund ${result.refundNo} of ${formatTaka(result.refundAmount)} is on its way - our team will send it to you.`
          : paid
            ? 'By our cancellation policy, no refund is due this close to the trip.'
            : 'Nothing was charged - your seats are released.',
      })
    },
  })

  const handleOpenChange = (next: boolean) => {
    if (cancel.isPending) return // don't close half-way
    if (!next) cancel.reset()
    onOpenChange(next)
  }

  const whatHappens = !paid
    ? 'Nothing has been paid, so nothing is charged. Your seats will be released.'
    : refundAmount > 0
      ? 'Here is what comes back, by our cancellation policy. Our team will send it to you.'
      : 'This close to the trip, our cancellation policy gives no refund.'

  return (
    <AlertDialog open={open} onOpenChange={handleOpenChange}>
      <AlertDialogContent className="data-[size=default]:sm:max-w-md">
        <AlertDialogHeader>
          <AlertDialogMedia>
            <CalendarXIcon />
          </AlertDialogMedia>
          <AlertDialogTitle>Cancel booking {booking.bookingNo}?</AlertDialogTitle>
          <AlertDialogDescription>{whatHappens} This can't be undone.</AlertDialogDescription>
        </AlertDialogHeader>

        {/* The quote in two big numbers: the share of the payment and the taka that come back. */}
        {paid && (
          <dl className="grid grid-cols-2 divide-x divide-ink-200/80 rounded-2xl bg-ink-50 py-4 ring-1 ring-ink-200/60 ring-inset">
            <div className="grid gap-1 px-4">
              <dt className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Refund</dt>
              <dd className={cn('text-2xl leading-tight font-bold tracking-tight', refundAmount > 0 ? 'text-ink-900' : 'text-clay-600')}>{refundPercent}%</dd>
              <dd className="text-xs text-ink-500">{daysBeforeStart} days before the trip</dd>
            </div>
            <div className="grid gap-1 px-4">
              <dt className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">You get back</dt>
              <dd className={cn('text-2xl leading-tight font-bold tracking-tight', refundAmount > 0 ? 'text-forest-700' : 'text-clay-600')}>{formatTaka(refundAmount)}</dd>
              <dd className="text-xs text-ink-500">of {formatTaka(booking.paidAmount)} paid</dd>
            </div>
          </dl>
        )}

        <FormField label="Why are you cancelling? (optional)" htmlFor="cancel-reason">
          <Textarea
            id="cancel-reason"
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            disabled={cancel.isPending}
          />
        </FormField>

        {cancel.isError && <FormAlert kind="error">{toAppError(cancel.error).message}</FormAlert>}

        <AlertDialogFooter>
          <AlertDialogCancel disabled={cancel.isPending}>Keep booking</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            disabled={cancel.isPending}
            onClick={(event) => {
              event.preventDefault() // stay open until the API answers
              cancel.mutate()
            }}
          >
            {cancel.isPending && <Spinner />}
            Cancel booking
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
