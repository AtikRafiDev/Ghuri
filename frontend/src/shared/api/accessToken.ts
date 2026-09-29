// The access token lives HERE - in a plain JavaScript variable - and
// nowhere else. Not in localStorage or sessionStorage: every script on the
// page can read those, so a single XSS bug would leak the login
// (blueprint 13.2: "access token in memory only").
// A page reload forgets it on purpose; the HttpOnly refresh cookie (which
// JavaScript can't read at all) then fetches a new one - see session.ts.

/** What the API returns from login, register, refresh and change-password. */
export type AccessTokenResponse = { accessToken: string; expiresAtUtc: string }

let accessToken: string | null = null
const listeners = new Set<() => void>()

export function getAccessToken(): string | null {
  return accessToken
}

export function setAccessToken(token: string | null): void {
  if (token === accessToken) return
  accessToken = token
  // Tell React (AuthProvider) - e.g. a failed refresh sets null here, and
  // every page guarded by RequireAuth immediately redirects to /login.
  listeners.forEach((notify) => notify())
}

/** For React's useSyncExternalStore: re-render when the token changes. */
export function subscribeToAccessToken(listener: () => void): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
