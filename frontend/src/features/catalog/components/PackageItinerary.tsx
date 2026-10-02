import { BedDoubleIcon, ChevronDownIcon, UtensilsIcon } from 'lucide-react'
import type { ItineraryDay } from '../api/catalog.api'

const mealNames: Record<string, string> = { B: 'Breakfast', L: 'Lunch', D: 'Dinner' }

/** "B,L" → "Breakfast, Lunch". */
function describeMeals(meals: string | null): string | null {
  const names = (meals ?? '').split(',').map((flag) => mealNames[flag.trim()]).filter(Boolean)
  return names.length > 0 ? names.join(', ') : null
}

/**
 * Day by day, each day folds open. Built on the browser's own <details>:
 * keyboard and screen-reader support for free, and no extra package.
 * Day 1 starts open so the customer sees there's more inside.
 */
export function PackageItinerary({ days }: { days: ItineraryDay[] }) {
  return (
    <ol className="grid gap-2">
      {days.map((day, i) => {
        const meals = describeMeals(day.meals)
        return (
          <li key={day.dayNo}>
            <details open={i === 0} className="group rounded-lg border">
              <summary className="flex cursor-pointer list-none items-center gap-3 p-3 [&::-webkit-details-marker]:hidden">
                <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground">
                  Day {day.dayNo}
                </span>
                <span className="flex-1 font-medium">{day.title}</span>
                <ChevronDownIcon className="size-4 shrink-0 text-muted-foreground transition-transform group-open:rotate-180" />
              </summary>
              <div className="grid gap-2 border-t px-3 pt-2 pb-3 text-sm">
                <p className="whitespace-pre-line text-muted-foreground">{day.description}</p>
                {(meals || day.accommodation) && (
                  <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs">
                    {meals && (
                      <span className="flex items-center gap-1">
                        <UtensilsIcon className="size-3.5 text-muted-foreground" />
                        {meals}
                      </span>
                    )}
                    {day.accommodation && (
                      <span className="flex items-center gap-1">
                        <BedDoubleIcon className="size-3.5 text-muted-foreground" />
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
