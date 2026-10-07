import { useQuery } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { categoriesQuery, destinationsQuery, packagesQuery } from '@/features/catalog/api/catalog.api'
import { AnimatedNumber } from '@/shared/components/AnimatedNumber'
import { gsap, ScrollTrigger, useGSAP } from '@/shared/motion/gsap'
import { SplitWords } from '@/shared/motion/SplitWords'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'
import { popularPackagesSearch } from '../data/homeQueries'

const statement =
  'Pick a date, grab your seats and pay online. We sort out the buses, the boats, the cottages and the guides - you just bring your curiosity.'

/**
 * One big sentence that "lights up" word by word as you scroll through it
 * (each word's opacity is tied to the scrollbar), and three live numbers
 * that count up when they come into view.
 *
 * The numbers are REAL - counted from the catalogue, never made up - and
 * the packages count reuses the "Popular packages" request (same query =
 * same cache), so this adds no extra download.
 */
export function IntroStatement() {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()
  const [counting, setCounting] = useState(false)

  const destinations = useQuery(destinationsQuery(false))
  const packages = useQuery(packagesQuery(popularPackagesSearch))
  const categories = useQuery(categoriesQuery)

  const stats = [
    { value: destinations.data?.length, label: 'destinations', text: 'From sea beaches to hill tracts - and a few trips abroad.' },
    { value: packages.data?.totalCount, label: 'trips to book', text: 'Group departures and flexible stays, with live prices.' },
    { value: categories.data?.length, label: 'trip styles', text: 'Beach, hills, honeymoon, family - travel your way.' },
  ]

  useGSAP(
    () => {
      // The counters start when the numbers come into view (not when the page opens, off screen).
      ScrollTrigger.create({ trigger: '[data-stats]', start: 'top 85%', once: true, onEnter: () => setCounting(true) })
      if (reduced) return
      gsap.fromTo(
        '[data-statement] [data-word]',
        { opacity: 0.15 },
        { opacity: 1, ease: 'none', stagger: 0.1, scrollTrigger: { trigger: '[data-statement]', start: 'top 80%', end: 'bottom 45%', scrub: true } },
      )
    },
    { scope: root, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    <section ref={root} className="mx-auto grid max-w-6xl gap-14 px-4 py-20 sm:py-28 lg:grid-cols-[1.4fr_1fr] lg:gap-20">
      <p data-statement className="text-[1.75rem] leading-[1.25] font-bold tracking-tight text-ink-900 sm:text-4xl sm:leading-[1.2]">
        <SplitWords text={statement} />
      </p>

      <dl data-stats className="grid content-center gap-8 border-ink-200 lg:border-l lg:pl-12">
        {stats.map(({ value, label, text }) => (
          <div key={label} className="grid gap-1">
            <dt className="order-2 text-sm font-bold tracking-wider text-forest-600 uppercase">{label}</dt>
            <dd className="order-1 text-5xl leading-none font-extrabold tracking-tight text-ink-900 sm:text-6xl">
              {value === undefined ? '–' : <AnimatedNumber value={counting ? value : 0} duration={1600} />}
            </dd>
            <dd className="order-3 text-sm text-ink-500">{text}</dd>
          </div>
        ))}
      </dl>
    </section>
  )
}
