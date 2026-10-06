import { cn } from '@/lib/utils'

/** Initials in a green disc ("Nusrat Jahan" → NJ) - the people in the app have no photos. */
export function UserAvatar({ name, size = 'default', className }: { name: string; size?: 'sm' | 'default' | 'lg'; className?: string }) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
  return (
    <span
      aria-hidden
      className={cn(
        'brand-surface flex shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-forest-500 to-forest-800 font-bold text-white ring-2 ring-white dark:ring-transparent',
        size === 'sm' && 'size-7 text-[0.625rem]',
        size === 'default' && 'size-9 text-xs',
        size === 'lg' && 'size-11 text-sm',
        className,
      )}
    >
      {initials || '?'}
    </span>
  )
}
