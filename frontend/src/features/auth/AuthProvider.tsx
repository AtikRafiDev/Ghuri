import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react'
import { getAccessToken, subscribeToAccessToken } from '@/shared/api/accessToken'
import { refreshSession } from '@/shared/api/session'
import { authApi, meQueryOptions } from './api/auth.api'
import { AuthContext, type AuthContextValue, type AuthStatus } from './AuthContext'
import type { RegisterRequest, Role } from './auth.types'
import type { LoginInput } from './schemas/login.schema'

/**
 * Knows who is logged in and shares it with every component (blueprint
 * 13.2: "auth session in React context + memory").
 *
 * The flow:
 * 1. Page loads: the access token is gone (memory only). Ask /auth/refresh
 *    once - if the HttpOnly cookie is still valid, we're logged in again.
 * 2. Whenever an access token exists, load /auth/me (name, roles).
 * 3. When the token becomes null - logout, or a failed refresh deep inside
 *    some API call - everything here switches to "anonymous" by itself.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [resumeAttempted, setResumeAttempted] = useState(false)

  // Re-renders this component whenever shared/api/accessToken.ts changes.
  const accessToken = useSyncExternalStore(subscribeToAccessToken, getAccessToken)

  useEffect(() => {
    // StrictMode runs this twice in development - harmless: refreshSession
    // shares one in-flight request, so the cookie is still sent only once.
    refreshSession()
      .catch(() => null) // API unreachable: treat as "not logged in"
      .finally(() => setResumeAttempted(true))
  }, [])

  const meQuery = useQuery({ ...meQueryOptions, enabled: accessToken !== null })
  const user = accessToken !== null ? (meQuery.data ?? null) : null

  const status: AuthStatus =
    !resumeAttempted || (accessToken !== null && meQuery.isPending)
      ? 'checking'
      : user
        ? 'authenticated'
        : 'anonymous'

  const login = useCallback(
    async (input: LoginInput) => {
      await authApi.login(input)
      // Load the profile BEFORE resolving, so the page that awaited login()
      // already knows the roles (to pick /admin or /account).
      await queryClient.fetchQuery(meQueryOptions)
    },
    [queryClient],
  )

  const register = useCallback(
    async (input: RegisterRequest) => {
      await authApi.register(input)
      await queryClient.fetchQuery(meQueryOptions)
    },
    [queryClient],
  )

  const logout = useCallback(async () => {
    await authApi.logout()
    // Forget EVERY cached answer - the next person on this computer must
    // never see the previous user's bookings or profile.
    queryClient.clear()
  }, [queryClient])

  const hasAnyRole = useCallback(
    (roles: readonly Role[]) => user?.roles.some((role) => roles.includes(role)) ?? false,
    [user],
  )

  const value = useMemo<AuthContextValue>(
    () => ({ status, user, login, register, logout, hasAnyRole }),
    [status, user, login, register, logout, hasAnyRole],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
