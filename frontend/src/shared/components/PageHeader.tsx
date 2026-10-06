import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/**
 * The top of a page - title, a line of explanation, and its main buttons on
 * the right - laid out the same way everywhere, so titles and actions line
 * up from page to page. Above the title: an optional back link or eyebrow.
 * titleAside sits beside the title (a status badge, a code) but outside the
 * <h1>, so screen readers announce just the title as the heading.
 */
export function PageHeader({
  title,
  titleAside,
  description,
  actions,
  eyebrow,
  className,
}: {
  title: ReactNode
  titleAside?: ReactNode
  description?: ReactNode
  actions?: ReactNode
  eyebrow?: ReactNode
  className?: string
}) {
  return (
    <header className={cn('flex animate-fade-up flex-wrap items-end justify-between gap-x-6 gap-y-4', className)}>
      <div className="grid min-w-0 gap-1.5">
        {eyebrow && <div className="flex items-center">{eyebrow}</div>}
        <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
          <h1 className="text-2xl font-bold text-ink-900 sm:text-[1.75rem]">{title}</h1>
          {titleAside}
        </div>
        {description && <div className="max-w-2xl text-sm text-ink-500 sm:text-[0.9375rem]">{description}</div>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  )
}
