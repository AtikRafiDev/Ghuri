import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { useAuth } from '../useAuth'

/**
 * Shows its content only to logged-in users; everyone else goes to /login,
 * which sends them back here afterwards.
 *
 * This is for a smooth EXPERIENCE only - the real protection is the API's
 * [Authorize]: someone who edits this JavaScript still gets 401 from the
 * API (blueprint 13: "frontend guards are for user experience only").
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'checking') return <PageSpinner />
  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }
  return children
}
