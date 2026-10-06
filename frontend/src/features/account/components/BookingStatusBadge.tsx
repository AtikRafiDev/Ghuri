import { Badge } from '@/components/ui/badge'
import type { BookingStatus } from '@/features/booking/api/bookings.api'

const labels: Record<BookingStatus, { text: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  1: { text: 'Waiting for payment', variant: 'outline' },
  2: { text: 'Confirmed', variant: 'default' },
  3: { text: 'Partly paid', variant: 'secondary' },
  4: { text: 'Completed', variant: 'secondary' },
  5: { text: 'Cancelled', variant: 'destructive' },
  6: { text: 'Expired', variant: 'secondary' },
}

/** A booking's status as a coloured label: "Confirmed", "Cancelled"... */
export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  const { text, variant } = labels[status]
  return <Badge variant={variant}>{text}</Badge>
}
