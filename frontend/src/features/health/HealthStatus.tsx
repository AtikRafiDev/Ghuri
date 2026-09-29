import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'
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
    <Card
      className={cn(
        'border-l-4',
        state === 'healthy' && 'border-l-green-600',
        (state === 'unhealthy' || state === 'error') && 'border-l-destructive',
        state === 'loading' && 'border-l-muted-foreground',
      )}
    >
      <CardHeader>
        <CardTitle>{endpoint.label}</CardTitle>
        <CardDescription>
          <code>GET {endpoint.path}</code>
        </CardDescription>
      </CardHeader>
      <CardContent className="grid gap-1">
        <p
          className={cn(
            'text-2xl font-semibold',
            state === 'healthy' && 'text-green-600',
            (state === 'unhealthy' || state === 'error') && 'text-destructive',
            state === 'loading' && 'text-muted-foreground',
          )}
        >
          {text}
        </p>
        <p className="text-sm text-muted-foreground">{endpoint.meaning}</p>
      </CardContent>
      <CardFooter className="flex-col items-start gap-0.5 text-sm text-muted-foreground">
        {isError && <span className="text-destructive">{error.message}</span>}
        {dataUpdatedAt > 0 && (
          <span>
            Last checked {new Date(dataUpdatedAt).toLocaleTimeString()}
            {isFetching && ' · refreshing…'}
          </span>
        )}
      </CardFooter>
    </Card>
  )
}

/** Admin → System: is the API up, and can it reach the database? */
export function HealthStatus() {
  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">System health</h1>
        <p className="text-muted-foreground">Refreshes every {refreshEveryMs / 1000} seconds.</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {healthEndpoints.map((endpoint) => (
          <HealthCard key={endpoint.path} endpoint={endpoint} />
        ))}
      </div>
    </div>
  )
}
