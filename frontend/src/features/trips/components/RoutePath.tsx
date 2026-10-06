import { cn } from '@/lib/utils'

/**
 * "Sylhet → Cox's Bazar → Bandarban" drawn as a little route: a dot per stop
 * (green start, amber end), joined by short dashed lines. Screen readers hear
 * "Sylhet to Cox's Bazar to Bandarban". Used by My trips, the trip page and Accept.
 * onDark: white names and softer rings, for the dark green summary panels.
 */
export function RoutePath({
  stops,
  size = 'default',
  onDark = false,
  className,
}: {
  stops: string[]
  size?: 'default' | 'lg'
  onDark?: boolean
  className?: string
}) {
  return (
    <span className={cn('flex min-w-0 flex-wrap items-center gap-y-1', size === 'lg' ? 'gap-x-3' : 'gap-x-2', className)}>
      {stops.map((stop, i) => (
        <span key={i} className={cn('flex min-w-0 items-center', size === 'lg' ? 'gap-3' : 'gap-2')}>
          <span
            aria-hidden
            className={cn(
              'shrink-0 rounded-full',
              size === 'lg' ? 'size-3 ring-4' : 'size-2.5 ring-[3px]',
              i === 0 ? (onDark ? 'bg-forest-300' : 'bg-forest-600') : i === stops.length - 1 ? 'bg-sun-500' : 'bg-forest-400',
              onDark ? 'ring-white/15' : i === 0 ? 'ring-forest-100' : i === stops.length - 1 ? 'ring-sun-100' : 'ring-forest-50',
            )}
          />
          <span className={cn('truncate font-semibold', onDark ? 'text-white' : 'text-ink-900', size === 'lg' && 'font-bold')}>
            {i > 0 && <span className="sr-only">to </span>}
            {stop}
          </span>
          {/* The dash trails the stop, so a wrapped route reads "Dhaka - - Sylhet - -" / "Sreemangal". */}
          {i < stops.length - 1 && (
            <span aria-hidden className={cn('shrink-0 border-t-2 border-dashed', onDark ? 'border-forest-300/60' : 'border-forest-300', size === 'lg' ? 'w-6' : 'w-4')} />
          )}
        </span>
      ))}
    </span>
  )
}
