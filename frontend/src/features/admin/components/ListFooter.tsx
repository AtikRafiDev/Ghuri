import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { Paged } from '@/shared/api/paged'

/** "Showing 21–40 of 135 · ‹ Page 2 of 7 ›" under an admin table. */
export function ListFooter({ data, onPage }: { data: Paged<unknown> | undefined; onPage: (page: number) => void }) {
  if (!data || data.totalCount === 0) return null

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-ink-500">
      <span>
        Showing{' '}
        <span className="nums font-semibold text-ink-900">
          {(data.page - 1) * data.pageSize + 1}–{Math.min(data.page * data.pageSize, data.totalCount)}
        </span>{' '}
        of <span className="nums font-semibold text-ink-900">{data.totalCount}</span>
      </span>
      <div className="flex items-center gap-2">
        <Button variant="outline" size="icon-sm" aria-label="Previous page" disabled={data.page <= 1} onClick={() => onPage(data.page - 1)}>
          <ChevronLeftIcon />
        </Button>
        <span className="nums min-w-24 text-center">
          Page <span className="font-semibold text-ink-900">{data.page}</span> of {data.totalPages}
        </span>
        <Button variant="outline" size="icon-sm" aria-label="Next page" disabled={data.page >= data.totalPages} onClick={() => onPage(data.page + 1)}>
          <ChevronRightIcon />
        </Button>
      </div>
    </div>
  )
}
