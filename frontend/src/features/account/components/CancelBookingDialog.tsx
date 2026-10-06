import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Label } from '@/components/ui/label'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { bookingKeys, bookingsApi, type MyBooking } from '@/features/booking/api/bookings.api'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { formatTaka } from '@/shared/lib/format'

type CancelBookingDialogProps = {
  booking: MyBooking
  open: boolean
  onOpenChange: (open: boolean) => void
}

/**
 * "Cancel this booking?" - says exactly what comes back BEFORE the customer
 * confirms (the API's cancellation quote, the same numbers the refund will use).
 */
export function CancelBookingDialog({ booking, open, onOpenChange }: CancelBookingDialogProps) {
  const queryClient = useQueryClient()
  const [reason, setReason] = useState('')
  const { refundAmount, refundPercent } = booking.cancellation
  const paid = booking.paidAmount > 0

  const cancel = useMutation({
    mutationFn: () => bookingsApi.cancel(booking.bookingNo, reason.trim() || null),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: bookingKeys.all }) // this booking, the list, the seats
      onOpenChange(false)
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
      ? `You'll get ${formatTaka(refundAmount)} back (${refundPercent}% of what you paid, by our cancellation policy). Our team will send it to you.`
      : 'This close to the trip, our cancellation policy gives no refund.'

  return (
    <AlertDialog open={open} onOpenChange={handleOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Cancel booking {booking.bookingNo}?</AlertDialogTitle>
          <AlertDialogDescription>{whatHappens} This can't be undone.</AlertDialogDescription>
        </AlertDialogHeader>

        <div className="grid gap-1.5">
          <Label htmlFor="cancel-reason">Why are you cancelling? (optional)</Label>
          <Textarea
            id="cancel-reason"
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            disabled={cancel.isPending}
          />
        </div>

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
