import {
  CalendarCheckIcon,
  ChevronDownIcon,
  CircleHelpIcon,
  InfoIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  LuggageIcon,
  MapIcon,
  MenuIcon,
  RouteIcon,
  ShieldCheckIcon,
  UserRoundIcon,
} from 'lucide-react'
import { useState, useSyncExternalStore } from 'react'
import { Link, NavLink, useLocation, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet'
import { Skeleton } from '@/components/ui/skeleton'
import { staffRoles, type Me } from '@/features/auth/auth.types'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { BrandLink } from '@/shared/components/BrandMark'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { ThemeToggle } from '@/shared/theme/ThemeToggle'

const navLinks = [
  { to: '/packages', label: 'Packages', icon: LuggageIcon },
  { to: '/plan-trip', label: 'Plan my trip', icon: MapIcon },
  { to: '/about', label: 'About', icon: InfoIcon },
  { to: '/faq', label: 'FAQ', icon: CircleHelpIcon },
]

const accountLinks = [
  { to: '/account', label: 'My account', icon: UserRoundIcon },
  { to: '/account/bookings', label: 'My bookings', icon: CalendarCheckIcon },
  { to: '/account/trips', label: 'My trips', icon: RouteIcon },
]

/**
 * The top bar on every public and account page. Sticky and frosted: it
 * gains a hairline and shadow once the page scrolls under it. On a phone the
 * links fold into a slide-in menu.
 */
export function SiteHeader() {
  const { status, user, hasAnyRole, logout } = useAuth()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)
  const scrolled = useScrolled()

  const handleLogout = async () => {
    setLoggingOut(true)
    try {
      await logout()
    } finally {
      setLoggingOut(false)
      navigate('/login', { replace: true })
    }
  }

  return (
    <header
      className={cn(
        'sticky top-0 z-40 border-b transition-[background-color,border-color,box-shadow] duration-300 print:hidden',
        scrolled ? 'border-ink-200/80 bg-card/80 shadow-soft backdrop-blur-xl' : 'border-transparent bg-ink-50/60 backdrop-blur-md',
      )}
    >
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4">
        <div className="flex items-center gap-8">
          <BrandLink />
          <nav aria-label="Main" className="hidden items-center gap-1 md:flex">
            {navLinks.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                className={({ isActive }) =>
                  cn(
                    'relative rounded-full px-3.5 py-2 text-sm font-semibold transition-colors duration-200',
                    isActive ? 'bg-forest-100 text-forest-800' : 'text-ink-600 hover:bg-ink-100 hover:text-ink-900',
                  )
                }
              >
                {link.label}
              </NavLink>
            ))}
          </nav>
        </div>

        <div className="flex items-center gap-2">
          <ThemeToggle />
          {status === 'checking' && <Skeleton className="h-10 w-36 rounded-full" />}
          {status === 'anonymous' && (
            <div className="hidden items-center gap-2 sm:flex">
              <Button asChild variant="ghost">
                <Link to="/login">Log in</Link>
              </Button>
              <Button asChild>
                <Link to="/register">Sign up</Link>
              </Button>
            </div>
          )}
          {status === 'authenticated' && user && (
            <UserMenu user={user} isStaff={hasAnyRole(staffRoles)} loggingOut={loggingOut} onLogout={handleLogout} />
          )}
          <MobileMenu status={status} user={user} isStaff={hasAnyRole(staffRoles)} onLogout={handleLogout} />
        </div>
      </div>
    </header>
  )
}

/** Avatar + first name; opens the account shortcuts and "Log out". */
function UserMenu({ user, isStaff, loggingOut, onLogout }: { user: Me; isStaff: boolean; loggingOut: boolean; onLogout: () => void }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="group hidden h-11 items-center gap-2 rounded-full border border-ink-200 bg-card py-1 pr-3 pl-1 shadow-soft transition-[border-color,box-shadow] duration-200 hover:border-forest-300 hover:shadow-card focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none data-[state=open]:border-forest-300 sm:flex"
        >
          <UserAvatar name={user.fullName} />
          <span className="max-w-32 truncate text-sm font-semibold text-ink-900">{user.fullName.split(' ')[0]}</span>
          <ChevronDownIcon className="size-4 text-ink-400 transition-transform duration-300 group-data-[state=open]:rotate-180" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuLabel className="flex items-center gap-3">
          <UserAvatar name={user.fullName} size="lg" />
          <span className="grid min-w-0">
            <span className="truncate font-semibold text-ink-900">{user.fullName}</span>
            <span className="truncate text-xs font-normal text-ink-500">{user.email ?? user.phone}</span>
          </span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {accountLinks.map(({ to, label, icon: Icon }) => (
          <DropdownMenuItem key={to} asChild>
            <Link to={to}>
              <Icon />
              {label}
            </Link>
          </DropdownMenuItem>
        ))}
        {isStaff && (
          <DropdownMenuItem asChild>
            <Link to="/admin">
              <LayoutDashboardIcon />
              Admin panel
            </Link>
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" disabled={loggingOut} onSelect={onLogout}>
          <LogOutIcon />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

/** Phones: a menu button that slides the whole navigation in from the right. Closes itself on navigation. */
function MobileMenu({
  status,
  user,
  isStaff,
  onLogout,
}: {
  status: 'checking' | 'authenticated' | 'anonymous'
  user: Me | null
  isStaff: boolean
  onLogout: () => void
}) {
  const [open, setOpen] = useState(false)
  const { pathname } = useLocation()
  const [lastPath, setLastPath] = useState(pathname)
  // A new page was opened from the menu → close it (adjusting state during render, React's recommended pattern).
  if (pathname !== lastPath) {
    setLastPath(pathname)
    setOpen(false)
  }

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      'flex h-12 items-center gap-3 rounded-xl px-3 text-[0.9375rem] font-semibold transition-colors',
      isActive ? 'bg-forest-100 text-forest-800' : 'text-ink-700 hover:bg-ink-100',
    )

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant="outline" size="icon" className="md:hidden" aria-label="Open menu">
          <MenuIcon />
        </Button>
      </SheetTrigger>
      <SheetContent side="right" className="w-[86%] max-w-sm gap-0 p-0">
        <SheetHeader className="border-b px-5 py-4">
          <SheetTitle asChild>
            <div>
              <BrandLink />
            </div>
          </SheetTitle>
          <SheetDescription className="sr-only">Site navigation</SheetDescription>
        </SheetHeader>
        <nav aria-label="Mobile" className="stagger grid gap-1 p-3">
          {navLinks.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} className={linkClass}>
              <Icon className="size-[18px] text-forest-500" />
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-auto grid gap-2 border-t p-4">
          {status === 'authenticated' && user ? (
            <>
              <div className="mb-1 flex items-center gap-3 px-1">
                <UserAvatar name={user.fullName} size="lg" />
                <span className="grid min-w-0">
                  <span className="truncate font-semibold">{user.fullName}</span>
                  <span className="truncate text-xs text-ink-500">{user.email ?? user.phone}</span>
                </span>
              </div>
              {accountLinks.map(({ to, label, icon: Icon }) => (
                <NavLink key={to} to={to} end className={linkClass}>
                  <Icon className="size-[18px] text-forest-500" />
                  {label}
                </NavLink>
              ))}
              {isStaff && (
                <NavLink to="/admin" className={linkClass}>
                  <ShieldCheckIcon className="size-[18px] text-forest-500" />
                  Admin panel
                </NavLink>
              )}
              <Button variant="destructive" className="mt-2" onClick={onLogout}>
                <LogOutIcon />
                Log out
              </Button>
            </>
          ) : (
            <div className="grid grid-cols-2 gap-2">
              <Button asChild variant="outline">
                <Link to="/login">Log in</Link>
              </Button>
              <Button asChild>
                <Link to="/register">Sign up</Link>
              </Button>
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}

/** True once the page has scrolled a little - the header then turns solid. */
function useScrolled(threshold = 8) {
  return useSyncExternalStore(subscribeToScroll, () => window.scrollY > threshold)
}

function subscribeToScroll(onChange: () => void) {
  window.addEventListener('scroll', onChange, { passive: true })
  return () => window.removeEventListener('scroll', onChange)
}
