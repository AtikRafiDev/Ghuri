import { CalendarCheckIcon, LayoutGridIcon, RouteIcon, UserRoundCogIcon } from 'lucide-react'
import { NavLink, Outlet, ScrollRestoration, useLocation } from 'react-router'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { RouteProgress } from '@/shared/components/RouteProgress'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { WorldMapBackdrop } from '@/shared/components/WorldMapBackdrop'
import { SiteFooter } from './SiteFooter'
import { SiteHeader } from './SiteHeader'

const tabs = [
  { to: '/account', label: 'Overview', end: true, icon: LayoutGridIcon },
  { to: '/account/bookings', label: 'My bookings', end: false, icon: CalendarCheckIcon },
  { to: '/account/trips', label: 'My trips', end: false, icon: RouteIcon },
  { to: '/account/profile', label: 'Profile', end: true, icon: UserRoundCogIcon },
]

/** The logged-in customer's area (/account/...). Guarded by RequireAuth in the router. */
export function AccountLayout() {
  const { user } = useAuth()
  const { pathname } = useLocation()

  return (
    <div className="flex min-h-svh flex-col">
      <WorldMapBackdrop />
      <ScrollRestoration />
      <RouteProgress />
      <SiteHeader />
      <main className="mx-auto grid w-full max-w-4xl flex-1 content-start gap-8 px-4 py-8 sm:py-10">
        <div className="grid animate-fade-up gap-6">
          {user && (
            <div className="flex items-center gap-4">
              <UserAvatar name={user.fullName} size="lg" className="size-14 text-base" />
              <div className="grid gap-0.5">
                <p className="text-sm font-medium text-ink-500">Welcome back</p>
                <p className="text-xl font-bold tracking-tight text-ink-900">{user.fullName}</p>
              </div>
            </div>
          )}
          {/* Pill tabs; scrolls sideways on a narrow phone rather than wrapping. */}
          <nav aria-label="My account" className="-mx-4 overflow-x-auto px-4">
            <div className="flex w-max gap-1 rounded-2xl bg-ink-100 p-1 ring-1 ring-ink-200/60 ring-inset">
              {tabs.map(({ to, label, end, icon: Icon }) => (
                <NavLink
                  key={to}
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    cn(
                      'flex h-10 shrink-0 items-center gap-2 rounded-xl px-4 text-sm font-semibold transition-all duration-200',
                      isActive ? 'bg-card text-forest-800 shadow-soft' : 'text-ink-500 hover:bg-card/60 hover:text-ink-900',
                    )
                  }
                >
                  <Icon className="size-4" />
                  {label}
                </NavLink>
              ))}
            </div>
          </nav>
        </div>
        <div key={pathname} className="animate-fade-up">
          <Outlet />
        </div>
      </main>
      <SiteFooter />
    </div>
  )
}
