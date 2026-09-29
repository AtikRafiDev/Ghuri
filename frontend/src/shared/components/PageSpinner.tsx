import { Spinner } from '@/components/ui/spinner'

/** Full-area loading state (blueprint 13.2: every screen has a loading state). */
export function PageSpinner() {
  return (
    <div className="flex min-h-[50vh] items-center justify-center">
      <Spinner className="size-6 text-muted-foreground" />
    </div>
  )
}
