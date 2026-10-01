import { useQuery } from '@tanstack/react-query'
import { ArrowRightIcon, MapPinIcon, MoonIcon, SearchIcon } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { CategoryIcon } from '@/features/admin/categories/components/CategoryIcon'
import { categoriesQuery, destinationsQuery, packagesQuery } from '@/features/catalog/api/catalog.api'
import { PackageCard, PackageCardSkeleton } from '@/features/catalog/components/PackageCard'
import { site } from '@/shared/config/site'

const popularCount = 8

/**
 * The home page (17-day plan, Day 6): hero search, popular packages, a
 * banner, destinations and categories. Three small requests run in
 * parallel - each list is cached on its own and reused by later pages.
 */
export function HomePage() {
  return (
    <div className="grid gap-12">
      <Hero />
      <PopularPackages />
      <FlexibleStayBanner />
      <FeaturedDestinations />
      <Categories />
    </div>
  )
}

/** Big heading + "where to?" search. Search opens the search page (Day 7) with the destination filled in. */
function Hero() {
  const navigate = useNavigate()
  const destinations = useQuery(destinationsQuery(false))
  const [destination, setDestination] = useState('any')

  const national = destinations.data?.filter((d) => !d.isInternational) ?? []
  const international = destinations.data?.filter((d) => d.isInternational) ?? []

  const onSearch = (event: FormEvent) => {
    event.preventDefault()
    navigate(destination === 'any' ? '/packages' : `/packages?destination=${encodeURIComponent(destination)}`)
  }

  return (
    <section className="rounded-2xl bg-gradient-to-br from-primary to-primary/80 px-6 py-12 text-primary-foreground sm:px-10 sm:py-16">
      <div className="mx-auto grid max-w-2xl gap-6 text-center">
        <div className="grid gap-3">
          <h1 className="text-3xl font-semibold tracking-tight sm:text-5xl">Your next trip starts here</h1>
          <p className="text-primary-foreground/80 sm:text-lg">
            {site.tagline} - pick a date, book your seats and pay online.
          </p>
        </div>

        <form onSubmit={onSearch} className="flex flex-col gap-2 rounded-xl bg-background p-2 text-foreground shadow-lg sm:flex-row">
          <Select value={destination} onValueChange={setDestination}>
            <SelectTrigger className="h-10 flex-1 border-0 shadow-none" aria-label="Where to?">
              <MapPinIcon className="text-muted-foreground" />
              <SelectValue placeholder="Where to?" />
            </SelectTrigger>
            <SelectContent position="popper" className="max-h-72">
              <SelectItem value="any">Anywhere</SelectItem>
              {national.length > 0 && <SelectSeparator />}
              {national.map((d) => (
                <SelectItem key={d.id} value={d.slug}>
                  {d.name}
                </SelectItem>
              ))}
              {international.length > 0 && <SelectSeparator />}
              {international.map((d) => (
                <SelectItem key={d.id} value={d.slug}>
                  {d.name} <span className="text-muted-foreground">· {d.countryName}</span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button type="submit" size="lg" className="h-10">
            <SearchIcon />
            Search tours
          </Button>
        </form>
      </div>
    </section>
  )
}

/** Featured first, then the newest - the search's "Recommended" order, first 8. */
function PopularPackages() {
  const { data, isPending, isError, refetch } = useQuery(packagesQuery({ sort: 'Recommended', pageSize: popularCount }))

  return (
    <section className="grid gap-4">
      <SectionHeading title="Popular packages" link={{ to: '/packages', label: 'See all packages' }} />

      {isError && (
        <div className="rounded-lg border py-8 text-center text-sm">
          <p className="text-muted-foreground">Packages couldn't be loaded.</p>
          <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
            Try again
          </Button>
        </div>
      )}

      {data?.items.length === 0 && (
        <p className="rounded-lg border py-8 text-center text-sm text-muted-foreground">New packages are on their way - check back soon.</p>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {isPending && Array.from({ length: 4 }, (_, i) => <PackageCardSkeleton key={i} />)}
        {data?.items.map((pkg) => <PackageCard key={pkg.id} pkg={pkg} />)}
      </div>
    </section>
  )
}

/** The plan's hardcoded banner (a banner editor is Phase 2): it advertises the flexible stays. */
function FlexibleStayBanner() {
  return (
    <section className="flex flex-col items-start gap-4 rounded-2xl border bg-muted/50 p-6 sm:flex-row sm:items-center sm:justify-between sm:p-8">
      <div className="flex gap-4">
        <span className="flex size-12 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
          <MoonIcon />
        </span>
        <div className="grid gap-1">
          <h2 className="text-lg font-semibold">Your dates, your nights</h2>
          <p className="text-sm text-muted-foreground">
            Flexible stays: choose when you start and how many nights you stay - the price follows.
          </p>
        </div>
      </div>
      <Button asChild variant="outline">
        <Link to="/packages?mode=FlexibleStay">
          See flexible stays
          <ArrowRightIcon />
        </Link>
      </Button>
    </section>
  )
}

/** The destinations marked "Show on home page" in the admin. Hidden when there are none. */
function FeaturedDestinations() {
  const { data, isPending } = useQuery(destinationsQuery(true))
  if (!isPending && (data?.length ?? 0) === 0) return null

  return (
    <section className="grid gap-4">
      <SectionHeading title="Popular destinations" />
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        {isPending && Array.from({ length: 4 }, (_, i) => <Skeleton key={i} className="aspect-[4/3] rounded-xl" />)}
        {data?.map((d) => (
          <Link
            key={d.id}
            to={`/packages?destination=${encodeURIComponent(d.slug)}`}
            className="group relative aspect-[4/3] overflow-hidden rounded-xl bg-muted focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
          >
            {d.imageUrl && (
              <img src={d.imageUrl} alt="" loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-105" />
            )}
            {/* Dark fade at the bottom so white text stays readable on any photo. */}
            <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent" />
            <div className="absolute inset-x-0 bottom-0 p-3 text-white">
              <p className="font-semibold">{d.name}</p>
              <p className="text-xs text-white/80">{d.countryName}</p>
            </div>
          </Link>
        ))}
      </div>
    </section>
  )
}

/** Beach, Hill, Honeymoon... each opens the search filtered to it. */
function Categories() {
  const { data } = useQuery(categoriesQuery)
  if (!data || data.length === 0) return null

  return (
    <section className="grid gap-4">
      <SectionHeading title="Find your kind of trip" />
      <div className="flex flex-wrap gap-2">
        {data.map((c) => (
          <Button key={c.id} asChild variant="outline" className="rounded-full">
            <Link to={`/packages?category=${encodeURIComponent(c.slug)}`}>
              <CategoryIcon name={c.icon} />
              {c.name}
            </Link>
          </Button>
        ))}
      </div>
    </section>
  )
}

function SectionHeading({ title, link }: { title: string; link?: { to: string; label: string } }) {
  return (
    <div className="flex items-end justify-between gap-4">
      <h2 className="text-xl font-semibold tracking-tight sm:text-2xl">{title}</h2>
      {link && (
        <Button asChild variant="link" className="px-0">
          <Link to={link.to}>
            {link.label}
            <ArrowRightIcon />
          </Link>
        </Button>
      )}
    </div>
  )
}
