import { Badge } from '@/components/ui/badge'
import { tripStatusLabels, type TripStatus } from '../api/trips.api'

// Tone = what the status means: amber waiting on staff, green-tint quoted/accepted, green paid, terracotta rejected, grey over.
const tones: Record<TripStatus, 'success' | 'warning' | 'danger' | 'info' | 'neutral'> = {
  1: 'warning',
  2: 'info',
  3: 'info',
  4: 'success',
  5: 'danger',
  6: 'neutral',
  7: 'neutral',
}

/** A custom trip's status as a coloured pill: "Quote ready", "Waiting for a quote"... */
export function TripStatusBadge({ status }: { status: TripStatus }) {
  return (
    <Badge variant={tones[status]} dot>
      {tripStatusLabels[status]}
    </Badge>
  )
}
