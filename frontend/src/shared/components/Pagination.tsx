import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'

/**
 * ‹ 1 … 4 5 6 … 10 › - every page is a real LINK (hrefFor), so it can be
 * opened in a new tab, and Back returns to the previous page. Every button
 * is 36px tall; the page numbers are 36px circles, the current one filled
 * forest green, the rest quiet until hovered.
 */
export function Pagination({ page, totalPages, hrefFor }: { page: number; totalPages: number; hrefFor: (page: number) => string }) {
  if (totalPages <= 1) return null

  return (
    <nav
      aria-label="Pages"
      className="mx-auto flex w-fit max-w-full flex-wrap items-center justify-center gap-1 rounded-full bg-card p-1 shadow-soft ring-1 ring-ink-200/80"
    >
      <PageLink to={page > 1 ? hrefFor(page - 1) : null} label="Previous page">
        <ChevronLeftIcon />
        <span className="hidden sm:inline">Previous</span>
      </PageLink>

      {pageWindow(page, totalPages).map((p, i) =>
        p === 'gap' ? (
          <span key={`gap-${i}`} className="flex size-9 items-center justify-center text-ink-400" aria-hidden>
            …
          </span>
        ) : (
          <Button
            key={p}
            asChild
            size="icon-sm"
            variant={p === page ? 'default' : 'ghost'}
            className="nums rounded-full text-[0.8125rem] hover:translate-y-0"
          >
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

/** Previous / Next: a link, or a greyed-out button at either end. A circle on a phone (icon only), a pill with its word from sm up. */
function PageLink({ to, label, children }: { to: string | null; label: string; children: ReactNode }) {
  const className = 'rounded-full max-sm:w-9 max-sm:px-0'
  if (to === null) {
    return (
      <Button size="sm" variant="ghost" disabled aria-label={label} className={className}>
        {children}
      </Button>
    )
  }
  return (
    <Button asChild size="sm" variant="ghost" className={className}>
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
