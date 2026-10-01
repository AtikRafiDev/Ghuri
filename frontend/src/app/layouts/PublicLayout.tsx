import { Outlet } from 'react-router'
import { SiteFooter } from './SiteFooter'
import { SiteHeader } from './SiteHeader'

/** The public website: home, packages, login, register... (mobile-first, blueprint 13). */
export function PublicLayout() {
  return (
    // A column as tall as the screen: on a short page the footer still sits at the bottom.
    <div className="flex min-h-svh flex-col">
      <SiteHeader />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        {/* The current page renders here. */}
        <Outlet />
      </main>
      <SiteFooter />
    </div>
  )
}
