import { ArrowUpRightIcon, MapIcon, MoonIcon, UsersIcon, type LucideIcon } from 'lucide-react'
import { useRef } from 'react'
import { Link } from 'react-router'
import { SectionHeading } from '@/shared/components/SectionHeading'
import { gsap, useGSAP } from '@/shared/motion/gsap'
import { Reveal } from '@/shared/motion/Reveal'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'
import { Photo } from '@/shared/photos/Photo'
import { sajekPhoto } from '@/shared/photos/photos'

/** Ghuri's three kinds of trip - each card opens the matching search (or the trip planner). */
const styles: { icon: LucideIcon; title: string; text: string; to: string; cta: string }[] = [
  {
    icon: UsersIcon,
    title: 'Group tours',
    text: 'Join a set departure with fellow travellers. The dates are fixed, the plan is made - just pick your seats.',
    to: '/packages?mode=FixedDepartures',
    cta: 'Browse group tours',
  },
  {
    icon: MoonIcon,
    title: 'Flexible stays',
    text: 'Choose the day you start and how many nights you stay - the price follows your plan.',
    to: '/packages?mode=FlexibleStay',
    cta: 'See flexible stays',
  },
  {
    icon: MapIcon,
    title: 'Custom trips',
    text: "Tell us where and when. We plan the route, hotels and transfers, and send you a quote to accept.",
    to: '/plan-trip',
    cta: 'Plan my trip',
  },
]

/**
 * A dark band on a photo of clouds over Sajek Valley. The photo is taller
 * than the band and slides the other way as the page scrolls (parallax), so
 * the band feels like a window onto a deeper landscape.
 */
export function TravelStyles() {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()

  useGSAP(
    () => {
      if (reduced) return
      gsap.fromTo(
        '[data-parallax]',
        { yPercent: -12 },
        { yPercent: 12, ease: 'none', scrollTrigger: { trigger: root.current, start: 'top bottom', end: 'bottom top', scrub: true } },
      )
    },
    { scope: root, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    <section ref={root} className="brand-surface relative isolate overflow-hidden bg-forest-950 py-24 text-white sm:py-32">
      {/* 15% taller than the band at each end, so the parallax never shows an edge. */}
      <div aria-hidden data-parallax className="absolute inset-x-0 -top-[15%] -bottom-[15%] -z-10">
        <Photo photo={sajekPhoto} loading="lazy" decoding="async" />
      </div>
      <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-b from-forest-950/85 via-forest-950/65 to-forest-950/90" />

      <div className="mx-auto grid max-w-6xl gap-12 px-4">
        <SectionHeading
          tone="light"
          eyebrow="Travel your way"
          title="Three ways to see more"
          text="Travel with a group, set your own dates, or let us plan the whole trip around you."
        />

        <Reveal className="grid gap-5 md:grid-cols-3" stagger={0.12}>
          {styles.map(({ icon: Icon, title, text, to, cta }, i) => (
            <Link
              key={title}
              to={to}
              className="group relative flex flex-col gap-5 overflow-hidden rounded-3xl bg-white/10 p-6 ring-1 ring-white/15 backdrop-blur-md transition-[background-color,translate,box-shadow] duration-500 ease-(--ease-out-expo) hover:-translate-y-1.5 hover:bg-white/15 hover:shadow-pop focus-visible:ring-4 focus-visible:ring-sun-300/50 focus-visible:outline-none sm:p-8"
            >
              <span aria-hidden className="absolute top-5 right-6 text-6xl leading-none font-extrabold text-white/10">
                0{i + 1}
              </span>
              <span className="flex size-12 items-center justify-center rounded-2xl bg-sun-500 text-forest-950 transition-transform duration-500 ease-(--ease-spring) group-hover:-rotate-6 group-hover:scale-110">
                <Icon className="size-6" />
              </span>
              <span className="grid gap-2">
                <span className="text-xl font-bold sm:text-2xl">{title}</span>
                <span className="text-forest-100/80">{text}</span>
              </span>
              <span className="mt-auto flex items-center gap-2 pt-2 text-sm font-bold text-sun-300">
                {cta}
                <ArrowUpRightIcon className="size-4 transition-transform duration-300 group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
              </span>
            </Link>
          ))}
        </Reveal>
      </div>
    </section>
  )
}
