import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import type { Role } from '../auth.types'
import { useAuth } from '../useAuth'
import { RequireAuth } from './RequireAuth'

/**
 * Logged in AND holding one of `roles` - e.g. the admin panel. A customer
 * who types /admin is sent home. Like RequireAuth, this is only the
 * experience: the API's policies (AdminArea) do the real protecting.
 */
export function RequireRole({ roles, children }: { roles: readonly Role[]; children: ReactNode }) {
  return (
    <RequireAuth>
      <RoleCheck roles={roles}>{children}</RoleCheck>
    </RequireAuth>
  )
}

function RoleCheck({ roles, children }: { roles: readonly Role[]; children: ReactNode }) {
  const { hasAnyRole } = useAuth()
  return hasAnyRole(roles) ? children : <Navigate to="/" replace />
}
