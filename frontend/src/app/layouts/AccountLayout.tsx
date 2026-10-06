import { NavLink, Outlet } from 'react-router'
import { cn } from '@/lib/utils'
import { SiteHeader } from './SiteHeader'

const tabs = [
  { to: '/account', label: 'Overview', end: true },
  { to: '/account/bookings', label: 'My bookings', end: false },
  { to: '/account/trips', label: 'My trips', end: false },
  { to: '/account/profile', label: 'Profile', end: true },
]

/** The logged-in customer's area (/account/...). Guarded by RequireAuth in the router. */
export function AccountLayout() {
  return (
    <div className="min-h-svh">
      <SiteHeader />
      <main className="mx-auto grid max-w-3xl gap-6 px-4 py-8">
        <nav aria-label="My account" className="flex gap-1 overflow-x-auto border-b">
          {tabs.map((tab) => (
            <NavLink
              key={tab.to}
              to={tab.to}
              end={tab.end}
              className={({ isActive }) =>
                cn(
                  '-mb-px shrink-0 border-b-2 px-3 py-2 text-sm font-medium transition-colors',
                  isActive ? 'border-primary text-foreground' : 'border-transparent text-muted-foreground hover:text-foreground',
                )
              }
            >
              {tab.label}
            </NavLink>
          ))}
        </nav>
        <div>
          <Outlet />
        </div>
      </main>
    </div>
  )
}
