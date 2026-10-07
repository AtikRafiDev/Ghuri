import { useQuery } from '@tanstack/react-query'
import { ArrowRightIcon, CompassIcon, LockIcon, MapPinIcon, MessageCircleIcon, PauseIcon, PlayIcon, SearchIcon } from 'lucide-react'
import { useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { categoriesQuery, destinationsQuery } from '@/features/catalog/api/catalog.api'
import { toPackageSearch } from '@/features/catalog/lib/packageSearch'
import { cn } from '@/lib/utils'
import { site } from '@/shared/config/site'
import { onBootSplashGone } from '@/shared/loader/bootSplash'
import { gsap, useGSAP } from '@/shared/motion/gsap'
import { SplitWords } from '@/shared/motion/SplitWords'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'
import { Photo } from '@/shared/photos/Photo'
import { heroPlaceholder, heroSlides } from '../data/photos'

/** The three promises under the search - what every booking comes with. */
const promises = [
  { icon: LockIcon, text: 'Secure online payment' },
  { icon: CompassIcon, text: 'Local guides' },
  { icon: MessageCircleIcon, text: 'Help on WhatsApp' },
]

/**
 * The first screen: real photos of Bangladesh fading one into the next,
 * each slowly settling from a slight zoom; the headline, the search and the
 * slide's caption on top.
 *
 * Motion (skipped entirely when the visitor's device asks for reduced motion):
 * - Entrance (GSAP timeline): the photo eases out of a zoom while the
 *   headline's words rise out of their masks, then the rest follows.
 *   It waits for the boot splash to fade, so it's actually seen.
 * - Scroll (ScrollTrigger): the photo moves slower than the page (parallax)
 *   and the text drifts up and fades as you leave.
 * - Slideshow: plain CSS. The thin bar under the active dot fills up over
 *   7 s; when it finishes, the next slide comes. Pausing pauses the bar,
 *   so the bar and the slideshow can never disagree.
 *
 * Loading: only the first photo is fetched at first (high priority, 8-53 KB);
 * the others start once it has arrived. A 130-byte blurred copy shows instantly.
 */
export function Hero() {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()
  const [active, setActive] = useState(0)
  /** The slide fading OUT - it keeps its zoom running until it's gone (no jump). */
  const [previous, setPrevious] = useState<number | null>(null)
  const [paused, setPaused] = useState(false)
  const [firstLoaded, setFirstLoaded] = useState(false)
  const slide = heroSlides[active]
  const autoplay = !paused && !reduced

  const show = (index: number) => {
    if (index === active) return
    setPrevious(active)
    setActive(index)
  }

  useGSAP(
    () => {
      if (reduced) return
      // Built paused: the "from" states apply at once (hidden behind the splash), the movement plays later.
      const intro = gsap.timeline({ paused: true, defaults: { ease: 'power3.out' } })
      intro
        .from('[data-hero-zoom]', { scale: 1.2, duration: 2.6, ease: 'power2.out' }, 0)
        .from('[data-hero-eyebrow]', { y: 20, autoAlpha: 0, duration: 0.8 }, 0.15)
        .from('[data-hero-title] [data-word]', { yPercent: 115, duration: 1.1, stagger: 0.08, ease: 'power4.out' }, 0.25)
        .from('[data-hero-lede]', { y: 24, autoAlpha: 0, duration: 0.9 }, 0.7)
        .from('[data-hero-search]', { y: 32, autoAlpha: 0, duration: 1 }, 0.85)
        .from('[data-hero-promise]', { y: 12, autoAlpha: 0, duration: 0.6, stagger: 0.08 }, 1.05)
        .from('[data-hero-bar]', { y: 16, autoAlpha: 0, duration: 0.8 }, 1.15)
      const stopWaiting = onBootSplashGone(() => intro.play())

      // scrub: the animation's progress follows the scrollbar, forwards and backwards.
      const leaving = { trigger: root.current, start: 'top top', end: 'bottom top', scrub: true }
      gsap.to('[data-hero-media]', { yPercent: 18, ease: 'none', scrollTrigger: leaving })
      gsap.to('[data-hero-content]', { y: -90, autoAlpha: 0, ease: 'none', scrollTrigger: { ...leaving, end: '75% top' } })

      return stopWaiting
    },
    { scope: root, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    // -mt: slides up under the sticky header (64px + its 1px border), so the photo reaches the top of the screen.
    <section ref={root} className="brand-surface relative isolate -mt-[calc(4rem+1px)] flex min-h-svh flex-col overflow-hidden bg-forest-950 text-white">
      {/* ---- The photos (decoration: the place is named in the caption) ---- */}
      <div aria-hidden data-hero-media className="absolute inset-0 -z-10">
        <div data-hero-zoom className="absolute inset-0">
          <div className="absolute inset-0 scale-110 bg-cover bg-center blur-2xl" style={{ backgroundImage: `url(${heroPlaceholder})` }} />
          {heroSlides.map((s, i) => (
            <div
              key={s.place}
              className={cn('absolute inset-0 transition-opacity duration-[1600ms] ease-out', i === active ? 'opacity-100' : 'opacity-0')}
            >
              {(i === 0 || firstLoaded || i === active) && (
                <Photo
                  photo={s.photo}
                  fetchPriority={i === 0 ? 'high' : 'low'}
                  onLoad={i === 0 ? () => setFirstLoaded(true) : undefined}
                  onError={i === 0 ? () => setFirstLoaded(true) : undefined}
                  className={cn(
                    'transition-opacity duration-700',
                    i === 0 && !firstLoaded && 'opacity-0',
                    (i === active || i === previous) && 'animate-ken-burns',
                  )}
                />
              )}
            </div>
          ))}
        </div>
      </div>
      {/* Shades so white text reads on any photo: darker at the bottom, on the left (behind the text) and under the header. */}
      <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-t from-forest-950 via-forest-950/35 to-forest-950/10" />
      <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-r from-forest-950/75 via-forest-950/25 to-transparent" />
      <div aria-hidden className="absolute inset-x-0 top-0 -z-10 h-40 bg-gradient-to-b from-forest-950/60 to-transparent" />

      {/* ---- Headline, search, promises ---- */}
      <div data-hero-content className="mx-auto flex w-full max-w-6xl flex-1 flex-col justify-center px-4 pt-28 pb-12 sm:pt-32">
        <div className="grid max-w-3xl justify-items-start gap-6">
          <span
            data-hero-eyebrow
            className="inline-flex h-8 items-center gap-2 rounded-full bg-white/10 px-3.5 text-xs font-semibold text-forest-50 ring-1 ring-white/20 backdrop-blur-md sm:text-sm"
          >
            <span aria-hidden className="size-1.5 rounded-full bg-sun-300" />
            {site.tagline}
          </span>

          <h1 data-hero-title className="text-[2.5rem] leading-[1.02] font-extrabold tracking-tight sm:text-7xl lg:text-[5rem]">
            <span className="block">
              <SplitWords mask text="Wander further," />
            </span>
            <span className="block text-sun-300">
              <SplitWords mask text="travel together." />
            </span>
          </h1>

          <p data-hero-lede className="max-w-xl text-lg text-forest-50/85 sm:text-xl">
            Group tours, flexible stays and custom trips across Bangladesh and beyond. Pick a date, book your seats and pay online - we arrange the
            rest.
          </p>

          <HeroSearch />

          <ul className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm text-forest-50/85">
            {promises.map(({ icon: Icon, text }) => (
              <li key={text} data-hero-promise className="flex items-center gap-2">
                <Icon className="size-4 text-sun-300" />
                {text}
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* ---- The slide's caption and the slideshow controls ---- */}
      <div data-hero-bar className="mx-auto w-full max-w-6xl px-4 pb-6">
        <div className="flex flex-wrap items-end justify-between gap-x-8 gap-y-4 border-t border-white/15 pt-5">
          <div key={active} className="grid animate-fade-in gap-1">
            <p className="flex items-center gap-2 font-bold">
              <MapPinIcon className="size-4 text-sun-300" />
              {slide.place}
            </p>
            <p className="text-sm text-forest-100/80">
              {slide.line}
              {' · '}
              <Link
                to={`/packages?${toPackageSearch({ q: slide.search })}`}
                className="font-semibold text-white underline-offset-4 hover:text-sun-300 hover:underline"
              >
                See trips
              </Link>
            </p>
          </div>

          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2">
              {heroSlides.map((s, i) => (
                <button
                  key={s.place}
                  type="button"
                  aria-label={`Show ${s.place}`}
                  aria-current={i === active}
                  onClick={() => show(i)}
                  className="group flex h-8 items-center focus-visible:outline-none"
                >
                  <span className="relative h-1 w-10 overflow-hidden rounded-full bg-white/25 transition-colors group-hover:bg-white/45 group-focus-visible:ring-2 group-focus-visible:ring-sun-300 sm:w-14">
                    {i === active &&
                      (autoplay ? (
                        // Each photo's 7 seconds. Remounted for every slide, so it always fills from empty; its end moves the slideshow on.
                        <span
                          className="absolute inset-0 origin-left animate-[grow-x_7s_linear_both] rounded-full bg-sun-300"
                          style={{ animationPlayState: paused ? 'paused' : 'running' }}
                          onAnimationEnd={() => show((active + 1) % heroSlides.length)}
                        />
                      ) : (
                        <span className="absolute inset-0 rounded-full bg-sun-300" />
                      ))}
                  </span>
                </button>
              ))}
            </div>
            {!reduced && (
              <button
                type="button"
                onClick={() => setPaused((p) => !p)}
                aria-label={paused ? 'Play slideshow' : 'Pause slideshow'}
                className="flex size-9 items-center justify-center rounded-full bg-white/10 ring-1 ring-white/20 backdrop-blur-md transition-colors hover:bg-white/20 focus-visible:ring-2 focus-visible:ring-sun-300 focus-visible:outline-none"
              >
                {paused ? <PlayIcon className="size-4 fill-current" /> : <PauseIcon className="size-4 fill-current" />}
              </button>
            )}
          </div>
        </div>
      </div>
    </section>
  )
}

/** "Where to?" + "Trip style" → the search page with both filled in. */
function HeroSearch() {
  const navigate = useNavigate()
  const destinations = useQuery(destinationsQuery(false))
  const categories = useQuery(categoriesQuery)
  const [destination, setDestination] = useState('any')
  const [category, setCategory] = useState('any')

  const national = destinations.data?.filter((d) => !d.isInternational) ?? []
  const international = destinations.data?.filter((d) => d.isInternational) ?? []

  const onSearch = (event: FormEvent) => {
    event.preventDefault()
    const query = toPackageSearch({
      destination: destination === 'any' ? undefined : destination,
      category: category === 'any' ? undefined : category,
    }).toString()
    navigate(query ? `/packages?${query}` : '/packages')
  }

  const triggerClass =
    'w-full border-transparent bg-ink-50 pl-12 text-[0.9375rem] font-semibold shadow-none hover:border-forest-200 hover:bg-forest-50/60 data-[size=default]:h-12'

  return (
    // One white bar: the two fields share the width, the button keeps its size - all 48px tall, edges level.
    <form
      data-hero-search
      onSubmit={onSearch}
      role="search"
      className="grid w-full gap-2 rounded-[1.375rem] bg-white p-2 text-ink-900 shadow-pop ring-1 ring-white/40 sm:flex sm:items-center"
    >
      <div className="relative min-w-0 flex-1">
        <MapPinIcon className="pointer-events-none absolute top-1/2 left-4 z-10 size-5 -translate-y-1/2 text-forest-600" />
        <Select value={destination} onValueChange={setDestination}>
          <SelectTrigger className={triggerClass} aria-label="Where to?">
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
      <div className="relative min-w-0 flex-1">
        <CompassIcon className="pointer-events-none absolute top-1/2 left-4 z-10 size-5 -translate-y-1/2 text-forest-600" />
        <Select value={category} onValueChange={setCategory}>
          <SelectTrigger className={triggerClass} aria-label="Trip style">
            <SelectValue placeholder="Trip style" />
          </SelectTrigger>
          <SelectContent position="popper" className="max-h-72">
            <SelectItem value="any">Any trip style</SelectItem>
            {(categories.data?.length ?? 0) > 0 && <SelectSeparator />}
            {categories.data?.map((c) => (
              <SelectItem key={c.id} value={c.slug}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <Button type="submit" size="lg" className="sm:px-7">
        <SearchIcon />
        Search tours
        <ArrowRightIcon className="hidden group-hover/button:translate-x-0.5 sm:block" />
      </Button>
    </form>
  )
}
