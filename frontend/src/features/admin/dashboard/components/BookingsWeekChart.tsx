import { cn } from '@/lib/utils'
import { dayMonth, niceMax, weekday } from '../lib/chartMath'

type Day = { date: string; bookings: number }

/**
 * Bookings made on each of the last 7 days, as pill columns in their own
 * soft tracks. Today is the dark column with its count always showing
 * (emphasis: the one day that matters); every other day shows its count on
 * hover or keyboard focus.
 */
export function BookingsWeekChart({ days }: { days: Day[] }) {
  // 20% headroom above the busiest day, so its count bubble never rises into the card's header.
  const max = niceMax(Math.max(...days.map((d) => d.bookings), 1) * 1.2)
  const today = days.length - 1

  return (
    <ol className="grid min-h-52 flex-1 grid-cols-7 items-end gap-2 pt-6 sm:gap-3" aria-label="Bookings per day, last 7 days">
      {days.map((d, i) => {
        const isToday = i === today
        const share = d.bookings / max
        return (
          <li
            key={d.date}
            tabIndex={0}
            aria-label={`${isToday ? 'Today' : `${weekday(d.date)} ${dayMonth(d.date)}`}: ${d.bookings} booking${d.bookings === 1 ? '' : 's'}`}
            className="group flex h-full flex-col items-center gap-2 outline-none"
          >
            <div className="relative w-full max-w-9 flex-1 rounded-full bg-forest-50 ring-1 ring-forest-100/60 ring-inset">
              {d.bookings > 0 && (
                <div
                  className={cn(
                    'absolute inset-x-0 bottom-0 min-h-4 origin-bottom rounded-full transition-colors duration-200',
                    // Dark theme: the quiet days dim and today glows, so the emphasis still lands on today.
                    isToday ? 'bg-primary dark:bg-forest-400' : 'bg-forest-300 group-hover:bg-forest-500 group-focus-visible:bg-forest-500 dark:bg-forest-200',
                  )}
                  style={{ height: `${share * 100}%`, animation: `grow-y 0.9s var(--ease-out-expo) ${0.1 + i * 0.06}s both` }}
                />
              )}
              <span
                className={cn(
                  'absolute left-1/2 z-10 -translate-x-1/2 -translate-y-full rounded-lg px-2 py-1 text-xs font-bold whitespace-nowrap shadow-soft transition-opacity duration-200',
                  isToday ? 'bg-forest-950 text-white opacity-100 dark:bg-forest-400 dark:text-forest-950' : 'bg-card text-ink-900 opacity-0 ring-1 ring-ink-200 group-hover:opacity-100 group-focus-visible:opacity-100',
                )}
                style={{ bottom: `calc(${share * 100}% + 6px)` }}
              >
                {d.bookings}
              </span>
            </div>
            <span className={cn('text-xs', isToday ? 'font-bold text-forest-700' : 'font-medium text-ink-400')}>{weekday(d.date, 'narrow')}</span>
          </li>
        )
      })}
    </ol>
  )
}
