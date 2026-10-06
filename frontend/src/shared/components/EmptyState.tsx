import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

/** "Nothing here yet" - an icon in a soft halo, a heading, a line, and what to do about it. */
export function EmptyState({
  icon: Icon,
  title,
  text,
  children,
  className,
}: {
  icon: LucideIcon
  title: ReactNode
  text?: ReactNode
  children?: ReactNode
  className?: string
}) {
  return (
    <div className={cn('grid animate-fade-up justify-items-center gap-3 rounded-2xl border border-dashed border-ink-300 bg-card/60 px-6 py-12 text-center', className)}>
      <span className="relative flex size-14 items-center justify-center rounded-2xl bg-forest-50 text-forest-600 ring-8 ring-forest-50/50">
        <Icon className="size-6" />
      </span>
      <h2 className="mt-1 text-base font-semibold text-ink-900">{title}</h2>
      {text && <p className="max-w-sm text-sm text-ink-500">{text}</p>}
      {children && <div className="mt-1 flex flex-wrap justify-center gap-2">{children}</div>}
    </div>
  )
}
