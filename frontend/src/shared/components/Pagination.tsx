import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'

/**
 * ‹ 1 … 4 5 6 … 10 › - every page is a real LINK (hrefFor), so it can be
 * opened in a new tab, and Back returns to the previous page.
 */
export function Pagination({ page, totalPages, hrefFor }: { page: number; totalPages: number; hrefFor: (page: number) => string }) {
  if (totalPages <= 1) return null

  return (
    <nav aria-label="Pages" className="flex flex-wrap items-center justify-center gap-1">
      <PageLink to={page > 1 ? hrefFor(page - 1) : null} label="Previous page">
        <ChevronLeftIcon />
        <span className="hidden sm:inline">Previous</span>
      </PageLink>

      {pageWindow(page, totalPages).map((p, i) =>
        p === 'gap' ? (
          <span key={`gap-${i}`} className="px-1 text-muted-foreground" aria-hidden>
            …
          </span>
        ) : (
          <Button key={p} asChild size="sm" variant={p === page ? 'default' : 'ghost'} className="min-w-8">
            <Link to={hrefFor(p)} aria-current={p === page ? 'page' : undefined} aria-label={`Page ${p}`}>
              {p}
            </Link>
          </Button>
        ),
      )}

      <PageLink to={page < totalPages ? hrefFor(page + 1) : null} label="Next page">
        <span className="hidden sm:inline">Next</span>
        <ChevronRightIcon />
      </PageLink>
    </nav>
  )
}

/** Previous / Next: a link, or a greyed-out button at either end. */
function PageLink({ to, label, children }: { to: string | null; label: string; children: ReactNode }) {
  if (to === null) {
    return (
      <Button size="sm" variant="ghost" disabled aria-label={label}>
        {children}
      </Button>
    )
  }
  return (
    <Button asChild size="sm" variant="ghost">
      <Link to={to} aria-label={label}>
        {children}
      </Link>
    </Button>
  )
}

/** The first, the last and the current page ±1, with "…" for the skipped stretches: 1 … 4 5 6 … 10. */
function pageWindow(current: number, total: number): (number | 'gap')[] {
  const pages = [...new Set([1, current - 1, current, current + 1, total])]
    .filter((p) => p >= 1 && p <= total)
    .sort((a, b) => a - b)

  const result: (number | 'gap')[] = []
  let previous = 0
  for (const p of pages) {
    if (p - previous > 1) result.push('gap')
    result.push(p)
    previous = p
  }
  return result
}
