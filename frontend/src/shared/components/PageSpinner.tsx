import { PackingLoader } from '@/shared/loader/PackingLoader'

/** Full-area loading state (blueprint 13.2: every screen has a loading state) - the packing-bags animation. */
export function PageSpinner() {
  return (
    <div className="flex min-h-[55vh] items-center justify-center">
      <PackingLoader />
    </div>
  )
}
