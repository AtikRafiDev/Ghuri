import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from './AuthContext'

/** Who is logged in, and login/logout. Usable in any component inside <AuthProvider>. */
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth() must be used inside <AuthProvider>.')
  return context
}
