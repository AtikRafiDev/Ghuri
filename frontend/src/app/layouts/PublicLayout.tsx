import { Outlet } from 'react-router'
import { SiteHeader } from './SiteHeader'

/** The public website: home, login, register... (mobile-first, blueprint 13). */
export function PublicLayout() {
  return (
    <div className="min-h-svh">
      <SiteHeader />
      <main className="mx-auto max-w-6xl px-4 py-8">
        {/* The current page renders here. */}
        <Outlet />
      </main>
    </div>
  )
}
