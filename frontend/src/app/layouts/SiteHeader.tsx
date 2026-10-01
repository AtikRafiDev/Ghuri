import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { staffRoles } from '@/features/auth/auth.types'
import { useAuth } from '@/features/auth/useAuth'
import { site } from '@/shared/config/site'

/** The top bar on every page: brand on the left, account actions on the right. */
export function SiteHeader() {
  const { status, user, hasAnyRole, logout } = useAuth()
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
    <header className="border-b bg-background">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
        <div className="flex items-center gap-4">
          <Link to="/" className="text-lg font-semibold tracking-tight">
            {site.name}
          </Link>
          <Button asChild variant="ghost" size="sm">
            <Link to="/packages">Packages</Link>
          </Button>
        </div>
        <nav className="flex items-center gap-1">
          {status === 'authenticated' && user && (
            <>
              {hasAnyRole(staffRoles) && (
                <Button asChild variant="ghost">
                  <Link to="/admin">Admin</Link>
                </Button>
              )}
              <Button asChild variant="ghost" className="max-w-40 truncate">
                <Link to="/account">{user.fullName}</Link>
              </Button>
              <Button variant="outline" onClick={handleLogout} disabled={loggingOut}>
                Log out
              </Button>
            </>
          )}
          {status === 'anonymous' && (
            <>
              <Button asChild variant="ghost">
                <Link to="/login">Log in</Link>
              </Button>
              <Button asChild>
                <Link to="/register">Sign up</Link>
              </Button>
            </>
          )}
          {/* status 'checking': show nothing for that split second, rather than a wrong button */}
        </nav>
      </div>
    </header>
  )
}
