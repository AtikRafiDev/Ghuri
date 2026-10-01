import { Badge } from '@/components/ui/badge'
import { PackageStatus, packageStatusLabels } from '../api/packages.api'

/** Draft = grey outline, Published = solid (it's live), Archived = muted. */
export function PackageStatusBadge({ status }: { status: PackageStatus }) {
  const variant = status === PackageStatus.Published ? 'default' : status === PackageStatus.Draft ? 'outline' : 'secondary'
  return <Badge variant={variant}>{packageStatusLabels[status]}</Badge>
}
