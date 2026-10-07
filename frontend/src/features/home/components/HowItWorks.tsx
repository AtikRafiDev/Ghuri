import { BackpackIcon, CreditCardIcon, SearchIcon } from 'lucide-react'
import { useRef } from 'react'
import { SectionHeading } from '@/shared/components/SectionHeading'
import { gsap, useGSAP } from '@/shared/motion/gsap'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'

const steps = [
  {
    icon: SearchIcon,
    title: 'Choose your trip',
    text: 'Browse group tours with set dates, or flexible stays where you pick the nights. Every price is live.',
  },
  {
    icon: CreditCardIcon,
    title: 'Book and pay online',
    text: 'Add your travellers and pay securely. Your seats are held for 20 minutes while you check out.',
  },
  {
    icon: BackpackIcon,
    title: 'Pack and go',
    text: "Your booking waits in your account, and we're on WhatsApp for anything you need - before and during the trip.",
  },
]

/**
 * Three steps joined by a "route" line. As you scroll through the section
 * the line draws itself (scrubbed: it follows the scrollbar both ways), and
 * each step pops in as the line reaches it. Across on wide screens; on
 * phones, down the left side in pieces from one circle to the next.
 */
export function HowItWorks() {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()

  useGSAP(
    () => {
      if (reduced) return
      // One timeline, 2 "seconds" long: the line takes all of it; step 1, 2, 3 arrive at 0, 1 and 2 - when the line reaches them.
      const route = gsap.timeline({
        scrollTrigger: { trigger: '[data-steps]', start: 'top 75%', end: 'bottom 65%', scrub: 0.6 },
      })
      route.from('[data-line-x]', { scaleX: 0, duration: 2, ease: 'none' }, 0)
      route.from('[data-line-y]', { scaleY: 0, duration: 1, ease: 'none', stagger: 1 }, 0) // piece 1 in 0-1, piece 2 in 1-2
      gsap.utils.toArray<HTMLElement>('[data-step]', root.current).forEach((step, i) => {
        route.from(step.querySelector('[data-step-dot]'), { scale: 0.3, autoAlpha: 0, duration: 0.35, ease: 'back.out(2.5)' }, i)
        route.from(step.querySelector('[data-step-text]'), { y: 24, autoAlpha: 0, duration: 0.45, ease: 'power2.out' }, i + 0.1)
      })
    },
    { scope: root, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    <section ref={root} className="border-y border-ink-200 bg-forest-50">
      <div className="mx-auto grid max-w-6xl gap-14 px-4 py-20 sm:py-28">
        <SectionHeading eyebrow="How it works" title="From idea to adventure in three steps" />

        <div data-steps className="relative grid gap-12 lg:grid-cols-3 lg:gap-0">
          {/* Wide screens: one route line under the step circles (whose centres are 1/6, 1/2 and 5/6 across). */}
          <div aria-hidden className="absolute top-7 right-[16.667%] left-[16.667%] hidden h-0.5 -translate-y-1/2 rounded-full bg-forest-200 lg:block">
            <div data-line-x className="size-full origin-left rounded-full bg-forest-500" />
          </div>

          {steps.map(({ icon: Icon, title, text }, i) => (
            <div key={title} data-step className="relative grid grid-cols-[3.5rem_1fr] gap-5 lg:grid-cols-1 lg:justify-items-center lg:px-6 lg:text-center">
              {/* Phones: a piece of line from this circle down to the next one (top-14 = under the circle, -bottom-12 = across the gap). */}
              {i < steps.length - 1 && (
                <div aria-hidden className="absolute top-14 -bottom-12 left-7 w-0.5 -translate-x-1/2 bg-forest-200 lg:hidden">
                  <div data-line-y className="size-full origin-top bg-forest-500" />
                </div>
              )}
              <span
                data-step-dot
                className="relative flex size-14 items-center justify-center rounded-full bg-primary text-white shadow-lift ring-8 ring-forest-50"
              >
                <Icon className="size-6" />
              </span>
              <div data-step-text className="grid gap-2 pt-1 lg:pt-0">
                <p className="text-xs font-bold tracking-wider text-forest-600 uppercase">Step {i + 1}</p>
                <h3 className="text-xl font-bold text-ink-900">{title}</h3>
                <p className="max-w-xs text-ink-500">{text}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
