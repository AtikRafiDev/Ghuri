import { NavLink, Outlet } from 'react-router'
import { cn } from '@/lib/utils'
import { SiteHeader } from './SiteHeader'

// The admin menu. Day 3+ adds Destinations, Packages, Bookings... here.
const adminMenu = [
  { to: '/admin', label: 'Dashboard', end: true },
  { to: '/admin/system', label: 'System health', end: false },
]

/** The admin panel (/admin/...), desktop-first. Guarded by RequireRole(staff) in the router. */
export function AdminLayout() {
  return (
    <div className="min-h-svh">
      <SiteHeader />
      <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-6 md:flex-row">
        <nav className="flex gap-1 md:w-48 md:shrink-0 md:flex-col" aria-label="Admin">
          {adminMenu.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  'rounded-md px-3 py-2 text-sm hover:bg-muted',
                  isActive ? 'bg-muted font-medium text-foreground' : 'text-muted-foreground',
                )
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <main className="min-w-0 flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
