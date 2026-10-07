import { Outlet, ScrollRestoration, useLocation } from 'react-router'
import { cn } from '@/lib/utils'
import { RouteProgress } from '@/shared/components/RouteProgress'
import { WhatsAppButton } from '@/shared/components/WhatsAppButton'
import { WorldMapBackdrop } from '@/shared/components/WorldMapBackdrop'
import { useFullBleed } from './fullBleed'
import { SiteFooter } from './SiteFooter'
import { SiteHeader } from './SiteHeader'

/** The public website: home, packages, login, register... (mobile-first, blueprint 13). */
export function PublicLayout() {
  const { pathname } = useLocation()
  const fullBleed = useFullBleed()
  return (
    // A column as tall as the screen: on a short page the footer still sits at the bottom.
    <div className="flex min-h-svh flex-col">
      <WorldMapBackdrop />
      {/* A new page opens at the top; Back returns to where you were (e.g. the same spot in the search results). */}
      <ScrollRestoration />
      <RouteProgress />
      <SiteHeader />
      {/* key: each new page gently rises into place. A full-bleed page (the home page) lays out its own
          sections edge to edge and plays its own entrance, so it only fades. */}
      <main
        key={pathname}
        className={cn('w-full flex-1', fullBleed ? 'animate-fade-in' : 'mx-auto max-w-6xl animate-fade-up px-4 py-8 sm:py-10')}
      >
        {/* The current page renders here. */}
        <Outlet />
      </main>
      <SiteFooter />
      <WhatsAppButton />
    </div>
  )
}
