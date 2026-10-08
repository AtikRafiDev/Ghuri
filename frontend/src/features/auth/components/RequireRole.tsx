import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import type { Role } from '../auth.types'
import { useAuth } from '../useAuth'
import { RequireAuth } from './RequireAuth'

type Props = {
  roles: readonly Role[]
  /** Where someone without the role goes. Default: the home page. */
  redirectTo?: string
  children: ReactNode
}

/**
 * Logged in AND holding one of `roles` - e.g. the admin panel. A customer
 * who types /admin is sent home. Like RequireAuth, this is only the
 * experience: the API's policies (AdminArea) do the real protecting.
 */
export function RequireRole({ roles, redirectTo = '/', children }: Props) {
  return (
    <RequireAuth>
      <RoleCheck roles={roles} redirectTo={redirectTo}>
        {children}
      </RoleCheck>
    </RequireAuth>
  )
}

function RoleCheck({ roles, redirectTo, children }: Required<Props>) {
  const { hasAnyRole } = useAuth()
  return hasAnyRole(roles) ? children : <Navigate to={redirectTo} replace />
}
