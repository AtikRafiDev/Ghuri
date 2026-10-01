import { useQueryClient } from '@tanstack/react-query'
import { ArchiveIcon, CircleAlertIcon, CircleCheckIcon, RocketIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { packageKeys, packagesApi, PackageStatus, type AdminPackage } from '../api/packages.api'

/**
 * Is the package live, and if not, what's stopping it? The problems come
 * from the API (publishProblems = the domain's own GetPublishProblems), so
 * this bar can never disagree with what Publish will actually do.
 */
export function PackagePublishBar({ pkg }: { pkg: AdminPackage }) {
  const queryClient = useQueryClient()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const run = async (action: (id: string) => Promise<void>) => {
    setBusy(true)
    setError(null)
    try {
      await action(pkg.id)
      // New status everywhere: this page's badge and every list page.
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
    } catch (e) {
      setError(toAppError(e).message)
    } finally {
      setBusy(false)
    }
  }

  const isPublished = pkg.status === PackageStatus.Published
  const problems = pkg.publishProblems
  const ready = problems.length === 0

  return (
    <div className="grid gap-3 rounded-lg border p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-sm">
          {isPublished ? (
            <>
              <CircleCheckIcon className="size-4 text-green-600" />
              <span>Live on the website.</span>
            </>
          ) : ready ? (
            <>
              <CircleCheckIcon className="size-4 text-green-600" />
              <span>Ready to publish.</span>
            </>
          ) : (
            <>
              <CircleAlertIcon className="size-4 text-amber-600" />
              <span>
                {pkg.status === PackageStatus.Archived ? 'Archived - hidden from the website. ' : 'Draft - hidden from the website. '}
                Before publishing:
              </span>
            </>
          )}
        </div>

        {isPublished ? (
          <Button variant="outline" disabled={busy} onClick={() => run(packagesApi.archive)}>
            {busy ? <Spinner /> : <ArchiveIcon />}
            Archive
          </Button>
        ) : (
          <Button disabled={busy || !ready} onClick={() => run(packagesApi.publish)}>
            {busy ? <Spinner /> : <RocketIcon />}
            Publish
          </Button>
        )}
      </div>

      {!isPublished && !ready && (
        <ul className="ml-6 list-disc text-sm text-muted-foreground">
          {problems.map((problem) => (
            <li key={problem}>{problem}</li>
          ))}
        </ul>
      )}

      {error && <FormAlert kind="error">{error}</FormAlert>}
    </div>
  )
}
