import { http } from '../../shared/api/http'

// The three states ASP.NET Core health checks can report. A union of string
// literals instead of a TypeScript `enum`: this project's tsconfig has
// `erasableSyntaxOnly` on, which (by design) forbids enums.
export type HealthState = 'Healthy' | 'Degraded' | 'Unhealthy'

export type HealthEndpoint = {
  path: '/health/live' | '/health/ready' | '/health'
  label: string
  meaning: string
}

export const healthEndpoints: HealthEndpoint[] = [
  { path: '/health/live', label: 'Live', meaning: 'Is the API process running? (checks nothing else)' },
  { path: '/health/ready', label: 'Ready', meaning: 'Can it serve real requests? (checks the database)' },
  { path: '/health', label: 'Overall', meaning: 'Everything - what an uptime monitor watches' },
]

export async function fetchHealth(path: HealthEndpoint['path']): Promise<HealthState> {
  const response = await http.get<string>(path, {
    responseType: 'text',
    // By default axios treats any status >= 400 as a thrown error. But a 503
    // here is a VALID answer ("Unhealthy") the page should display, not a
    // crash - so 503 counts as a normal response. Anything else (e.g. the
    // API not running at all) still throws and shows as an error.
    validateStatus: (status) => status === 200 || status === 503,
  })
  return response.data.trim() as HealthState
}
