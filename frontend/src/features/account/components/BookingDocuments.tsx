import { useMutation } from '@tanstack/react-query'
import { DownloadIcon, ReceiptTextIcon, TicketIcon, type LucideIcon } from 'lucide-react'
import { Spinner } from '@/components/ui/spinner'
import { bookingsApi, type MyBooking } from '@/features/booking/api/bookings.api'
import { toAppError } from '@/shared/api/problem'
import { notify } from '@/shared/lib/notify'
import { saveFile } from '@/shared/lib/saveFile'

type DocumentKind = 'voucher' | 'invoice'

const documents: Record<DocumentKind, { title: string; note: string; icon: LucideIcon }> = {
  voucher: { title: 'E-voucher', note: 'PDF · your trip confirmation', icon: TicketIcon },
  invoice: { title: 'Invoice', note: "PDF · what you've paid", icon: ReceiptTextIcon },
}

/**
 * "E-voucher" and "Invoice" download buttons (17-day plan, Day 11). Shown
 * only when the API would give them: the voucher while the booking is
 * confirmed, the invoice once something is paid. A failed download pops up
 * a toast - the buttons stay, so trying again is one click.
 */
export function BookingDocuments({ booking }: { booking: MyBooking }) {
  const hasVoucher = booking.status === 2 || booking.status === 4
  const hasInvoice = booking.paidAmount > 0

  const download = useMutation({
    mutationFn: (kind: DocumentKind) => bookingsApi.document(booking.bookingNo, kind),
    onSuccess: (blob, kind) => saveFile(blob, `${kind === 'voucher' ? 'Voucher' : 'Invoice'}-${booking.bookingNo}.pdf`),
    onError: (error, kind) => notify.error(`The ${documents[kind].title.toLowerCase()} couldn't be downloaded`, { description: toAppError(error).message }),
  })

  if (!hasVoucher && !hasInvoice) return null

  const button = (kind: DocumentKind) => {
    const { title, note, icon: Icon } = documents[kind]
    const busy = download.isPending && download.variables === kind
    return (
      <button
        type="button"
        disabled={download.isPending}
        aria-busy={busy || undefined}
        onClick={() => download.mutate(kind)}
        className="group flex w-full items-center gap-3 rounded-2xl border border-ink-200/80 bg-card p-3 text-left transition-[border-color,background-color] duration-200 hover:border-forest-200 hover:bg-forest-50/60 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none disabled:cursor-wait disabled:opacity-70"
      >
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-forest-50 text-forest-600">
          <Icon className="size-5" />
        </span>
        <span className="grid min-w-0 flex-1">
          <span className="truncate text-sm font-semibold text-ink-900">{title}</span>
          <span className="truncate text-xs text-ink-500">{note}</span>
        </span>
        <span className="flex size-8 shrink-0 items-center justify-center rounded-full text-ink-500 ring-1 ring-ink-200 transition-colors duration-200 group-hover:bg-primary group-hover:text-white group-hover:ring-primary">
          {busy ? <Spinner className="size-4" /> : <DownloadIcon className="size-4 transition-[translate] duration-300 group-hover:translate-y-0.5" />}
        </span>
      </button>
    )
  }

  return (
    <section className="grid content-start gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
      <header className="grid gap-0.5">
        <h2 className="text-base font-bold text-ink-900">Documents</h2>
        <p className="text-xs text-ink-500">Save them, or print them for the trip.</p>
      </header>
      <div className="grid gap-2.5">
        {hasVoucher && button('voucher')}
        {hasInvoice && button('invoice')}
      </div>
    </section>
  )
}
