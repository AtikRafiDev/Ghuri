import { useQuery } from '@tanstack/react-query'
import {
  ArrowRightIcon,
  ArrowUpRightIcon,
  CloudOffIcon,
  CompassIcon,
  LockIcon,
  LuggageIcon,
  MapPinIcon,
  MessageCircleIcon,
  MoonIcon,
  MountainIcon,
  SearchIcon,
  SunIcon,
  type LucideIcon,
} from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { CategoryIcon } from '@/features/admin/categories/components/CategoryIcon'
import { categoriesQuery, destinationsQuery, packagesQuery } from '@/features/catalog/api/catalog.api'
import { PackageCard, PackageCardSkeleton } from '@/features/catalog/components/PackageCard'
import { EmptyState } from '@/shared/components/EmptyState'
import { site } from '@/shared/config/site'

const popularCount = 8

/** The three promises under the hero search - what every booking comes with. */
const promises = [
  { icon: LockIcon, text: 'Secure online payment' },
  { icon: CompassIcon, text: 'Local guides' },
  { icon: MessageCircleIcon, text: 'Help on WhatsApp' },
]

/**
 * The home page (17-day plan, Day 6): hero search, popular packages, a
 * banner, destinations and categories. Three small requests run in
 * parallel - each list is cached on its own and reused by later pages.
 */
export function HomePage() {
  return (
    <div className="grid gap-14 sm:gap-20">
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
    <section className="brand-surface relative isolate overflow-hidden rounded-[2rem] bg-gradient-to-br from-forest-700 via-forest-800 to-forest-950 px-5 py-14 text-white shadow-lift sm:px-10 sm:py-20 lg:py-24">
      {/* Decoration only: contour lines, two soft glows, and two postcards floating either side on wide screens. */}
      <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
      <div aria-hidden className="absolute -top-32 -right-24 -z-10 size-96 rounded-full bg-sun-500/10 blur-3xl" />
      <div aria-hidden className="absolute -bottom-36 -left-24 -z-10 size-[28rem] rounded-full bg-forest-400/25 blur-3xl" />
      <FloatingPostcard icon={SunIcon} title="Sunny escapes" text="Beaches & islands" className="top-16 left-10 -rotate-6" />
      <FloatingPostcard icon={MountainIcon} title="Into the hills" text="Treks & tea gardens" className="right-8 bottom-12 rotate-6 [animation-delay:-3s]" />

      <div className="mx-auto grid max-w-2xl justify-items-center gap-7 text-center">
        <span className="inline-flex h-8 items-center gap-2 rounded-full bg-white/10 px-3.5 text-xs font-semibold text-forest-50 ring-1 ring-white/15 backdrop-blur-sm sm:text-sm">
          <span aria-hidden className="size-1.5 rounded-full bg-sun-300" />
          {site.tagline}
        </span>

        <div className="grid gap-4">
          <h1 className="text-[2.5rem] leading-[1.05] font-extrabold tracking-tight sm:text-6xl">
            Your next trip <span className="text-sun-300">starts here</span>
          </h1>
          <p className="mx-auto max-w-md text-forest-100/80 sm:text-lg">Pick a date, book your seats and pay online - we arrange the rest.</p>
        </div>

        {/* One white bar: the destination field grows, the button keeps its size - both 48px tall, edges level. */}
        <form
          onSubmit={onSearch}
          role="search"
          className="grid w-full gap-2 rounded-[1.375rem] bg-white p-2 text-ink-900 shadow-pop ring-1 ring-white/40 sm:flex sm:items-center"
        >
          <div className="relative min-w-0 flex-1">
            <MapPinIcon className="pointer-events-none absolute top-1/2 left-4 z-10 size-5 -translate-y-1/2 text-forest-600" />
            <Select value={destination} onValueChange={setDestination}>
              <SelectTrigger
                className="w-full border-transparent bg-ink-50 pl-12 text-[0.9375rem] font-semibold shadow-none hover:border-forest-200 hover:bg-forest-50/60 data-[size=default]:h-12"
                aria-label="Where to?"
              >
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
          </div>
          <Button type="submit" size="lg" className="sm:px-7">
            <SearchIcon />
            Search tours
          </Button>
        </form>

        <ul className="flex flex-wrap items-center justify-center gap-x-6 gap-y-2 text-sm text-forest-100/85">
          {promises.map(({ icon: Icon, text }) => (
            <li key={text} className="flex items-center gap-2">
              <Icon className="size-4 text-sun-300" />
              {text}
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}

/** A frosted "postcard" drifting gently beside the hero text - wide screens only, purely decorative. */
function FloatingPostcard({ icon: Icon, title, text, className }: { icon: LucideIcon; title: string; text: string; className: string }) {
  return (
    <div aria-hidden className={`absolute hidden animate-float xl:block ${className}`}>
      <div className="flex items-center gap-3 rounded-2xl bg-white/10 py-2.5 pr-4 pl-2.5 ring-1 ring-white/15 backdrop-blur-md">
        <span className="flex size-10 items-center justify-center rounded-xl bg-sun-500 text-forest-950">
          <Icon className="size-5" />
        </span>
        <span className="grid text-left">
          <span className="text-sm font-bold text-white">{title}</span>
          <span className="text-xs text-forest-100/75">{text}</span>
        </span>
      </div>
    </div>
  )
}

/** Featured first, then the newest - the search's "Recommended" order, first 8. */
function PopularPackages() {
  const { data, isPending, isError, refetch } = useQuery(packagesQuery({ sort: 'Recommended', pageSize: popularCount }))

  return (
    <section className="grid gap-6">
      <SectionHeading
        eyebrow="Hand-picked"
        title="Popular packages"
        text="Featured trips first, then the newest - group departures and flexible stays."
        link={{ to: '/packages', label: 'See all packages' }}
      />

      {isError && (
        <EmptyState icon={CloudOffIcon} title="Packages couldn't be loaded" text="Check your connection and try again.">
          <Button variant="outline" onClick={() => refetch()}>
            Try again
          </Button>
        </EmptyState>
      )}

      {data?.items.length === 0 && (
        <EmptyState icon={LuggageIcon} title="New packages are on their way" text="Check back soon - or tell us where you'd like to go." />
      )}

      <div className="stagger grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {isPending && Array.from({ length: 4 }, (_, i) => <PackageCardSkeleton key={i} />)}
        {data?.items.map((pkg) => <PackageCard key={pkg.id} pkg={pkg} />)}
      </div>
    </section>
  )
}

/** Night sky for "your nights": scattered stars (left %, top %, size class, opacity class). */
const stars = [
  ['8%', '22%', 'size-1', 'opacity-70'],
  ['18%', '70%', 'size-1.5', 'opacity-50'],
  ['31%', '15%', 'size-1', 'opacity-60'],
  ['46%', '78%', 'size-1', 'opacity-40'],
  ['58%', '24%', 'size-1.5', 'opacity-60'],
  ['67%', '62%', 'size-1', 'opacity-50'],
  ['79%', '18%', 'size-1', 'opacity-70'],
  ['92%', '74%', 'size-1.5', 'opacity-40'],
] as const

/** The plan's hardcoded banner (a banner editor is Phase 2): it advertises the flexible stays. */
function FlexibleStayBanner() {
  return (
    <section className="brand-surface relative isolate overflow-hidden rounded-[2rem] bg-gradient-to-r from-forest-950 via-forest-900 to-forest-700 p-6 text-white shadow-lift sm:p-10">
      <div aria-hidden className="bg-topo absolute inset-0 -z-10 opacity-60" />
      <div aria-hidden className="absolute top-1/2 -right-16 -z-10 size-72 -translate-y-1/2 rounded-full bg-sun-500/15 blur-3xl" />
      {stars.map(([left, top, size, opacity]) => (
        <span key={left} aria-hidden className={`absolute -z-10 rounded-full bg-sun-300 ${size} ${opacity}`} style={{ left, top }} />
      ))}

      <div className="flex flex-col items-start gap-6 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center">
          <span className="relative flex size-16 shrink-0 items-center justify-center rounded-2xl bg-white/10 ring-1 ring-white/15">
            <span aria-hidden className="absolute inset-2 rounded-full bg-sun-300/25 blur-md" />
            <MoonIcon className="relative size-7 fill-sun-300 text-sun-300" />
          </span>
          <div className="grid gap-1.5">
            <p className="text-[0.6875rem] font-semibold tracking-wider text-sun-300 uppercase">Flexible stays</p>
            <h2 className="text-2xl font-bold sm:text-3xl">Your dates, your nights</h2>
            <p className="max-w-lg text-sm text-forest-100/80 sm:text-base">
              Choose when you start and how many nights you stay - the price follows.
            </p>
          </div>
        </div>
        <Button asChild variant="accent" size="lg" className="shrink-0">
          <Link to="/packages?mode=FlexibleStay">
            See flexible stays
            <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
          </Link>
        </Button>
      </div>
    </section>
  )
}

/** The destinations marked "Show on home page" in the admin. Hidden when there are none. */
function FeaturedDestinations() {
  const { data, isPending } = useQuery(destinationsQuery(true))
  if (!isPending && (data?.length ?? 0) === 0) return null

  return (
    <section className="grid gap-6">
      <SectionHeading eyebrow="Where to next" title="Popular destinations" text="Pick a place to see every tour that goes there." />
      <div className="stagger grid grid-cols-2 gap-3 sm:gap-5 lg:grid-cols-4">
        {isPending && Array.from({ length: 4 }, (_, i) => <Skeleton key={i} className="aspect-[4/3] rounded-3xl" />)}
        {data?.map((d) => (
          <Link
            key={d.id}
            to={`/packages?destination=${encodeURIComponent(d.slug)}`}
            className="group relative isolate aspect-[4/3] overflow-hidden rounded-3xl bg-ink-100 shadow-card transition-shadow duration-300 hover:shadow-lift focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none"
          >
            {d.imageUrl && (
              <img
                src={d.imageUrl}
                alt=""
                loading="lazy"
                className="absolute inset-0 -z-10 size-full object-cover transition-transform duration-700 ease-(--ease-out-expo) group-hover:scale-110"
              />
            )}
            {/* A deep-forest fade at the bottom so the white name stays readable on any photo. */}
            <div className="absolute inset-0 -z-10 bg-gradient-to-t from-forest-950/85 via-forest-950/20 to-transparent transition-opacity duration-300 group-hover:opacity-90" />
            <div className="brand-surface absolute inset-x-0 bottom-0 flex items-end justify-between gap-2 p-3 text-white sm:p-4">
              <div className="grid min-w-0">
                <p className="truncate text-sm font-bold sm:text-base">{d.name}</p>
                <p className="flex items-center gap-1 text-xs text-forest-100/85">
                  <MapPinIcon className="size-3 shrink-0" />
                  <span className="truncate">{d.countryName}</span>
                </p>
              </div>
              <span
                aria-hidden
                className="brand-surface hidden size-8 shrink-0 translate-y-1 items-center justify-center rounded-full bg-white text-forest-800 opacity-0 transition-[opacity,translate] duration-300 group-hover:translate-y-0 group-hover:opacity-100 sm:flex"
              >
                <ArrowUpRightIcon className="size-4" />
              </span>
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
    <section className="grid gap-6">
      <SectionHeading eyebrow="Travel your way" title="Find your kind of trip" />
      <div className="flex flex-wrap gap-3">
        {data.map((c) => (
          <Link
            key={c.id}
            to={`/packages?category=${encodeURIComponent(c.slug)}`}
            className="group inline-flex h-12 items-center gap-3 rounded-full bg-card pr-5 pl-1.5 text-sm font-semibold text-ink-700 shadow-soft ring-1 ring-ink-200 transition-[translate,box-shadow,color] duration-300 ease-(--ease-out-expo) hover:-translate-y-0.5 hover:text-forest-800 hover:shadow-card hover:ring-forest-300 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none"
          >
            <span className="flex size-9 items-center justify-center rounded-full bg-forest-50 text-forest-600 transition-colors duration-300 group-hover:bg-primary group-hover:text-white">
              <CategoryIcon name={c.icon} className="size-4" />
            </span>
            {c.name}
          </Link>
        ))}
      </div>
    </section>
  )
}

/**
 * Every section starts the same way: a small coloured eyebrow, the title,
 * and an optional line under it. The "See all" link sits on the title's
 * row and shares its baseline (items-baseline), so it lines up the same in
 * every section, however long the text beneath is.
 */
function SectionHeading({ eyebrow, title, text, link }: { eyebrow: string; title: string; text?: string; link?: { to: string; label: string } }) {
  return (
    <div className="grid gap-2">
      <p className="text-[0.6875rem] font-semibold tracking-wider text-forest-600 uppercase">{eyebrow}</p>
      <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-2">
        <h2 className="text-2xl font-bold text-ink-900 sm:text-3xl">{title}</h2>
        {link && (
          <Button asChild variant="link" className="text-[0.9375rem]">
            <Link to={link.to}>
              {link.label}
              <ArrowRightIcon />
            </Link>
          </Button>
        )}
      </div>
      {text && <p className="text-ink-500">{text}</p>}
    </div>
  )
}
