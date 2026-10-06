import { HouseIcon, RotateCwIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { BrandMark } from '@/shared/components/BrandMark'
import { site } from '@/shared/config/site'
import { LostSuitcase } from './NotFoundPage'

/**
 * Shown when a page crashes while rendering, or its code can't be
 * downloaded (e.g. we deployed a new version while this tab was open and
 * the old file is gone). A reload fixes both cases in practice.
 *
 * It replaces the whole layout (no header or footer), so it carries its own
 * logo. "Home" is a plain <a>, not a router Link: a full page load also
 * picks up the newly deployed files, which is exactly what's needed here.
 */
export function RouteErrorPage() {
  return (
    <section className="relative isolate grid min-h-svh place-content-center justify-items-center gap-7 overflow-hidden bg-background p-4 text-center">
      <div aria-hidden className="absolute top-1/2 left-1/2 -z-10 size-[36rem] -translate-1/2 bg-[radial-gradient(closest-side,var(--color-forest-100),transparent)]" />

      <div className="flex items-center gap-2.5 text-xl font-bold tracking-tight text-forest-900">
        <BrandMark />
        {site.name}
      </div>

      {/* The suitcase has tipped over - something went wrong on the way. */}
      <LostSuitcase mark="!" className="h-36 -rotate-12 animate-float sm:h-44" />

      <div className="grid max-w-md justify-items-center gap-3">
        <h1 className="text-2xl font-bold text-ink-900 sm:text-3xl">We hit a bump in the road</h1>
        <p className="text-ink-500">Something went wrong. Please reload the page. If it keeps happening, try again in a few minutes.</p>
      </div>

      <div className="flex flex-wrap justify-center gap-3">
        <Button size="lg" className="[&_svg]:transition-[rotate]" onClick={() => window.location.reload()}>
          <RotateCwIcon className="group-hover/button:rotate-180" />
          Reload
        </Button>
        <Button asChild size="lg" variant="outline">
          <a href="/">
            <HouseIcon />
            Go to the home page
          </a>
        </Button>
      </div>
    </section>
  )
}
