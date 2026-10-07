import { useRef, type ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { onBootSplashGone } from '@/shared/loader/bootSplash'
import { gsap, useGSAP } from '@/shared/motion/gsap'
import { SplitWords } from '@/shared/motion/SplitWords'
import { usePrefersReducedMotion } from '@/shared/motion/usePrefersReducedMotion'
import { Photo } from '@/shared/photos/Photo'
import type { Photo as PhotoData } from '@/shared/photos/photos'

/**
 * The top of an inner page in the home page's style: a dark band that runs
 * edge to edge and slides up under the see-through header. Behind it, one of
 * three things:
 *
 * - photo:    one of the site's own photos (shared/photos);
 * - imageUrl: a photo from the API (a package's cover, a destination);
 * - neither:  the brand green with its contour lines.
 *
 * On top: an optional back link (top), an eyebrow, the title - its words
 * rise out of their masks - a line of text, and whatever the page puts under
 * it (children: a search bar, badges, steps).
 *
 * The page's route must be full-bleed (handle: { fullBleed: true } in
 * app/router.tsx). That's what lets the band start under the header - and the
 * header's light text needs this dark band behind it.
 *
 * Motion (skipped entirely with "reduce motion"): the background eases out of
 * a zoom while the title rises and the rest follows; on scroll the background
 * moves slower than the page (parallax) and the text drifts up and fades.
 */
export function PageHero({
  title,
  eyebrow,
  text,
  top,
  children,
  photo,
  imageUrl,
  size = 'md',
}: {
  title: string
  eyebrow?: ReactNode
  text?: ReactNode
  top?: ReactNode
  children?: ReactNode
  photo?: PhotoData
  imageUrl?: string | null
  /** md: a band for the title; lg: most of the screen, for a page that leads with its photo. */
  size?: 'md' | 'lg'
}) {
  const root = useRef<HTMLElement>(null)
  const reduced = usePrefersReducedMotion()
  const hasImage = Boolean(photo || imageUrl)

  useGSAP(
    () => {
      if (reduced) return
      // Built paused: the "from" states apply at once, the movement plays once the boot splash
      // has gone (straight away when arriving from another page).
      const intro = gsap.timeline({ paused: true, defaults: { ease: 'power3.out' } })
      intro
        .from('[data-hero-zoom]', { scale: 1.15, duration: 2.2, ease: 'power2.out' }, 0)
        .from('[data-hero-lead]', { y: 16, autoAlpha: 0, duration: 0.7, stagger: 0.06 }, 0.1)
        .from('[data-hero-title] [data-word]', { yPercent: 115, duration: 1, stagger: 0.06, ease: 'power4.out' }, 0.2)
        // clearProps: hand the elements back to CSS - a leftover transform would trap anything "fixed" inside them.
        .from('[data-hero-fade]', { y: 24, autoAlpha: 0, duration: 0.9, stagger: 0.1, clearProps: 'transform,opacity,visibility' }, 0.55)
      const stopWaiting = onBootSplashGone(() => intro.play())

      // scrub: the progress follows the scrollbar, forwards and backwards.
      const leaving = { trigger: root.current, start: 'top top', end: 'bottom top', scrub: true }
      gsap.to('[data-hero-media]', { yPercent: 18, ease: 'none', scrollTrigger: leaving })
      gsap.to('[data-hero-content]', { y: -60, autoAlpha: 0, ease: 'none', scrollTrigger: { ...leaving, end: '85% top' } })

      return stopWaiting
    },
    { scope: root, dependencies: [reduced], revertOnUpdate: true },
  )

  return (
    // -mt: slides up under the sticky header (64px + its 1px border).
    <section
      ref={root}
      className={cn(
        'brand-surface relative isolate -mt-[calc(4rem+1px)] flex flex-col overflow-hidden bg-forest-950 text-white',
        size === 'lg' ? 'min-h-[78svh]' : 'min-h-[26rem] sm:min-h-[30rem]',
      )}
    >
      <div aria-hidden data-hero-media className="absolute inset-0 -z-10">
        <div data-hero-zoom className="absolute inset-0">
          {photo ? (
            <Photo photo={photo} fetchPriority="high" />
          ) : imageUrl ? (
            // key: a new photo (another destination picked) fades in instead of snapping.
            <img key={imageUrl} src={imageUrl} alt="" fetchPriority="high" className="absolute inset-0 size-full animate-fade-in object-cover" />
          ) : (
            <>
              <div className="absolute inset-0 bg-gradient-to-br from-forest-700 via-forest-800 to-forest-950" />
              <div className="bg-topo absolute inset-0" />
              <div className="absolute -top-24 -right-16 size-96 rounded-full bg-sun-500/10 blur-3xl" />
              <div className="absolute -bottom-32 -left-20 size-96 rounded-full bg-forest-400/20 blur-3xl" />
            </>
          )}
        </div>
      </div>
      {hasImage && (
        // Shades so white text reads on any photo: darker at the bottom, on the left (behind the text) and under the header.
        <>
          <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-t from-forest-950 via-forest-950/45 to-forest-950/15" />
          <div aria-hidden className="absolute inset-0 -z-10 bg-gradient-to-r from-forest-950/70 via-forest-950/25 to-transparent" />
          <div aria-hidden className="absolute inset-x-0 top-0 -z-10 h-40 bg-gradient-to-b from-forest-950/60 to-transparent" />
        </>
      )}

      <div data-hero-content className="mx-auto flex w-full max-w-6xl flex-1 flex-col justify-end gap-8 px-4 pt-28 pb-12 sm:pt-32 sm:pb-16">
        <div className="grid max-w-3xl justify-items-start gap-5">
          {top && <div data-hero-lead>{top}</div>}
          {eyebrow && (
            <div data-hero-lead className="flex items-center gap-2 text-xs font-bold tracking-[0.18em] text-sun-300 uppercase">
              <span aria-hidden className="h-px w-8 bg-sun-300" />
              {eyebrow}
            </div>
          )}
          <h1 data-hero-title className="text-4xl leading-[1.04] font-extrabold tracking-tight text-balance sm:text-6xl">
            <SplitWords mask text={title} />
          </h1>
          {text && (
            <div data-hero-fade className="max-w-2xl text-lg text-forest-50/85 sm:text-xl">
              {text}
            </div>
          )}
          {children && (
            <div data-hero-fade className="w-full">
              {children}
            </div>
          )}
        </div>
      </div>
    </section>
  )
}
