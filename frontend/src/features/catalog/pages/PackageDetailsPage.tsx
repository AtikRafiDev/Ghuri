import { useQuery } from '@tanstack/react-query'
import { CalendarDaysIcon, CheckIcon, ChevronLeftIcon, MapPinIcon, MoonIcon, UserIcon, UsersIcon, XIcon } from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import { Link, useParams } from 'react-router'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
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
      <Message title="This package isn’t available" text="It may have been removed or the link may be wrong.">
        <Button asChild>
          <Link to="/packages">See all packages</Link>
        </Button>
      </Message>
    ) : (
      <Message title="This package couldn’t be loaded" text={toAppError(error).message}>
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
    <article className="grid gap-6">
      <Link to="/packages" className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ChevronLeftIcon className="size-4" />
        All packages
      </Link>

      <header className="grid gap-3">
        <Link
          to={`/packages?destination=${encodeURIComponent(pkg.destinationSlug)}`}
          className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground hover:underline"
        >
          <MapPinIcon className="size-4" />
          {pkg.destinationName}, {pkg.countryName}
        </Link>
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{pkg.title}</h1>
        <div className="flex flex-wrap gap-2">
          <Badge variant="secondary" className="gap-1">
            {isFlexible ? <MoonIcon /> : <CalendarDaysIcon />}
            {describeDuration(pkg)}
          </Badge>
          <Badge variant="secondary" className="gap-1">
            {pkg.tourType === 2 ? <UserIcon /> : <UsersIcon />}
            {tourTypeLabels[pkg.tourType]}
          </Badge>
          {isFlexible && <Badge variant="secondary">Pick your own dates</Badge>}
          {pkg.minAge !== null && <Badge variant="outline">Age {pkg.minAge}+</Badge>}
          {pkg.categories.map((c) => (
            <Badge key={c.slug} variant="outline" asChild>
              <Link to={`/packages?category=${encodeURIComponent(c.slug)}`}>{c.name}</Link>
            </Badge>
          ))}
        </div>
      </header>

      {/* Two columns on a laptop (content | booking box). On a phone the box comes after the content. */}
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem] lg:items-start">
        <div className="grid gap-8">
          <PackageGallery images={pkg.imageUrls} title={pkg.title} />

          <Section title="About this trip">
            <p className="whitespace-pre-line text-muted-foreground">{pkg.description ?? pkg.summary}</p>
          </Section>

          {pkg.itinerary.length > 0 && (
            <Section title="Itinerary">
              <PackageItinerary days={pkg.itinerary} />
            </Section>
          )}

          {(pkg.inclusions.length > 0 || pkg.exclusions.length > 0) && (
            <Section title="What’s included">
              <div className="grid gap-6 sm:grid-cols-2">
                <IncludeList items={pkg.inclusions} included />
                <IncludeList items={pkg.exclusions} included={false} />
              </div>
            </Section>
          )}

          {pkg.termsAndPolicy && (
            <Section title="Terms & policy">
              <p className="text-sm whitespace-pre-line text-muted-foreground">{pkg.termsAndPolicy}</p>
            </Section>
          )}
        </div>

        {/* scroll-mt: when the phone bar scrolls here, leave a little space above the box. */}
        <aside id="book" ref={bookingRef} className="scroll-mt-4 lg:sticky lg:top-4">
          <BookingPanel pkg={pkg} />
        </aside>
      </div>

      {/* Phone only: the price and a button that jumps to the booking box. Hides once the box is on screen. */}
      {!bookingInView && (
        <div className="fixed inset-x-0 bottom-0 z-40 border-t bg-background/95 p-3 backdrop-blur lg:hidden">
          <div className="mx-auto flex max-w-6xl items-center justify-between gap-3">
            <div className="text-sm">
              {priceFrom !== null ? (
                <>
                  <span className="text-muted-foreground">from </span>
                  <span className="text-lg font-semibold">{formatTaka(priceFrom)}</span>
                  <span className="text-muted-foreground"> / person</span>
                </>
              ) : (
                <span className="text-muted-foreground">{describeDuration(pkg)}</span>
              )}
            </div>
            <Button size="lg" onClick={() => document.getElementById('book')?.scrollIntoView({ behavior: 'smooth' })}>
              {isFlexible ? 'Choose dates' : 'See dates'}
            </Button>
          </div>
        </div>
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

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="grid gap-3">
      <h2 className="text-xl font-semibold tracking-tight">{title}</h2>
      {children}
    </section>
  )
}

function IncludeList({ items, included }: { items: string[]; included: boolean }) {
  if (items.length === 0) return null
  const Icon = included ? CheckIcon : XIcon
  return (
    <div className="grid gap-2">
      <h3 className="text-sm font-medium">{included ? 'Included' : 'Not included'}</h3>
      <ul className="grid gap-1.5 text-sm">
        {items.map((item) => (
          <li key={item} className="flex gap-2">
            <Icon className={included ? 'mt-0.5 size-4 shrink-0 text-emerald-600' : 'mt-0.5 size-4 shrink-0 text-muted-foreground'} />
            <span className={included ? '' : 'text-muted-foreground'}>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

function Message({ title, text, children }: { title: string; text: string; children: ReactNode }) {
  return (
    <section className="grid justify-items-center gap-3 py-16 text-center">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-muted-foreground">{text}</p>
      <div className="mt-2">{children}</div>
    </section>
  )
}

/** The page's shape while it loads, so nothing jumps when the content arrives. */
function PackageDetailsSkeleton() {
  return (
    <div className="grid gap-6" aria-busy="true" aria-label="Loading package">
      <Skeleton className="h-4 w-24" />
      <div className="grid gap-3">
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-8 w-3/4" />
        <Skeleton className="h-5 w-64" />
      </div>
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="grid gap-4">
          <Skeleton className="aspect-[16/9] w-full rounded-xl" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-5/6" />
        </div>
        <Skeleton className="h-96 rounded-xl" />
      </div>
    </div>
  )
}
