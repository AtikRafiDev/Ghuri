import { useQuery } from '@tanstack/react-query'
import { fetchHealth, healthEndpoints, type HealthEndpoint } from './healthApi'

// How often each card re-asks the API, so the page stays current without
// a manual refresh (stop the API or the database and watch it change).
const refreshEveryMs = 10_000

function HealthCard({ endpoint }: { endpoint: HealthEndpoint }) {
  // useQuery replaces hand-written useEffect + useState + loading flags:
  // TanStack Query tracks loading / success / error itself, caches the
  // result under the queryKey, and re-fetches on the interval.
  const { data, isPending, isError, error, dataUpdatedAt, isFetching } = useQuery({
    queryKey: ['health', endpoint.path],
    queryFn: () => fetchHealth(endpoint.path),
    refetchInterval: refreshEveryMs,
    // A health check should report failure straight away, not quietly
    // retry 3 times first (TanStack Query's default).
    retry: false,
  })

  // One visual state per situation - blueprint 13.2: every data screen has
  // loading, error and success states.
  const state = isPending ? 'loading' : isError ? 'error' : data === 'Healthy' ? 'healthy' : 'unhealthy'
  const text = isPending ? 'Checking…' : isError ? 'API unreachable' : data

  return (
    <article className={`health-card ${state}`}>
      <header>
        <h2>{endpoint.label}</h2>
        <code>GET {endpoint.path}</code>
      </header>
      <p className="health-state">{text}</p>
      <p className="health-meaning">{endpoint.meaning}</p>
      <footer>
        {isError && <span className="health-error">{error.message}</span>}
        {dataUpdatedAt > 0 && (
          <span>
            Last checked {new Date(dataUpdatedAt).toLocaleTimeString()}
            {isFetching && ' · refreshing…'}
          </span>
        )}
      </footer>
    </article>
  )
}

export function HealthStatus() {
  return (
    <section>
      <h1>Ghuri API health</h1>
      <p className="subtitle">Refreshes every {refreshEveryMs / 1000} seconds.</p>
      <div className="health-grid">
        {healthEndpoints.map((endpoint) => (
          <HealthCard key={endpoint.path} endpoint={endpoint} />
        ))}
      </div>
    </section>
  )
}
