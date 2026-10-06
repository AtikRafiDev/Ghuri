import {
  ActivityIcon,
  ArrowUpRightIcon,
  CalendarCheckIcon,
  ChevronDownIcon,
  CornerDownLeftIcon,
  CreditCardIcon,
  ExternalLinkIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  MapPinIcon,
  PackageIcon,
  RouteIcon,
  SearchIcon,
  TagsIcon,
  Undo2Icon,
  UsersIcon,
  type LucideIcon,
} from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, NavLink, Outlet, ScrollRestoration, useLocation, useMatch, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Separator } from '@/components/ui/separator'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarTrigger,
} from '@/components/ui/sidebar'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { staffManagerRoles, type Role } from '@/features/auth/auth.types'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { BrandMark } from '@/shared/components/BrandMark'
import { RouteProgress } from '@/shared/components/RouteProgress'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { ThemeToggle } from '@/shared/theme/ThemeToggle'

/** roles: only these may see the entry (the page itself checks too). Absent = every staff member. */
type MenuItem = { to: string; label: string; icon: LucideIcon; end?: boolean; roles?: readonly Role[] }

// The admin menu, in groups.
const adminMenu: { label: string; items: MenuItem[] }[] = [
  { label: 'Overview', items: [{ to: '/admin', label: 'Dashboard', icon: LayoutDashboardIcon, end: true }] },
  {
    label: 'Operations',
    items: [
      { to: '/admin/bookings', label: 'Bookings', icon: CalendarCheckIcon },
      { to: '/admin/custom-trips', label: 'Custom trips', icon: RouteIcon },
      { to: '/admin/payments', label: 'Payments', icon: CreditCardIcon },
      { to: '/admin/refunds', label: 'Refunds', icon: Undo2Icon },
    ],
  },
  {
    label: 'Catalogue',
    items: [
      { to: '/admin/packages', label: 'Packages', icon: PackageIcon },
      { to: '/admin/destinations', label: 'Destinations', icon: MapPinIcon },
      { to: '/admin/categories', label: 'Categories', icon: TagsIcon },
    ],
  },
  {
    label: 'System',
    items: [
      { to: '/admin/staff', label: 'Staff', icon: UsersIcon, roles: staffManagerRoles },
      { to: '/admin/system', label: 'System health', icon: ActivityIcon },
    ],
  },
]

const roleLabels: Record<Role, string> = { SuperAdmin: 'Super Admin', Manager: 'Manager', Sales: 'Sales', Accounts: 'Accounts', Customer: 'Customer' }

/**
 * The admin panel (/admin/...), desktop-first (blueprint 13): a sidebar
 * that collapses to icons (Ctrl/⌘+B) and becomes a slide-in drawer on a
 * phone; a top bar with a page finder (Ctrl/⌘+K). Guarded by RequireRole(staff) in the router.
 */
export function AdminLayout() {
  const { pathname } = useLocation()
  const { hasAnyRole } = useAuth()
  const visibleMenu = adminMenu
    .map((group) => ({ ...group, items: group.items.filter((item) => !item.roles || hasAnyRole(item.roles)) }))
    .filter((group) => group.items.length > 0)
  const allItems = visibleMenu.flatMap((group) => group.items)
  const current = allItems.find((item) => (item.end ? pathname === item.to : pathname.startsWith(item.to)))
  const currentGroup = current && visibleMenu.find((group) => group.items.includes(current))

  return (
    <SidebarProvider>
      <ScrollRestoration />
      <RouteProgress />
      <Sidebar collapsible="icon" className="border-r border-sidebar-border">
        <SidebarHeader className="px-3 pt-4 pb-2">
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton asChild size="lg" tooltip="Ghuri admin" className="hover:bg-transparent">
                <Link to="/admin">
                  <BrandMark className="size-9" />
                  <span className="grid leading-tight">
                    <span className="text-lg font-bold tracking-tight text-forest-900">Ghuri</span>
                    <span className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">Admin panel</span>
                  </span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>

        <SidebarContent className="px-1">
          {visibleMenu.map((group) => (
            <SidebarGroup key={group.label}>
              <SidebarGroupLabel>{group.label}</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu className="gap-0.5">
                  {group.items.map((item) => (
                    <AdminMenuLink key={item.to} item={item} />
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          ))}
        </SidebarContent>

        <SidebarFooter className="p-3">
          <WebsiteCard />
        </SidebarFooter>
        {/* The thin strip on the sidebar's edge: click it to collapse/expand. */}
        <SidebarRail />
      </Sidebar>

      <SidebarInset className="min-w-0 bg-background">
        <header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-3 border-b border-ink-200/70 bg-ink-50/80 px-4 backdrop-blur-xl md:px-6">
          <SidebarTrigger className="size-9 rounded-lg" />
          <Separator orientation="vertical" className="h-5" />
          <nav aria-label="Breadcrumb" className="flex min-w-0 items-center gap-1.5 text-sm">
            {currentGroup && currentGroup.label !== 'Overview' && <span className="hidden text-ink-400 sm:inline">{currentGroup.label} /</span>}
            <span className="truncate font-semibold text-ink-900">{current?.label ?? 'Admin'}</span>
          </nav>
          <div className="ml-auto flex items-center gap-2">
            <QuickNav items={allItems} />
            <ThemeToggle />
            <Tooltip>
              <TooltipTrigger asChild>
                <Button asChild variant="outline" size="icon" className="hidden sm:inline-flex">
                  <Link to="/" aria-label="View website">
                    <ExternalLinkIcon />
                  </Link>
                </Button>
              </TooltipTrigger>
              <TooltipContent>View website</TooltipContent>
            </Tooltip>
            <AdminUserMenu />
          </div>
        </header>
        <div key={pathname} className="mx-auto w-full max-w-[1400px] min-w-0 flex-1 animate-fade-up p-4 md:p-8">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}

/** One menu entry. Its own component so it can use the useMatch hook ("is this page open?"). */
function AdminMenuLink({ item }: { item: MenuItem }) {
  const isActive = useMatch({ path: item.to, end: item.end ?? false }) !== null
  const Icon = item.icon
  return (
    <SidebarMenuItem>
      {/* tooltip = the label, shown when the sidebar is collapsed to icons */}
      <SidebarMenuButton asChild isActive={isActive} tooltip={item.label}>
        <NavLink to={item.to} end={item.end}>
          <Icon />
          <span>{item.label}</span>
        </NavLink>
      </SidebarMenuButton>
    </SidebarMenuItem>
  )
}

/** The green card at the bottom of the sidebar - a way back to the public site. Hidden when collapsed to icons. */
function WebsiteCard() {
  return (
    <div className="brand-surface relative overflow-hidden rounded-2xl bg-gradient-to-br from-forest-700 to-forest-950 p-4 text-white group-data-[collapsible=icon]:hidden [@media(max-height:760px)]:hidden">
      <div aria-hidden className="bg-topo absolute inset-0" />
      <div aria-hidden className="absolute -top-6 -right-6 size-20 rounded-full bg-sun-500/25 blur-xl" />
      <div className="relative grid gap-3">
        <span className="flex size-9 items-center justify-center rounded-xl bg-white/10 ring-1 ring-white/15">
          <BrandMark className="size-6" />
        </span>
        <div className="grid gap-0.5">
          <p className="text-sm font-bold">Your live website</p>
          <p className="text-xs text-forest-100/75">See packages the way customers do.</p>
        </div>
        <Button asChild variant="accent" size="sm" className="w-full">
          <Link to="/">
            Open website
            <ArrowUpRightIcon />
          </Link>
        </Button>
      </div>
    </div>
  )
}

/** Top-right: who is logged in (name + role), with "View website" and "Log out". */
function AdminUserMenu() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)
  if (!user) return null
  const role = user.roles.find((r) => r !== 'Customer') ?? user.roles[0]

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
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          className="group flex h-11 items-center gap-2.5 rounded-full border border-ink-200 bg-card py-1 pr-3 pl-1 shadow-soft transition-[border-color,box-shadow] hover:border-forest-300 hover:shadow-card focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none data-[state=open]:border-forest-300"
        >
          <UserAvatar name={user.fullName} />
          <span className="hidden min-w-0 text-left leading-tight lg:grid">
            <span className="max-w-36 truncate text-sm font-semibold text-ink-900">{user.fullName}</span>
            <span className="text-[0.6875rem] font-medium text-ink-500">{role ? roleLabels[role] : 'Staff'}</span>
          </span>
          <ChevronDownIcon className="size-4 text-ink-400 transition-transform duration-300 group-data-[state=open]:rotate-180" />
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel className="grid">
          <span className="truncate font-semibold">{user.fullName}</span>
          <span className="truncate text-xs font-normal text-ink-500">{user.email ?? user.phone}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link to="/">
            <ExternalLinkIcon />
            View website
          </Link>
        </DropdownMenuItem>
        <DropdownMenuItem variant="destructive" disabled={loggingOut} onSelect={handleLogout}>
          <LogOutIcon />
          Log out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

/**
 * "Jump to a page": a search box in the top bar, also opened with Ctrl/⌘+K.
 * Type a few letters, arrow to the page, Enter.
 */
function QuickNav({ items }: { items: MenuItem[] }) {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [active, setActive] = useState(0)

  const results = useMemo(() => {
    const q = query.trim().toLowerCase()
    return q ? items.filter((item) => item.label.toLowerCase().includes(q)) : items
  }, [items, query])

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        setOpen((o) => !o)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  const openChange = (next: boolean) => {
    setOpen(next)
    setQuery('')
    setActive(0)
  }

  const go = (item: MenuItem | undefined) => {
    if (!item) return
    openChange(false)
    navigate(item.to)
  }

  return (
    <>
      <button
        type="button"
        onClick={() => openChange(true)}
        className="group flex h-10 items-center gap-2 rounded-xl border border-ink-200 bg-card px-3 text-sm text-ink-400 shadow-soft transition-[border-color,color] hover:border-forest-300 hover:text-ink-600 md:w-64"
      >
        <SearchIcon className="size-4 transition-colors group-hover:text-forest-600" />
        <span className="hidden md:inline">Jump to a page…</span>
        <kbd className="ml-auto hidden rounded-md border border-ink-200 bg-ink-50 px-1.5 py-0.5 font-sans text-[0.6875rem] font-semibold text-ink-500 md:inline">Ctrl K</kbd>
      </button>
      <Dialog open={open} onOpenChange={openChange}>
        <DialogContent showCloseButton={false} className="top-[18%] translate-y-0 gap-0 overflow-hidden p-0 sm:max-w-lg">
          <DialogTitle className="sr-only">Jump to a page</DialogTitle>
          <DialogDescription className="sr-only">Type to filter the admin pages, then press Enter.</DialogDescription>
          <div className="flex items-center gap-3 border-b px-4">
            <SearchIcon className="size-5 text-forest-600" />
            <input
              autoFocus
              value={query}
              onChange={(e) => {
                setQuery(e.target.value)
                setActive(0)
              }}
              onKeyDown={(e) => {
                if (e.key === 'ArrowDown') {
                  e.preventDefault()
                  setActive((i) => Math.min(i + 1, results.length - 1))
                } else if (e.key === 'ArrowUp') {
                  e.preventDefault()
                  setActive((i) => Math.max(i - 1, 0))
                } else if (e.key === 'Enter') {
                  go(results[active])
                }
              }}
              placeholder="Bookings, payments, packages…"
              className="h-14 flex-1 bg-transparent text-base outline-none placeholder:text-ink-400"
              aria-label="Page name"
            />
          </div>
          <ul className="grid max-h-80 gap-0.5 overflow-y-auto p-2" role="listbox" aria-label="Pages">
            {results.length === 0 && <li className="px-3 py-8 text-center text-sm text-ink-500">No page called “{query}”.</li>}
            {results.map((item, i) => {
              const Icon = item.icon
              return (
                <li key={item.to} role="option" aria-selected={i === active}>
                  <button
                    type="button"
                    onMouseEnter={() => setActive(i)}
                    onClick={() => go(item)}
                    className={cn(
                      'flex h-11 w-full items-center gap-3 rounded-xl px-3 text-sm font-medium transition-colors',
                      i === active ? 'bg-accent text-accent-foreground' : 'text-ink-700',
                    )}
                  >
                    <span className={cn('flex size-8 items-center justify-center rounded-lg', i === active ? 'bg-card text-forest-600 shadow-soft' : 'bg-ink-100 text-ink-500')}>
                      <Icon className="size-4" />
                    </span>
                    {item.label}
                    {i === active && <CornerDownLeftIcon className="ml-auto size-4 text-forest-500" />}
                  </button>
                </li>
              )
            })}
          </ul>
        </DialogContent>
      </Dialog>
    </>
  )
}
