import axios, { type InternalAxiosRequestConfig } from 'axios'
import { getAccessToken } from './accessToken'
import { toAppError } from './problem'
import { refreshSession } from './session'

// The ONE axios instance every API call goes through (blueprint 13.1:
// shared/api/http.ts), so cross-cutting behaviour lives in exactly one
// place: attaching the token, silently refreshing it, typed errors.
export const http = axios.create({
  // Empty base URL = "same address the page came from". In development
  // that's Vite, whose proxy forwards to the API; in production it'll be
  // Nginx doing the same. No hard-coded server address in app code.
  baseURL: '',
  timeout: 15_000,
})

// 1. EVERY request: attach the access token, if there is one.
http.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// These answer 401 for their OWN reasons (wrong password, no session) - a
// refresh can't fix that, so they must never trigger one.
const authPathsWithoutRefresh = ['/api/v1/auth/login', '/api/v1/auth/register', '/api/v1/auth/refresh', '/api/v1/auth/logout']

type RetryableRequest = InternalAxiosRequestConfig & { alreadyRetried?: boolean }

// 2. A 401 elsewhere = "access token missing or expired" (it lives only 15
//    minutes). Refresh once, then repeat the original request once. The
//    user never notices.
http.interceptors.response.use(undefined, async (error: unknown) => {
  if (axios.isAxiosError(error) && error.response?.status === 401 && error.config) {
    const request = error.config as RetryableRequest
    const isAuthPath = authPathsWithoutRefresh.some((path) => request.url?.startsWith(path))

    if (!request.alreadyRetried && !isAuthPath) {
      request.alreadyRetried = true
      // null = the session is really over. refreshSession already set the
      // token to null, so AuthProvider switches to "logged out" and guarded
      // pages redirect to /login.
      const token = await refreshSession().catch(() => null)
      if (token) return http(request) // interceptor 1 attaches the NEW token
    }
  }

  // Every caller receives the same typed error (ProblemDetails → AppError).
  throw toAppError(error)
})
