import { Badge } from '@/components/ui/badge'
import { tripStatusLabels, type TripStatus } from '../api/trips.api'

const variants: Record<TripStatus, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  1: 'outline',
  2: 'default',
  3: 'default',
  4: 'default',
  5: 'destructive',
  6: 'secondary',
  7: 'secondary',
}

/** A custom trip's status as a coloured label: "Quote ready", "Waiting for a quote"... */
export function TripStatusBadge({ status }: { status: TripStatus }) {
  return <Badge variant={variants[status]}>{tripStatusLabels[status]}</Badge>
}
