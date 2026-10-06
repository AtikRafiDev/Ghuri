import { Outlet, ScrollRestoration } from 'react-router'
import { WhatsAppButton } from '@/shared/components/WhatsAppButton'
import { SiteFooter } from './SiteFooter'
import { SiteHeader } from './SiteHeader'

/** The public website: home, packages, login, register... (mobile-first, blueprint 13). */
export function PublicLayout() {
  return (
    // A column as tall as the screen: on a short page the footer still sits at the bottom.
    <div className="flex min-h-svh flex-col">
      {/* A new page opens at the top; Back returns to where you were (e.g. the same spot in the search results). */}
      <ScrollRestoration />
      <SiteHeader />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        {/* The current page renders here. */}
        <Outlet />
      </main>
      <SiteFooter />
      <WhatsAppButton />
    </div>
  )
}
