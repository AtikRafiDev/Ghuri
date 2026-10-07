import { useQuery } from '@tanstack/react-query'
import { destinationsQuery } from '@/features/catalog/api/catalog.api'
import { cn } from '@/lib/utils'

/**
 * A slow, endless band of every destination's name, solid and outlined in
 * turn - the "where could I go?" moment right under the hero. Pure CSS
 * (animate-marquee): the list is written twice side by side, and sliding
 * the pair left by exactly half brings the second copy to where the first
 * began - so the loop has no visible seam. It stops while hovered, and
 * stands still with "reduce motion" on (index.css).
 */
export function DestinationMarquee() {
  const { data } = useQuery(destinationsQuery(false))
  if (!data || data.length < 3) return null

  const names = data.map((d) => d.name)
  return (
    <section aria-label="Destinations we travel to" className="overflow-hidden border-b border-ink-200 bg-card py-6 sm:py-8">
      <div className="flex w-max animate-marquee hover:[animation-play-state:paused]">
        <NameList names={names} />
        <NameList names={names} hidden />
      </div>
    </section>
  )
}

function NameList({ names, hidden = false }: { names: string[]; hidden?: boolean }) {
  return (
    // The second copy is only there for the loop - screen readers skip it.
    <ul aria-hidden={hidden || undefined} className="flex shrink-0 items-center">
      {names.map((name, i) => (
        <li key={name} className="flex items-center">
          <span
            className={cn(
              'px-6 text-3xl font-extrabold tracking-tight whitespace-nowrap sm:px-8 sm:text-5xl',
              i % 2 === 0 ? 'text-ink-900' : 'text-transparent [-webkit-text-stroke:1.5px_var(--color-ink-400)]',
            )}
          >
            {name}
          </span>
          <span aria-hidden className="size-2.5 shrink-0 rounded-full bg-sun-500 sm:size-3" />
        </li>
      ))}
    </ul>
  )
}
