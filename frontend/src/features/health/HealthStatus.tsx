import { useQueries, useQueryClient } from '@tanstack/react-query'
import { ActivityIcon, CircleAlertIcon, CircleCheckIcon, LoaderIcon, RefreshCwIcon, TriangleAlertIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { PageHeader } from '@/shared/components/PageHeader'
import { fetchHealth, healthEndpoints, type HealthEndpoint, type HealthState } from './healthApi'

// How often each check re-asks the API, so the page stays current without
// a manual refresh (stop the API or the database and watch it change).
const refreshEveryMs = 10_000

/** One visual state per situation - blueprint 13.2: every data screen has loading, error and success states. */
type CheckState = 'loading' | 'healthy' | 'degraded' | 'down'

type Check = {
  endpoint: HealthEndpoint
  state: CheckState
  text: string
  errorMessage: string | null
  checkedAt: number
  isFetching: boolean
}

// What each state looks like: the dot's colour and the words' colour. Never colour alone - the words say it too.
const tones: Record<CheckState, { dot: string; text: string }> = {
  loading: { dot: 'bg-ink-300', text: 'text-ink-500' },
  healthy: { dot: 'bg-forest-500', text: 'text-forest-700' },
  degraded: { dot: 'bg-sun-500', text: 'text-sun-700' },
  down: { dot: 'bg-clay-500', text: 'text-clay-600' },
}

const timeOf = (ms: number) => new Date(ms).toLocaleTimeString()

/** Admin → System: is the API up, and can it reach the database? */
export function HealthStatus() {
  const queryClient = useQueryClient()

  // useQueries replaces hand-written useEffect + useState + loading flags:
  // TanStack Query tracks loading / success / error for each check itself,
  // caches the result under its queryKey, and re-fetches on the interval.
  const results = useQueries({
    queries: healthEndpoints.map((endpoint) => ({
      queryKey: ['health', endpoint.path],
      queryFn: () => fetchHealth(endpoint.path),
      refetchInterval: refreshEveryMs,
      // A health check should report failure straight away, not quietly
      // retry 3 times first (TanStack Query's default).
      retry: false,
    })),
  })

  const checks: Check[] = results.map(({ data, isPending, isError, error, dataUpdatedAt, errorUpdatedAt, isFetching }, i) => ({
    endpoint: healthEndpoints[i],
    state: isPending ? 'loading' : isError ? 'down' : data === 'Healthy' ? 'healthy' : data === 'Degraded' ? 'degraded' : 'down',
    text: isPending ? 'Checking…' : isError ? 'API unreachable' : (data as HealthState),
    errorMessage: isError ? error.message : null,
    checkedAt: Math.max(dataUpdatedAt, errorUpdatedAt),
    isFetching,
  }))

  // The summary at the top is the worst of the three.
  const overall: CheckState = checks.some((c) => c.state === 'down')
    ? 'down'
    : checks.some((c) => c.state === 'loading')
      ? 'loading'
      : checks.some((c) => c.state === 'degraded')
        ? 'degraded'
        : 'healthy'
  const anyFetching = checks.some((c) => c.isFetching)
  const lastChecked = Math.max(...checks.map((c) => c.checkedAt))

  return (
    <div className="grid gap-6">
      <PageHeader
        title="System health"
        description={`Is the API up, and can it reach the database? Checked again every ${refreshEveryMs / 1000} seconds.`}
        actions={
          <Button variant="outline" onClick={() => queryClient.refetchQueries({ queryKey: ['health'] })} disabled={anyFetching}>
            <RefreshCwIcon className={cn(anyFetching && 'animate-spin')} />
            Check now
          </Button>
        }
      />

      <section className="animate-fade-up overflow-hidden rounded-3xl bg-card shadow-card ring-1 ring-ink-200/80">
        {/* The verdict: a dark brand panel, its icon tinted by how things stand. */}
        <header className="brand-surface relative isolate flex items-center gap-4 overflow-hidden bg-gradient-to-br from-forest-800 to-forest-950 p-5 text-white sm:gap-5 sm:p-6">
          <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
          <span
            className={cn(
              'flex size-12 shrink-0 items-center justify-center rounded-2xl ring-8 sm:size-14',
              overall === 'healthy' && 'bg-forest-500/25 text-forest-200 ring-forest-500/10',
              overall === 'degraded' && 'bg-sun-500/25 text-sun-300 ring-sun-500/10',
              overall === 'down' && 'bg-clay-500/30 text-clay-100 ring-clay-500/15',
              overall === 'loading' && 'bg-white/10 text-forest-100 ring-white/5',
            )}
          >
            {overall === 'healthy' && <CircleCheckIcon className="size-6 sm:size-7" />}
            {overall === 'degraded' && <TriangleAlertIcon className="size-6 sm:size-7" />}
            {overall === 'down' && <CircleAlertIcon className="size-6 sm:size-7" />}
            {overall === 'loading' && <LoaderIcon className="size-6 animate-spin sm:size-7" />}
          </span>
          <div className="grid min-w-0 gap-0.5" role="status">
            <h2 className="text-lg font-bold sm:text-xl">
              {overall === 'healthy' && 'All systems running'}
              {overall === 'degraded' && 'Running, with warnings'}
              {overall === 'down' && 'Something is down'}
              {overall === 'loading' && 'Checking the system…'}
            </h2>
            <p className="text-sm text-forest-100/80">
              {lastChecked > 0 ? `Last checked ${timeOf(lastChecked)}` : 'Asking the API…'}
              {anyFetching && lastChecked > 0 && ' · refreshing…'}
            </p>
          </div>
          <ActivityIcon aria-hidden className="absolute -right-4 -bottom-6 size-36 text-white/5" />
        </header>

        <ul className="divide-y divide-ink-100">
          {checks.map((check) => (
            <CheckRow key={check.endpoint.path} check={check} />
          ))}
        </ul>
      </section>
    </div>
  )
}

/** One check as a row: a live dot, what it checks, the endpoint, and its answer. */
function CheckRow({ check }: { check: Check }) {
  const { endpoint, state, text, errorMessage, checkedAt } = check
  const tone = tones[state]
  return (
    <li className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-4 gap-y-1 px-5 py-4 sm:px-6">
      {/* The dot breathes while things are fine (a ring ripples out of it); it sits still when something's wrong. */}
      <span aria-hidden className="relative flex size-3">
        {state === 'healthy' && <span className={cn('absolute inset-0 animate-[ring-ping_1.8s_ease-out_infinite] rounded-full', tone.dot)} />}
        <span className={cn('relative size-3 rounded-full ring-4', tone.dot, state === 'healthy' ? 'ring-forest-100' : state === 'down' ? 'ring-clay-100' : state === 'degraded' ? 'ring-sun-100' : 'ring-ink-100')} />
      </span>
      <div className="grid min-w-0 gap-0.5">
        <span className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
          <span className="font-semibold text-ink-900">{endpoint.label}</span>
          <code className="rounded-md bg-ink-50 px-1.5 py-0.5 font-mono text-[0.6875rem] text-ink-500 ring-1 ring-ink-200/70 ring-inset">GET {endpoint.path}</code>
        </span>
        <span className="text-sm text-ink-500">{endpoint.meaning}</span>
        {errorMessage && <span className="text-sm font-medium text-clay-600">{errorMessage}</span>}
      </div>
      <div className="grid justify-items-end gap-0.5 text-right">
        <span className={cn('text-sm font-bold', tone.text)}>{text}</span>
        {checkedAt > 0 && <span className="nums text-xs text-ink-400">{timeOf(checkedAt)}</span>}
      </div>
    </li>
  )
}
