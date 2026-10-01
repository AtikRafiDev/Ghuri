import {
  ActivityIcon,
  ExternalLinkIcon,
  LayoutDashboardIcon,
  LogOutIcon,
  MapPinIcon,
  PackageIcon,
  TagsIcon,
  type LucideIcon,
} from 'lucide-react'
import { useState } from 'react'
import { Link, NavLink, Outlet, useLocation, useMatch, useNavigate } from 'react-router'
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
import { useAuth } from '@/features/auth/useAuth'

type MenuItem = { to: string; label: string; icon: LucideIcon; end?: boolean }

// The admin menu, in groups. Later days add Bookings, Payments... here.
const adminMenu: { label: string; items: MenuItem[] }[] = [
  { label: 'Overview', items: [{ to: '/admin', label: 'Dashboard', icon: LayoutDashboardIcon, end: true }] },
  {
    label: 'Catalogue',
    items: [
      { to: '/admin/packages', label: 'Packages', icon: PackageIcon },
      { to: '/admin/destinations', label: 'Destinations', icon: MapPinIcon },
      { to: '/admin/categories', label: 'Categories', icon: TagsIcon },
    ],
  },
  { label: 'System', items: [{ to: '/admin/system', label: 'System health', icon: ActivityIcon }] },
]

/**
 * The admin panel (/admin/...), desktop-first (blueprint 13): a sidebar
 * that collapses to icons (Ctrl/⌘+B) and becomes a slide-in drawer on a
 * phone. Guarded by RequireRole(staff) in the router.
 */
export function AdminLayout() {
  const { pathname } = useLocation()
  const current = adminMenu
    .flatMap((group) => group.items)
    .find((item) => (item.end ? pathname === item.to : pathname.startsWith(item.to)))

  return (
    <SidebarProvider>
      <Sidebar collapsible="icon">
        <SidebarHeader>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton asChild size="lg" tooltip="Ghuri admin">
                <Link to="/admin">
                  <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary font-semibold text-primary-foreground">
                    G
                  </span>
                  <span className="font-semibold">Ghuri admin</span>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarHeader>

        <SidebarContent>
          {adminMenu.map((group) => (
            <SidebarGroup key={group.label}>
              <SidebarGroupLabel>{group.label}</SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  {group.items.map((item) => (
                    <AdminMenuLink key={item.to} item={item} />
                  ))}
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          ))}
        </SidebarContent>

        <SidebarFooter>
          <AdminUserMenu />
        </SidebarFooter>
        {/* The thin strip on the sidebar's edge: click it to collapse/expand. */}
        <SidebarRail />
      </Sidebar>

      <SidebarInset>
        <header className="flex h-14 shrink-0 items-center gap-2 border-b px-4">
          <SidebarTrigger />
          <Separator orientation="vertical" className="mr-1 h-4" />
          <span className="text-sm font-medium">{current?.label ?? 'Admin'}</span>
        </header>
        <div className="min-w-0 flex-1 p-4 md:p-6">
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

/** Bottom of the sidebar: who is logged in, back to the website, log out. */
function AdminUserMenu() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [loggingOut, setLoggingOut] = useState(false)

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
    <SidebarMenu>
      <SidebarMenuItem>
        <SidebarMenuButton asChild tooltip="View website">
          <Link to="/">
            <ExternalLinkIcon />
            <span>View website</span>
          </Link>
        </SidebarMenuButton>
      </SidebarMenuItem>
      <SidebarMenuItem>
        <SidebarMenuButton onClick={handleLogout} disabled={loggingOut} tooltip="Log out">
          <LogOutIcon />
          <span className="truncate">Log out {user ? `(${user.fullName})` : ''}</span>
        </SidebarMenuButton>
      </SidebarMenuItem>
    </SidebarMenu>
  )
}
