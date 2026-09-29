import axios from 'axios'
import { setAccessToken, type AccessTokenResponse } from './accessToken'
import { toAppError } from './problem'

const refreshUrl = '/api/v1/auth/refresh'

// How long to wait before the one retry (see refreshWithOneRetry).
const otherTabGraceMs = 300

let refreshing: Promise<string | null> | null = null

/**
 * Trades the HttpOnly refresh cookie for a new access token (the browser
 * sends the cookie by itself). Resolves to the new token, or null when
 * there is no valid session - i.e. the user is simply not logged in.
 *
 * ONE shared request at a time: if ten API calls get a 401 together, or
 * React's StrictMode runs the startup effect twice, they all wait for the
 * SAME refresh. Sending one refresh token twice would look like token
 * theft to the API (blueprint 13.2: "one shared refresh promise").
 */
export function refreshSession(): Promise<string | null> {
  refreshing ??= refreshWithOneRetry().finally(() => {
    refreshing = null
  })
  return refreshing
}

type Attempt = { kind: 'ok'; token: string } | { kind: 'expired' } | { kind: 'none' }

async function refreshWithOneRetry(): Promise<string | null> {
  let attempt = await tryRefresh()

  // "session_expired" can mean ANOTHER TAB refreshed a moment ago: it spent
  // the cookie we just sent, and the browser now holds the new one. So try
  // exactly once more; if the session really ended, that fails too.
  if (attempt.kind === 'expired') {
    await new Promise((resolve) => setTimeout(resolve, otherTabGraceMs))
    attempt = await tryRefresh()
  }

  const token = attempt.kind === 'ok' ? attempt.token : null
  setAccessToken(token)
  return token
}

async function tryRefresh(): Promise<Attempt> {
  try {
    // Plain axios, NOT the shared `http` instance: this request must never
    // trigger http.ts's "401 → refresh" handling itself (an endless loop).
    const { data } = await axios.post<AccessTokenResponse>(refreshUrl)
    return { kind: 'ok', token: data.accessToken }
  } catch (error) {
    const appError = toAppError(error)
    if (appError.status !== 401) throw appError // e.g. API down - a real problem, not "logged out"
    return appError.code === 'session_expired' ? { kind: 'expired' } : { kind: 'none' }
  }
}
