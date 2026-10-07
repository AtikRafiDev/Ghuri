import { useQuery } from '@tanstack/react-query'
import {
  ArrowDownIcon,
  CalendarDaysIcon,
  CheckIcon,
  ChevronLeftIcon,
  CircleCheckIcon,
  CircleXIcon,
  MoonIcon,
  SparklesIcon,
  UserIcon,
  UsersIcon,
  XIcon,
} from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import { createPortal } from 'react-dom'
import { Link, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { CategoryIcon } from '@/features/admin/categories/components/CategoryIcon'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { PageHero } from '@/shared/components/PageHero'
import { describeDuration, formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { Reveal } from '@/shared/motion/Reveal'
import { useScrollTriggerRefresh } from '@/shared/motion/useScrollTriggerRefresh'
import { departuresQuery, packageDetailsQuery, type PackageDetails } from '../api/catalog.api'
import { BookingPanel } from '../components/BookingPanel'
import { PackageGallery } from '../components/PackageGallery'
import { PackageItinerary } from '../components/PackageItinerary'

const tourTypeLabels: Record<PackageDetails['tourType'], string> = { 1: 'Group tour', 2: 'Private tour', 3: 'Custom tour' }

/** The glass pills on the hero photo - the trip facts, the category links and "All packages". */
const heroChip =
  'inline-flex h-8 items-center gap-1.5 rounded-full bg-white/10 px-3 text-sm font-semibold text-white ring-1 ring-white/20 backdrop-blur-md transition-colors [&_svg]:size-4 [&_svg]:text-sun-300'

/**
 * The public package page, /packages/:slug (17-day plan, Day 7): photos,
 * itinerary, what's included, and the booking box - a departure picker
 * for fixed packages, check-in date + nights for flexible stays, both with
 * a live price.
 *
 * It opens with the package's own cover photo as a big hero (full-bleed
 * route) - title, trip facts and the starting price on top - and every
 * section below rises in as it scrolls into view. Loading and error states
 * keep a dark hero too: the see-through header's light text needs it.
 */
export function PackageDetailsPage() {
  const { slug = '' } = useParams()
  const { data: pkg, isPending, isError, error, refetch } = useQuery(packageDetailsQuery(slug))

  useDocumentMeta({ title: pkg ? pkg.title : isError ? 'Package not found' : 'Tour package' })

  if (isPending) return <PackageDetailsSkeleton />

  if (isError) {
    // 404 = never existed, or a draft/archived package - to a customer, both are "not available".
    return toAppError(error).status === 404 ? (
      <PageHero eyebrow="Tour package" title="This package isn’t available" text="It may have been removed or the link may be wrong.">
        <Button asChild variant="accent" size="lg">
          <Link to="/packages">See all packages</Link>
        </Button>
      </PageHero>
    ) : (
      <PageHero eyebrow="Tour package" title="This package couldn’t be loaded" text={toAppError(error).message}>
        <Button size="lg" variant="outline" onClick={() => refetch()}>
          Try again
        </Button>
      </PageHero>
    )
  }

  return <PackageView pkg={pkg} />
}

function PackageView({ pkg }: { pkg: PackageDetails }) {
  const isFlexible = pkg.pricingMode === 2
  const root = useRef<HTMLElement>(null)
  useScrollTriggerRefresh(root)
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
  const goToBooking = () => document.getElementById('book')?.scrollIntoView({ behavior: 'smooth' })

  return (
    <article ref={root}>
      <PageHero
        size="lg"
        imageUrl={pkg.imageUrls[0]}
        top={
          <Link to="/packages" className={cn(heroChip, 'hover:bg-white/20')}>
            <ChevronLeftIcon />
            All packages
          </Link>
        }
        eyebrow={
          <Link to={`/packages?destination=${encodeURIComponent(pkg.destinationSlug)}`} className="underline-offset-4 hover:text-white hover:underline">
            {pkg.destinationName}, {pkg.countryName}
          </Link>
        }
        title={pkg.title}
        text={pkg.summary}
      >
        <div className="grid gap-6">
          {/* Every chip is the same 32px glass pill, so the row reads as one even line however many there are. */}
          <ul className="flex flex-wrap items-center gap-2">
            <li className={heroChip}>
              {isFlexible ? <MoonIcon /> : <CalendarDaysIcon />}
              {describeDuration(pkg)}
            </li>
            <li className={heroChip}>
              {pkg.tourType === 2 ? <UserIcon /> : <UsersIcon />}
              {tourTypeLabels[pkg.tourType]}
            </li>
            {isFlexible && (
              <li className={cn(heroChip, 'bg-sun-500 text-forest-950 ring-0 [&_svg]:text-forest-950')}>
                <SparklesIcon />
                Pick your own dates
              </li>
            )}
            {pkg.minAge !== null && <li className={heroChip}>Age {pkg.minAge}+</li>}
            {pkg.categories.map((c) => (
              <li key={c.slug}>
                <Link to={`/packages?category=${encodeURIComponent(c.slug)}`} className={cn(heroChip, 'hover:bg-white/20')}>
                  <CategoryIcon name={c.icon} />
                  {c.name}
                </Link>
              </li>
            ))}
          </ul>
          <div className="flex flex-wrap items-center gap-x-6 gap-y-4">
            {priceFrom !== null && (
              <p className="flex items-baseline gap-2">
                <span className="text-xs font-bold tracking-wider text-forest-100/70 uppercase">From</span>
                <span className="text-3xl leading-none font-extrabold tracking-tight">{formatTaka(priceFrom)}</span>
                <span className="text-forest-100/80">/ person</span>
              </p>
            )}
            <Button variant="accent" size="lg" onClick={goToBooking}>
              {isFlexible ? 'Choose dates' : 'See dates'}
              <ArrowDownIcon className="group-hover/button:translate-y-0.5" />
            </Button>
          </div>
        </div>
      </PageHero>

      {/* Two columns on a laptop (content | booking box). On a phone the box comes after the content. */}
      <div className="mx-auto grid max-w-6xl gap-12 px-4 py-12 sm:py-16 lg:grid-cols-[minmax(0,1fr)_23rem] lg:items-start">
        <div className="grid gap-14">
          <PackageGallery images={pkg.imageUrls} title={pkg.title} />

          {/* The summary is already in the hero - only a longer description earns its own section. */}
          {pkg.description && (
            <Section eyebrow="Overview" title="About this trip">
              <p className="text-lg leading-relaxed whitespace-pre-line text-ink-600">{pkg.description}</p>
            </Section>
          )}

          {pkg.itinerary.length > 0 && (
            <Section eyebrow="Day by day" title="Itinerary" aside={`${pkg.itinerary.length} day${pkg.itinerary.length === 1 ? '' : 's'}`}>
              <PackageItinerary days={pkg.itinerary} />
            </Section>
          )}

          {(pkg.inclusions.length > 0 || pkg.exclusions.length > 0) && (
            <Section eyebrow="Good to know" title="What’s included">
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
            <Section eyebrow="Before you book" title="Terms & policy">
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
              <Button onClick={goToBooking}>
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

/**
 * A content block in the home page's style: a small eyebrow, the title (with an optional quiet note on
 * the right, e.g. "3 days"), then the content. Heading and content rise in as they scroll into view.
 */
function Section({ eyebrow, title, aside, children }: { eyebrow: string; title: string; aside?: string; children: ReactNode }) {
  return (
    <section>
      <Reveal y={32} className="grid gap-5">
        <div className="grid gap-2">
          <p className="flex items-center gap-2 text-xs font-bold tracking-[0.18em] text-forest-600 uppercase">
            <span aria-hidden className="h-px w-8 bg-forest-500" />
            {eyebrow}
          </p>
          <div className="flex items-baseline justify-between gap-4">
            <h2 className="text-2xl leading-tight font-extrabold tracking-tight text-ink-900 sm:text-3xl">{title}</h2>
            {aside && <span className="text-sm font-medium text-ink-500">{aside}</span>}
          </div>
        </div>
        {children}
      </Reveal>
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
 * The page's shape while it loads, so nothing jumps when the content arrives: a dark hero the size of the
 * real one (the see-through header's light text needs it) with pale bars where the title and facts go.
 */
function PackageDetailsSkeleton() {
  const bar = 'animate-pulse rounded-full bg-white/10'
  return (
    <div aria-busy="true" aria-label="Loading package">
      <div className="brand-surface relative -mt-[calc(4rem+1px)] flex min-h-[78svh] bg-forest-950">
        <div className="mx-auto grid w-full max-w-6xl content-end gap-5 px-4 pt-28 pb-12 sm:pt-32 sm:pb-16">
          <div className={cn(bar, 'h-8 w-36')} />
          <div className={cn(bar, 'h-3 w-48')} />
          <div className={cn(bar, 'h-12 w-full max-w-2xl rounded-2xl sm:h-16')} />
          <div className={cn(bar, 'h-5 w-full max-w-xl')} />
          <div className="flex gap-2">
            <div className={cn(bar, 'h-8 w-32')} />
            <div className={cn(bar, 'h-8 w-24')} />
            <div className={cn(bar, 'h-8 w-20')} />
          </div>
        </div>
      </div>
      <div className="mx-auto grid max-w-6xl gap-12 px-4 py-12 sm:py-16 lg:grid-cols-[minmax(0,1fr)_23rem]">
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
