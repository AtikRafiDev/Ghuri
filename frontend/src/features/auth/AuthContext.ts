import { createContext } from 'react'
import type { Me, RegisterRequest, Role } from './auth.types'
import type { LoginInput } from './schemas/login.schema'

/**
 * checking      - page just loaded; trying to resume the session from the cookie
 * authenticated - logged in, `user` is loaded
 * anonymous     - not logged in
 */
export type AuthStatus = 'checking' | 'authenticated' | 'anonymous'

export type AuthContextValue = {
  status: AuthStatus
  user: Me | null
  login(input: LoginInput): Promise<void>
  register(input: RegisterRequest): Promise<void>
  logout(): Promise<void>
  hasAnyRole(roles: readonly Role[]): boolean
}

// In its own file (not AuthProvider.tsx): the fast-refresh lint rule wants
// component files to export components only.
export const AuthContext = createContext<AuthContextValue | null>(null)
