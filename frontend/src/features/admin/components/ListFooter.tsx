import { Button } from '@/components/ui/button'
import type { Paged } from '@/shared/api/paged'

/** "21–40 of 135 · Page 2 of 7 · Previous / Next" under an admin table. */
export function ListFooter({ data, onPage }: { data: Paged<unknown> | undefined; onPage: (page: number) => void }) {
  if (!data || data.totalCount === 0) return null

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
      <span>
        {(data.page - 1) * data.pageSize + 1}–{Math.min(data.page * data.pageSize, data.totalCount)} of {data.totalCount}
      </span>
      <div className="flex items-center gap-2">
        <span>
          Page {data.page} of {data.totalPages}
        </span>
        <Button variant="outline" size="sm" disabled={data.page <= 1} onClick={() => onPage(data.page - 1)}>
          Previous
        </Button>
        <Button variant="outline" size="sm" disabled={data.page >= data.totalPages} onClick={() => onPage(data.page + 1)}>
          Next
        </Button>
      </div>
    </div>
  )
}
