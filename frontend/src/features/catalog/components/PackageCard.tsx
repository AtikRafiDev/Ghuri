import { ArrowUpRightIcon, CalendarDaysIcon, ImageIcon, MapPinIcon, MoonIcon, StarIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Skeleton } from '@/components/ui/skeleton'
import { describeDuration, formatTaka } from '@/shared/lib/format'
import type { PackageCard as PackageCardData } from '../api/catalog.api'

/**
 * One package in a grid - the home page and the search page. The whole
 * card is one link, so it's one big tap target on a phone. The body is a
 * column whose price block is pushed to the bottom (mt-auto): cards in a
 * row are the same height, so their prices line up even when one title
 * runs to two lines.
 */
export function PackageCard({ pkg }: { pkg: PackageCardData }) {
  const isFlexible = pkg.pricingMode === 2

  return (
    <Link
      to={`/packages/${pkg.slug}`}
      className="group hover-lift flex flex-col overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none"
    >
      <div className="relative aspect-[4/3] overflow-hidden bg-ink-100">
        {pkg.coverImageUrl ? (
          <img
            src={pkg.coverImageUrl}
            alt=""
            loading="lazy"
            className="size-full object-cover transition-transform duration-700 ease-(--ease-out-expo) group-hover:scale-105"
          />
        ) : (
          <div className="flex size-full items-center justify-center">
            <ImageIcon className="size-8 text-ink-300" />
          </div>
        )}
        {/* A soft shade at the top so the white chips stand out on a bright photo. */}
        <div aria-hidden className="absolute inset-x-0 top-0 h-20 bg-gradient-to-b from-forest-950/25 to-transparent" />

        <span className="brand-surface absolute top-3 left-3 inline-flex h-7 items-center gap-1.5 rounded-full bg-white/90 px-2.5 text-xs font-semibold text-forest-800 shadow-soft backdrop-blur-sm">
          {isFlexible ? <MoonIcon className="size-3.5" /> : <CalendarDaysIcon className="size-3.5" />}
          {describeDuration(pkg)}
        </span>
        {pkg.isFeatured && (
          <span className="absolute top-3 right-3 inline-flex h-7 items-center gap-1 rounded-full bg-sun-500 px-2.5 text-xs font-bold text-forest-950 shadow-soft">
            <StarIcon className="size-3.5 fill-current" />
            Featured
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col gap-2 p-5">
        <span className="flex items-center gap-1.5 text-xs font-semibold text-forest-600">
          <MapPinIcon className="size-3.5 shrink-0" />
          <span className="truncate">{pkg.destinationName}</span>
        </span>
        <h3 className="line-clamp-2 text-base leading-snug font-bold text-ink-900 transition-colors group-hover:text-forest-700">
          {pkg.title}
        </h3>
        <p className="line-clamp-2 text-sm text-ink-500">{pkg.summary}</p>

        {/* mt-auto: pinned to the bottom of the card, whatever the text above it. */}
        <div className="mt-auto flex items-end justify-between gap-3 border-t border-ink-100 pt-4">
          {pkg.priceFrom !== null ? (
            <p className="grid gap-0.5">
              <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">From</span>
              <span className="flex items-baseline gap-1">
                <span className="text-xl leading-none font-bold text-ink-900">{formatTaka(pkg.priceFrom)}</span>
                <span className="text-xs text-ink-500">/ person</span>
              </span>
            </p>
          ) : (
            <p className="grid gap-0.5">
              <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Dates</span>
              <span className="text-sm leading-5 font-semibold text-ink-600">New dates coming soon</span>
            </p>
          )}
          <span
            aria-hidden
            className="flex size-9 shrink-0 items-center justify-center rounded-full bg-forest-50 text-forest-700 transition-[background-color,color,rotate] duration-300 group-hover:rotate-45 group-hover:bg-primary group-hover:text-white"
          >
            <ArrowUpRightIcon className="size-4" />
          </span>
        </div>
      </div>
    </Link>
  )
}

/** The same shape while loading, so the grid doesn't jump when the cards arrive. */
export function PackageCardSkeleton() {
  return (
    <div className="flex flex-col overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80">
      <Skeleton className="aspect-[4/3] w-full rounded-none" />
      <div className="grid gap-2 p-5">
        <Skeleton className="h-3.5 w-24" />
        <Skeleton className="h-5 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <div className="mt-2 flex items-end justify-between gap-3 border-t border-ink-100 pt-4">
          <div className="grid gap-1.5">
            <Skeleton className="h-3 w-10" />
            <Skeleton className="h-5 w-28" />
          </div>
          <Skeleton className="size-9 rounded-full" />
        </div>
      </div>
    </div>
  )
}
