import { Badge } from '@/components/ui/badge'
import { PackageStatus, packageStatusLabels } from '../api/packages.api'

/** Published = green (it's live), Draft = amber (not finished), Archived = grey. */
export function PackageStatusBadge({ status }: { status: PackageStatus }) {
  const variant = status === PackageStatus.Published ? 'success' : status === PackageStatus.Draft ? 'warning' : 'neutral'
  return (
    <Badge variant={variant} dot>
      {packageStatusLabels[status]}
    </Badge>
  )
}
