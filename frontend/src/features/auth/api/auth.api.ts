import { queryOptions } from '@tanstack/react-query'
import { setAccessToken, type AccessTokenResponse } from '@/shared/api/accessToken'
import { http } from '@/shared/api/http'
import type { Me, RegisterRequest } from '../auth.types'
import type { LoginInput } from '../schemas/login.schema'

const base = '/api/v1/auth'

type ResetPasswordRequest = { email: string; token: string; newPassword: string }

/** Every call to /api/v1/auth, in one place (blueprint 13.1: features own their API calls). */
export const authApi = {
  async login(input: LoginInput): Promise<void> {
    const { data } = await http.post<AccessTokenResponse>(`${base}/login`, input)
    setAccessToken(data.accessToken) // the refresh cookie was set by the response itself
  },

  async register(input: RegisterRequest): Promise<void> {
    const { data } = await http.post<AccessTokenResponse>(`${base}/register`, input)
    setAccessToken(data.accessToken)
  },

  async logout(): Promise<void> {
    try {
      await http.post(`${base}/logout`) // revokes the session + deletes the cookie
    } finally {
      setAccessToken(null) // logged out locally even if the API couldn't be reached
    }
  },

  async forgotPassword(email: string): Promise<void> {
    await http.post(`${base}/password/forgot`, { email })
  },

  async resetPassword(input: ResetPasswordRequest): Promise<void> {
    await http.post(`${base}/password/reset`, input)
  },

  async me(): Promise<Me> {
    const { data } = await http.get<Me>(`${base}/me`)
    return data
  },
}

/**
 * The logged-in user, cached by TanStack Query under one key. Components
 * read it through useAuth(); logout clears the whole cache.
 */
export const meQueryOptions = queryOptions({
  queryKey: ['auth', 'me'],
  queryFn: authApi.me,
  staleTime: 5 * 60_000,
})
