import { useMutation } from '@tanstack/react-query'
import { FileDownIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { bookingsApi, type MyBooking } from '@/features/booking/api/bookings.api'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { saveFile } from '@/shared/lib/saveFile'

type DocumentKind = 'voucher' | 'invoice'

/**
 * "E-voucher" and "Invoice" download buttons (17-day plan, Day 11). Shown
 * only when the API would give them: the voucher while the booking is
 * confirmed, the invoice once something is paid.
 */
export function BookingDocuments({ booking }: { booking: MyBooking }) {
  const hasVoucher = booking.status === 2 || booking.status === 4
  const hasInvoice = booking.paidAmount > 0

  const download = useMutation({
    mutationFn: (kind: DocumentKind) => bookingsApi.document(booking.bookingNo, kind),
    onSuccess: (blob, kind) => saveFile(blob, `${kind === 'voucher' ? 'Voucher' : 'Invoice'}-${booking.bookingNo}.pdf`),
  })

  if (!hasVoucher && !hasInvoice) return null

  const button = (kind: DocumentKind, label: string) => (
    <Button variant="outline" disabled={download.isPending} onClick={() => download.mutate(kind)}>
      {download.isPending && download.variables === kind ? <Spinner /> : <FileDownIcon />}
      {label}
    </Button>
  )

  return (
    <section className="grid gap-3 rounded-xl border p-4">
      <h2 className="font-semibold">Documents</h2>
      <div className="flex flex-wrap gap-2">
        {hasVoucher && button('voucher', 'E-voucher (PDF)')}
        {hasInvoice && button('invoice', 'Invoice (PDF)')}
      </div>
      {download.isError && <FormAlert kind="error">{toAppError(download.error).message}</FormAlert>}
    </section>
  )
}
