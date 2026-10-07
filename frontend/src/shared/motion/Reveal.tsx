import { useRef, type ReactNode } from 'react'
import { gsap, useGSAP } from './gsap'
import { usePrefersReducedMotion } from './usePrefersReducedMotion'

/**
 * A box whose children rise and fade in, one after another, the first time
 * it scrolls into view. The children are laid out by className (a grid, a
 * flex row...) exactly as if this were a plain <div>.
 *
 * For content that arrives later (API data), give it a key that changes
 * when the data lands: <Reveal key={data ? 'data' : 'loading'}>. The box
 * then starts fresh and the real cards play their entrance too.
 */
export function Reveal({ children, className, y = 40, stagger = 0.08 }: { children: ReactNode; className?: string; y?: number; stagger?: number }) {
  const ref = useRef<HTMLDivElement>(null)
  const reduced = usePrefersReducedMotion()

  useGSAP(
    () => {
      if (reduced || !ref.current) return
      gsap.from(ref.current.children, {
        autoAlpha: 0, // opacity + visibility together: nothing invisible can be clicked
        y,
        duration: 0.9,
        ease: 'power3.out',
        stagger,
        // Hand the element back to CSS afterwards: a transform left behind would
        // block CSS hover effects (e.g. .hover-lift) on the same element.
        clearProps: 'transform,opacity,visibility',
        scrollTrigger: { trigger: ref.current, start: 'top 85%', once: true },
      })
    },
    { scope: ref, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    <div ref={ref} className={className}>
      {children}
    </div>
  )
}
