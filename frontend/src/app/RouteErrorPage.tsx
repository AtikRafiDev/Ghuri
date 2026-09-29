import { Button } from '@/components/ui/button'

/**
 * Shown when a page crashes while rendering, or its code can't be
 * downloaded (e.g. we deployed a new version while this tab was open and
 * the old file is gone). A reload fixes both cases in practice.
 */
export function RouteErrorPage() {
  return (
    <section className="grid min-h-svh place-content-center justify-items-center gap-4 p-4 text-center">
      <h1 className="text-2xl font-semibold">Something went wrong</h1>
      <p className="max-w-sm text-muted-foreground">Please reload the page. If it keeps happening, try again in a few minutes.</p>
      <Button onClick={() => window.location.reload()}>Reload</Button>
    </section>
  )
}
