import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/**
 * The top of a page - title, a line of explanation, and its main buttons on
 * the right - laid out the same way everywhere, so titles and actions line
 * up from page to page. Above the title: an optional back link or eyebrow.
 * titleAside sits beside the title (a status badge, a code) but outside the
 * <h1>, so screen readers announce just the title as the heading.
 *
 * variant="display": the home page's heading style, for the customer's own
 * pages (account, trips, checkout) - a bigger, bolder title, and a text
 * eyebrow drawn as the small uppercase line with a rule before it. The
 * admin keeps the compact default.
 */
export function PageHeader({
  title,
  titleAside,
  description,
  actions,
  eyebrow,
  variant = 'default',
  className,
}: {
  title: ReactNode
  titleAside?: ReactNode
  description?: ReactNode
  actions?: ReactNode
  eyebrow?: ReactNode
  variant?: 'default' | 'display'
  className?: string
}) {
  const display = variant === 'display'
  return (
    <header className={cn('flex animate-fade-up flex-wrap items-end justify-between gap-x-6 gap-y-4', className)}>
      <div className={cn('grid min-w-0', display ? 'gap-2.5' : 'gap-1.5')}>
        {eyebrow &&
          (display && typeof eyebrow === 'string' ? (
            <p className="flex items-center gap-2 text-xs font-bold tracking-[0.18em] text-forest-600 uppercase">
              <span aria-hidden className="h-px w-8 bg-forest-500" />
              {eyebrow}
            </p>
          ) : (
            <div className="flex items-center">{eyebrow}</div>
          ))}
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
          <h1
            className={cn(
              'text-ink-900',
              display ? 'text-3xl leading-[1.1] font-extrabold tracking-tight sm:text-[2.5rem]' : 'text-2xl font-bold sm:text-[1.75rem]',
            )}
          >
            {title}
          </h1>
          {titleAside}
        </div>
        {description && <div className={cn('max-w-2xl text-ink-500', display ? 'sm:text-lg' : 'text-sm sm:text-[0.9375rem]')}>{description}</div>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  )
}
