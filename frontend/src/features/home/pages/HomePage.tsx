import { useRef } from 'react'
import { site } from '@/shared/config/site'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { useScrollTriggerRefresh } from '@/shared/motion/useScrollTriggerRefresh'
import { Categories } from '../components/Categories'
import { DestinationMarquee } from '../components/DestinationMarquee'
import { DestinationMosaic } from '../components/DestinationMosaic'
import { Hero } from '../components/Hero'
import { HowItWorks } from '../components/HowItWorks'
import { IntroStatement } from '../components/IntroStatement'
import { PopularPackages } from '../components/PopularPackages'
import { TravelStyles } from '../components/TravelStyles'

/**
 * The landing page. A full-bleed route (app/router.tsx): it runs edge to
 * edge and starts under the see-through header, and each section sets its
 * own width. Top to bottom:
 *
 *   Hero               photo slideshow, headline, search
 *   DestinationMarquee every destination's name, drifting by
 *   IntroStatement     a sentence that lights up as you scroll + live numbers
 *   PopularPackages    8 package cards
 *   TravelStyles       group tours / flexible stays / custom trips, on a photo
 *   DestinationMosaic  the featured destinations
 *   HowItWorks         three steps on a self-drawing line
 *   Categories         trip-style chips
 *
 * Every list comes from the API (cached, so the search page reuses it);
 * the four photos ship with the site (features/home/assets).
 */
export function HomePage() {
  const root = useRef<HTMLDivElement>(null)
  useDocumentMeta({ title: site.tagline })
  useScrollTriggerRefresh(root)

  return (
    <div ref={root}>
      <Hero />
      <DestinationMarquee />
      <IntroStatement />
      <PopularPackages />
      <TravelStyles />
      <DestinationMosaic />
      <HowItWorks />
      <Categories />
    </div>
  )
}
