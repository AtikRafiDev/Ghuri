import { ChevronRightIcon } from 'lucide-react'
import { Link } from 'react-router'
import type { MyBookingSummary } from '@/features/booking/api/bookings.api'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { BookingStatusBadge } from './BookingStatusBadge'

/** The customer's bookings as a list of links to each booking's page. */
export function BookingList({ bookings }: { bookings: MyBookingSummary[] }) {
  return (
    <ul className="divide-y rounded-xl border bg-card">
      {bookings.map((b) => (
        <li key={b.bookingNo}>
          <Link
            to={`/account/bookings/${encodeURIComponent(b.bookingNo)}`}
            className="flex items-center gap-4 p-4 transition-colors hover:bg-muted/50"
          >
            <div className="grid min-w-0 flex-1 gap-1">
              <div className="flex flex-wrap items-center gap-2">
                <span className="truncate font-medium">{b.packageTitle ?? 'Custom trip'}</span>
                <BookingStatusBadge status={b.status} />
              </div>
              <p className="text-sm text-muted-foreground">
                {formatDate(b.startDate)} → {formatDate(b.endDate)} · {b.travellers} traveller{b.travellers === 1 ? '' : 's'} ·{' '}
                {formatTaka(b.totalAmount)}
              </p>
              <p className="font-mono text-xs text-muted-foreground">{b.bookingNo}</p>
            </div>
            <ChevronRightIcon className="size-4 shrink-0 text-muted-foreground" />
          </Link>
        </li>
      ))}
    </ul>
  )
}
