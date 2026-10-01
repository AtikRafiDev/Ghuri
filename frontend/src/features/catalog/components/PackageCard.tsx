import { CalendarDaysIcon, ImageIcon, MapPinIcon, MoonIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { describeDuration, formatTaka } from '@/shared/lib/format'
import type { PackageCard as PackageCardData } from '../api/catalog.api'

/**
 * One package in a grid - the home page now, the search page on Day 7.
 * The whole card is one link, so it's one big tap target on a phone.
 */
export function PackageCard({ pkg }: { pkg: PackageCardData }) {
  const isFlexible = pkg.pricingMode === 2

  return (
    <Link
      to={`/packages/${pkg.slug}`}
      className="group flex flex-col overflow-hidden rounded-xl border bg-card transition-shadow hover:shadow-md focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
    >
      <div className="relative aspect-[4/3] bg-muted">
        {pkg.coverImageUrl ? (
          <img
            src={pkg.coverImageUrl}
            alt=""
            loading="lazy"
            className="size-full object-cover transition-transform duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex size-full items-center justify-center">
            <ImageIcon className="size-8 text-muted-foreground" />
          </div>
        )}
        <Badge variant="secondary" className="absolute top-2 left-2 gap-1">
          {isFlexible ? <MoonIcon /> : <CalendarDaysIcon />}
          {describeDuration(pkg)}
        </Badge>
      </div>

      <div className="flex flex-1 flex-col gap-1 p-3">
        <span className="flex items-center gap-1 text-xs text-muted-foreground">
          <MapPinIcon className="size-3" />
          {pkg.destinationName}
        </span>
        <h3 className="line-clamp-2 font-semibold group-hover:underline">{pkg.title}</h3>
        <p className="line-clamp-2 text-sm text-muted-foreground">{pkg.summary}</p>

        <div className="mt-auto pt-2">
          {pkg.priceFrom !== null ? (
            <p className="text-sm">
              <span className="text-muted-foreground">from </span>
              <span className="text-lg font-semibold">{formatTaka(pkg.priceFrom)}</span>
              <span className="text-muted-foreground"> / person</span>
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">New dates coming soon</p>
          )}
        </div>
      </div>
    </Link>
  )
}

/** The same shape while loading, so the grid doesn't jump when the cards arrive. */
export function PackageCardSkeleton() {
  return (
    <div className="overflow-hidden rounded-xl border">
      <Skeleton className="aspect-[4/3] w-full rounded-none" />
      <div className="grid gap-2 p-3">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-5 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="mt-2 h-6 w-32" />
      </div>
    </div>
  )
}
