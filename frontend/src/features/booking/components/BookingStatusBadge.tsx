import { Badge } from '@/components/ui/badge'
import type { BookingStatus } from '@/features/booking/api/bookings.api'

type Tone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

// Tone = what the status means: green good, amber waiting on someone, terracotta lost, grey over.
const labels: Record<BookingStatus, { text: string; tone: Tone }> = {
  1: { text: 'Waiting for payment', tone: 'warning' },
  2: { text: 'Confirmed', tone: 'success' },
  3: { text: 'Partly paid', tone: 'info' },
  4: { text: 'Completed', tone: 'neutral' },
  5: { text: 'Cancelled', tone: 'danger' },
  6: { text: 'Expired', tone: 'neutral' },
}

/** A booking's status as a coloured pill with a dot: "Confirmed", "Cancelled"... */
export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  const { text, tone } = labels[status]
  return (
    <Badge variant={tone} dot>
      {text}
    </Badge>
  )
}
