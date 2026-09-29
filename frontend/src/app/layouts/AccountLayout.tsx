import { Outlet } from 'react-router'
import { SiteHeader } from './SiteHeader'

/** The logged-in customer's area (/account/...). Guarded by RequireAuth in the router. */
export function AccountLayout() {
  return (
    <div className="min-h-svh">
      <SiteHeader />
      <main className="mx-auto max-w-3xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
