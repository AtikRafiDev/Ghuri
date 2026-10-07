import { useQuery } from '@tanstack/react-query'
import { ArrowUpRightIcon, MapPinIcon } from 'lucide-react'
import { useRef } from 'react'
import { Link } from 'react-router'
import { Skeleton } from '@/components/ui/skeleton'
import { destinationsQuery, type DestinationSummary } from '@/features/catalog/api/catalog.api'
import { cn } from '@/lib/utils'
import { SectionHeading } from '@/shared/components/SectionHeading'
import { gsap, useGSAP } from '@/shared/motion/gsap'
import { Reveal } from '@/shared/motion/Reveal'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'

/** One big tile and four small ones fill the mosaic exactly. */
const mosaicSize = 5

/**
 * The destinations marked "Show on home page" in the admin, as a photo
 * mosaic: the first one big, the next four around it (with fewer than five,
 * an even grid). Each photo drifts gently inside its frame as the page
 * scrolls. Hidden when there are none.
 */
export function DestinationMosaic() {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()
  const { data, isPending } = useQuery(destinationsQuery(true))
  const tiles = data?.slice(0, mosaicSize) ?? []
  const mosaic = tiles.length === mosaicSize

  useGSAP(
    () => {
      if (reduced) return
      gsap.utils.toArray<HTMLElement>('[data-drift]', root.current).forEach((photo) => {
        gsap.fromTo(
          photo,
          { yPercent: -6 },
          { yPercent: 6, ease: 'none', scrollTrigger: { trigger: photo.parentElement, start: 'top bottom', end: 'bottom top', scrub: true } },
        )
      })
    },
    { scope: root, dependencies: [reduced, tiles.length], revertOnUpdate: true },
  )

  if (!isPending && tiles.length === 0) return null

  return (
    <section ref={root} className="mx-auto grid max-w-6xl gap-10 px-4 py-20 sm:py-28">
      <SectionHeading
        eyebrow="Where to next"
        title="Popular destinations"
        text="Pick a place to see every tour that goes there."
        link={{ to: '/packages', label: 'Browse all trips' }}
      />
      <Reveal
        key={data ? 'tiles' : 'loading'}
        className={cn('grid grid-cols-2 gap-3 sm:gap-5', mosaic ? 'auto-rows-[11rem] sm:auto-rows-[15rem] lg:grid-cols-4' : 'lg:grid-cols-4')}
      >
        {isPending && Array.from({ length: 4 }, (_, i) => <Skeleton key={i} className="aspect-[4/3] rounded-3xl" />)}
        {tiles.map((d, i) => (
          <DestinationTile
            key={d.id}
            destination={d}
            big={mosaic && i === 0}
            className={cn(mosaic ? (i === 0 ? 'col-span-2 row-span-2' : '') : 'aspect-[4/3]')}
          />
        ))}
      </Reveal>
    </section>
  )
}

function DestinationTile({ destination: d, big, className }: { destination: DestinationSummary; big: boolean; className?: string }) {
  return (
    <Link
      to={`/packages?destination=${encodeURIComponent(d.slug)}`}
      className={cn(
        'group relative isolate overflow-hidden rounded-3xl bg-ink-100 shadow-card transition-shadow duration-300 hover:shadow-lift focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none',
        className,
      )}
    >
      {d.imageUrl && (
        // Two layers: the outer one drifts with the scroll (GSAP), the inner photo zooms on hover (CSS) - each owns its own transform.
        <div data-drift className="absolute -inset-y-[8%] inset-x-0 -z-10">
          <img
            src={d.imageUrl}
            alt=""
            loading="lazy"
            decoding="async"
            className="size-full object-cover transition-transform duration-700 ease-(--ease-out-expo) group-hover:scale-110"
          />
        </div>
      )}
      {/* A deep-forest fade at the bottom so the white name stays readable on any photo. */}
      <div className="absolute inset-0 -z-10 bg-gradient-to-t from-forest-950/85 via-forest-950/20 to-transparent transition-opacity duration-300 group-hover:opacity-90" />
      <div className="brand-surface absolute inset-x-0 bottom-0 flex items-end justify-between gap-2 p-4 text-white sm:p-5">
        <div className="grid min-w-0 gap-1">
          <p className={cn('truncate font-extrabold tracking-tight', big ? 'text-2xl sm:text-4xl' : 'text-base sm:text-xl')}>{d.name}</p>
          <p className="flex items-center gap-1 text-xs text-forest-100/85 sm:text-sm">
            <MapPinIcon className="size-3.5 shrink-0" />
            <span className="truncate">{d.countryName}</span>
          </p>
          {big && d.summary && (
            <p className="mt-1 hidden max-w-md text-sm text-forest-100/85 sm:block">
              <span className="line-clamp-2">{d.summary}</span>
            </p>
          )}
        </div>
        <span
          aria-hidden
          className="hidden size-10 shrink-0 translate-y-1 items-center justify-center rounded-full bg-white text-forest-800 opacity-0 transition-[opacity,translate,rotate] duration-300 group-hover:translate-y-0 group-hover:rotate-45 group-hover:opacity-100 sm:flex"
        >
          <ArrowUpRightIcon className="size-4" />
        </span>
      </div>
    </Link>
  )
}
