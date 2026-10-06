import { useQuery } from '@tanstack/react-query'
import {
  CalendarDaysIcon,
  CheckIcon,
  ChevronLeftIcon,
  CircleCheckIcon,
  CircleXIcon,
  CloudOffIcon,
  LuggageIcon,
  MapPinIcon,
  MoonIcon,
  SparklesIcon,
  UserIcon,
  UsersIcon,
  XIcon,
  type LucideIcon,
} from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import { createPortal } from 'react-dom'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { CategoryIcon } from '@/features/admin/categories/components/CategoryIcon'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { describeDuration, formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { departuresQuery, packageDetailsQuery, type PackageDetails } from '../api/catalog.api'
import { BookingPanel } from '../components/BookingPanel'
import { PackageGallery } from '../components/PackageGallery'
import { PackageItinerary } from '../components/PackageItinerary'

const tourTypeLabels: Record<PackageDetails['tourType'], string> = { 1: 'Group tour', 2: 'Private tour', 3: 'Custom tour' }

/**
 * The public package page, /packages/:slug (17-day plan, Day 7): photos,
 * itinerary, what's included, and the booking box - a departure picker
 * for fixed packages, check-in date + nights for flexible stays, both with
 * a live price.
 */
export function PackageDetailsPage() {
  const { slug = '' } = useParams()
  const { data: pkg, isPending, isError, error, refetch } = useQuery(packageDetailsQuery(slug))

  useDocumentMeta(
    pkg
      ? { title: pkg.seoTitle, description: pkg.seoDescription, image: pkg.imageUrls[0] }
      : { title: isError ? 'Package not found' : 'Tour package' },
  )

  if (isPending) return <PackageDetailsSkeleton />

  if (isError) {
    // 404 = never existed, or a draft/archived package - to a customer, both are "not available".
    return toAppError(error).status === 404 ? (
      <Message icon={LuggageIcon} title="This package isn’t available" text="It may have been removed or the link may be wrong.">
        <Button asChild>
          <Link to="/packages">See all packages</Link>
        </Button>
      </Message>
    ) : (
      <Message icon={CloudOffIcon} title="This package couldn’t be loaded" text={toAppError(error).message}>
        <Button variant="outline" onClick={() => refetch()}>
          Try again
        </Button>
      </Message>
    )
  }

  return <PackageView pkg={pkg} />
}

function PackageView({ pkg }: { pkg: PackageDetails }) {
  const isFlexible = pkg.pricingMode === 2
  const bookingRef = useRef<HTMLElement>(null)
  const bookingInView = useHasReached(bookingRef)

  // Same cache entry the booking box uses: one request, not two.
  const departures = useQuery({ ...departuresQuery(pkg.slug), enabled: !isFlexible })
  const openDates = departures.data?.filter((d) => d.seatsLeft > 0) ?? []
  const priceFrom = isFlexible
    ? pkg.basePrice
    : openDates.length > 0
      ? Math.min(...openDates.map((d) => d.adultPrice))
      : null

  return (
    <article className="grid gap-8">
      <header className="grid animate-fade-up gap-4">
        <Button asChild variant="ghost" size="sm" className="-ml-3 w-fit">
          <Link to="/packages">
            <ChevronLeftIcon />
            All packages
          </Link>
        </Button>

        <div className="grid gap-3">
          <Link
            to={`/packages?destination=${encodeURIComponent(pkg.destinationSlug)}`}
            className="flex w-fit items-center gap-1.5 text-sm font-semibold text-forest-600 underline-offset-4 hover:text-forest-800 hover:underline"
          >
            <MapPinIcon className="size-4" />
            {pkg.destinationName}, {pkg.countryName}
          </Link>
          <h1 className="text-3xl leading-tight font-bold text-ink-900 sm:text-4xl">{pkg.title}</h1>
          {/* Every badge is the same 24px pill, so the row reads as one even line however many there are. */}
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="secondary">
              {isFlexible ? <MoonIcon /> : <CalendarDaysIcon />}
              {describeDuration(pkg)}
            </Badge>
            <Badge variant="secondary">
              {pkg.tourType === 2 ? <UserIcon /> : <UsersIcon />}
              {tourTypeLabels[pkg.tourType]}
            </Badge>
            {isFlexible && (
              <Badge variant="warning">
                <SparklesIcon />
                Pick your own dates
              </Badge>
            )}
            {pkg.minAge !== null && <Badge variant="outline">Age {pkg.minAge}+</Badge>}
            {pkg.categories.map((c) => (
              <Badge key={c.slug} variant="outline" asChild>
                <Link to={`/packages?category=${encodeURIComponent(c.slug)}`}>
                  <CategoryIcon name={c.icon} />
                  {c.name}
                </Link>
              </Badge>
            ))}
          </div>
        </div>
      </header>

      {/* Two columns on a laptop (content | booking box). On a phone the box comes after the content. */}
      <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_23rem] lg:items-start">
        <div className="grid gap-10">
          <PackageGallery images={pkg.imageUrls} title={pkg.title} />

          <Section title="About this trip">
            <p className="leading-relaxed whitespace-pre-line text-ink-600">{pkg.description ?? pkg.summary}</p>
          </Section>

          {pkg.itinerary.length > 0 && (
            <Section title="Itinerary" aside={`${pkg.itinerary.length} day${pkg.itinerary.length === 1 ? '' : 's'}`}>
              <PackageItinerary days={pkg.itinerary} />
            </Section>
          )}

          {(pkg.inclusions.length > 0 || pkg.exclusions.length > 0) && (
            <Section title="What’s included">
              {/*
                One card, two columns. items-start + identical column markup: both headings sit on the same
                line and both lists start at the same height with the same row spacing, whatever their lengths.
              */}
              <div className="grid items-start gap-6 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:grid-cols-2 sm:gap-0 sm:p-6">
                <IncludeList items={pkg.inclusions} included />
                <IncludeList items={pkg.exclusions} included={false} />
              </div>
            </Section>
          )}

          {pkg.termsAndPolicy && (
            <Section title="Terms & policy">
              <p className="rounded-2xl bg-ink-100/60 p-4 text-sm leading-relaxed whitespace-pre-line text-ink-600 ring-1 ring-ink-200/70 ring-inset sm:p-5">
                {pkg.termsAndPolicy}
              </p>
            </Section>
          )}
        </div>

        {/*
          scroll-mt: when the phone bar scrolls here, leave room for the sticky site header above the box.
          Laptop: the box stays in view just below the site header. On a short screen it may be taller than
          the space left, so it gets its own scroll (max-h + overflow) - "Book now" can always be reached.
          The negative margins + padding give the card's shadow room inside that scroll area.
        */}
        <aside
          id="book"
          ref={bookingRef}
          className="scroll-mt-20 lg:sticky lg:top-17 lg:-mx-3 lg:-mt-3 lg:max-h-[calc(100svh-4.25rem)] lg:overflow-y-auto lg:px-3 lg:pt-3 lg:pb-4"
        >
          <BookingPanel pkg={pkg} />
        </aside>
      </div>

      {/*
        Phone only: the price and a button that jumps to the booking box. Hides once the box is on screen.
        Rendered straight into <body> (a portal): an animated parent (the layout's <main> fades up) can
        otherwise pin a "fixed" bar to itself instead of to the screen. z-30 keeps the WhatsApp button (z-40) on top.
      */}
      {!bookingInView &&
        createPortal(
          <div className="fixed inset-x-0 bottom-0 z-30 animate-fade-up border-t border-ink-200/80 bg-card/90 px-4 py-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] shadow-pop backdrop-blur-xl lg:hidden">
            {/* pr-18: the round WhatsApp button floats over the bottom-right corner - keep "See dates" clear of it. */}
            <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 pr-18">
              <div className="grid min-w-0">
                {priceFrom !== null ? (
                  <>
                    <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">From</span>
                    <span className="flex items-baseline gap-1">
                      <span className="text-lg leading-tight font-bold text-ink-900">{formatTaka(priceFrom)}</span>
                      <span className="text-xs text-ink-500">/ person</span>
                    </span>
                  </>
                ) : (
                  <span className="text-sm font-semibold text-ink-700">{describeDuration(pkg)}</span>
                )}
              </div>
              <Button onClick={() => document.getElementById('book')?.scrollIntoView({ behavior: 'smooth' })}>
                {isFlexible ? 'Choose dates' : 'See dates'}
              </Button>
            </div>
          </div>,
          document.body,
        )}
    </article>
  )
}

/**
 * true once the element is on screen OR the page has scrolled past it. The
 * booking box is the last thing before the footer, so this also keeps the
 * phone bar from covering the footer.
 */
function useHasReached(ref: RefObject<HTMLElement | null>): boolean {
  const [reached, setReached] = useState(false)
  useEffect(() => {
    const element = ref.current
    if (!element) return
    const observer = new IntersectionObserver(([entry]) =>
      setReached(entry.isIntersecting || entry.boundingClientRect.top < 0),
    )
    observer.observe(element)
    return () => observer.disconnect()
  }, [ref])
  return reached
}

/** A content block: its title (with an optional quiet note on the right, e.g. "3 days"), then the content. */
function Section({ title, aside, children }: { title: string; aside?: string; children: ReactNode }) {
  return (
    <section className="grid gap-4">
      <div className="flex items-baseline justify-between gap-4">
        <h2 className="text-xl font-bold text-ink-900 sm:text-2xl">{title}</h2>
        {aside && <span className="text-sm font-medium text-ink-500">{aside}</span>}
      </div>
      {children}
    </section>
  )
}

/**
 * One column of "What's included". Both columns use exactly this markup -
 * a 32px heading row, then rows of 20px icon discs beside 20px-tall text
 * with the same gap - so the two lists keep one rhythm side by side.
 * A tick in forest green for included, a cross in terracotta for not included.
 */
function IncludeList({ items, included }: { items: string[]; included: boolean }) {
  if (items.length === 0) return null
  const Icon = included ? CheckIcon : XIcon
  return (
    <div className={cn('grid content-start gap-4', !included && 'sm:border-l sm:border-ink-200/80 sm:pl-6', included && 'sm:pr-6')}>
      <h3 className="flex h-8 items-center gap-2.5 text-sm font-bold text-ink-900">
        <span
          className={cn(
            'flex size-8 items-center justify-center rounded-xl',
            included ? 'bg-forest-50 text-forest-600' : 'bg-clay-50 text-clay-600',
          )}
        >
          {included ? <CircleCheckIcon className="size-4" /> : <CircleXIcon className="size-4" />}
        </span>
        {included ? 'Included' : 'Not included'}
      </h3>
      <ul className="grid content-start gap-3 text-sm">
        {items.map((item) => (
          <li key={item} className="flex items-start gap-3">
            <span
              aria-hidden
              className={cn(
                'flex size-5 shrink-0 items-center justify-center rounded-full',
                included ? 'bg-forest-100 text-forest-700' : 'bg-clay-50 text-clay-600',
              )}
            >
              <Icon className="size-3" strokeWidth={3} />
            </span>
            <span className={cn('leading-5', included ? 'text-ink-700' : 'text-ink-500')}>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * "Not available" / "couldn't be loaded" in place of the whole page. Same look as EmptyState (icon in a
 * soft halo), but its title is the page's h1 - it's the only heading on the page.
 */
function Message({ icon: Icon, title, text, children }: { icon: LucideIcon; title: string; text: string; children: ReactNode }) {
  return (
    <section className="mx-auto grid max-w-md animate-fade-up justify-items-center gap-3 py-16 text-center">
      <span className="flex size-16 items-center justify-center rounded-2xl bg-forest-50 text-forest-600 ring-8 ring-forest-50/50">
        <Icon className="size-7" />
      </span>
      <h1 className="mt-3 text-2xl font-bold text-ink-900">{title}</h1>
      <p className="text-ink-500">{text}</p>
      <div className="mt-3 flex flex-wrap justify-center gap-2">{children}</div>
    </section>
  )
}

/** The page's shape while it loads, so nothing jumps when the content arrives. */
function PackageDetailsSkeleton() {
  return (
    <div className="grid gap-8" aria-busy="true" aria-label="Loading package">
      <div className="grid gap-4">
        <Skeleton className="h-9 w-32 rounded-lg" />
        <div className="grid gap-3">
          <Skeleton className="h-4 w-40" />
          <Skeleton className="h-10 w-3/4" />
          <div className="flex gap-2">
            <Skeleton className="h-6 w-32 rounded-full" />
            <Skeleton className="h-6 w-24 rounded-full" />
            <Skeleton className="h-6 w-20 rounded-full" />
          </div>
        </div>
      </div>
      <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_23rem]">
        <div className="grid gap-4">
          <Skeleton className="aspect-[16/9] w-full rounded-3xl" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
        </div>
        <Skeleton className="h-[30rem] rounded-3xl" />
      </div>
    </div>
  )
}
