import { Link } from 'react-router'
import { cn } from '@/lib/utils'
import { site } from '@/shared/config/site'

/** The Ghuri logo tile: a white map pin with a sun inside, on forest green (same drawing as public/favicon.svg). */
export function BrandMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" aria-hidden className={cn('brand-surface size-8 shrink-0', className)}>
      <rect width="32" height="32" rx="9" className="fill-forest-700" />
      <path d="M16 6.5a7 7 0 0 0-7 7c0 5 7 12.5 7 12.5s7-7.5 7-12.5a7 7 0 0 0-7-7z" className="fill-white" />
      <circle cx="16" cy="13.5" r="2.8" className="fill-sun-500" />
    </svg>
  )
}

/** Logo tile + "Ghuri" wordmark, linking home. tone="light" for dark backgrounds (footer). */
export function BrandLink({ to = '/', tone = 'dark', className }: { to?: string; tone?: 'dark' | 'light'; className?: string }) {
  return (
    <Link
      to={to}
      className={cn(
        'group/brand flex shrink-0 items-center gap-2.5 rounded-xl text-xl font-bold tracking-tight focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none',
        tone === 'dark' ? 'text-forest-900' : 'text-white',
        className,
      )}
    >
      <BrandMark className="transition-transform duration-500 ease-(--ease-spring) group-hover/brand:-rotate-6 group-hover/brand:scale-105" />
      {site.name}
    </Link>
  )
}
