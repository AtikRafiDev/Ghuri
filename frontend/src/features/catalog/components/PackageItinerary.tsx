import { BedDoubleIcon, ChevronDownIcon, UtensilsIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { ItineraryDay } from '../api/catalog.api'

const mealNames: Record<string, string> = { B: 'Breakfast', L: 'Lunch', D: 'Dinner' }

/** "B,L" → "Breakfast, Lunch". */
function describeMeals(meals: string | null): string | null {
  const names = (meals ?? '').split(',').map((flag) => mealNames[flag.trim()]).filter(Boolean)
  return names.length > 0 ? names.join(', ') : null
}

/**
 * Day by day as a vertical timeline: a numbered node per day on a line that
 * joins them, and beside each node a card that folds open. Built on the
 * browser's own <details>: keyboard and screen-reader support for free, and
 * no extra package. Day 1 starts open so the customer sees there's more inside.
 *
 * Alignment: the node (40px) sits 12px down, so its centre is at 32px - the
 * same as the centre of the card's 64px-tall title row. The line runs from
 * the first node's centre to the last node's centre.
 */
export function PackageItinerary({ days }: { days: ItineraryDay[] }) {
  return (
    <ol className="grid">
      {days.map((day, i) => {
        const meals = describeMeals(day.meals)
        const first = i === 0
        const last = i === days.length - 1
        return (
          <li key={day.dayNo} className={cn('relative flex gap-4 sm:gap-5', !last && 'pb-3')}>
            {days.length > 1 && (
              <span
                aria-hidden
                className={cn('absolute left-5 w-0.5 -translate-x-1/2 bg-forest-200', first ? 'top-8 bottom-0' : last ? 'top-0 h-8' : 'inset-y-0')}
              />
            )}
            <span
              aria-hidden
              className="nums relative mt-3 flex size-10 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-white shadow-soft ring-4 ring-background"
            >
              {day.dayNo}
            </span>

            <details
              open={first}
              className="group min-w-0 flex-1 rounded-2xl bg-card shadow-soft ring-1 ring-ink-200/80 transition-shadow duration-300 open:shadow-card open:ring-forest-200"
            >
              <summary className="flex min-h-16 cursor-pointer list-none items-center gap-3 rounded-2xl px-4 py-3 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none sm:px-5 [&::-webkit-details-marker]:hidden">
                <span className="grid min-w-0 flex-1 gap-0.5">
                  <span className="text-[0.6875rem] font-semibold tracking-wider text-forest-600 uppercase">Day {day.dayNo}</span>
                  <span className="font-semibold text-ink-900">{day.title}</span>
                </span>
                <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-ink-50 text-ink-500 ring-1 ring-ink-200 transition-[rotate,background-color,color] duration-300 group-open:rotate-180 group-open:bg-forest-50 group-open:text-forest-700 group-open:ring-forest-200">
                  <ChevronDownIcon className="size-4" />
                </span>
              </summary>
              <div className="grid gap-3 px-4 pb-4 text-sm sm:px-5 sm:pb-5">
                <p className="leading-relaxed whitespace-pre-line text-ink-600">{day.description}</p>
                {(meals || day.accommodation) && (
                  <div className="flex flex-wrap gap-2">
                    {meals && (
                      <span className="inline-flex h-7 items-center gap-1.5 rounded-full bg-ink-50 px-3 text-xs font-medium text-ink-700 ring-1 ring-ink-200 ring-inset">
                        <UtensilsIcon className="size-3.5 text-forest-600" />
                        {meals}
                      </span>
                    )}
                    {day.accommodation && (
                      <span className="inline-flex h-7 items-center gap-1.5 rounded-full bg-ink-50 px-3 text-xs font-medium text-ink-700 ring-1 ring-ink-200 ring-inset">
                        <BedDoubleIcon className="size-3.5 text-forest-600" />
                        {day.accommodation}
                      </span>
                    )}
                  </div>
                )}
              </div>
            </details>
          </li>
        )
      })}
    </ol>
  )
}
