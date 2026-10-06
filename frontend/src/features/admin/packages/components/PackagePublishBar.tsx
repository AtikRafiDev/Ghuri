import { useQueryClient } from '@tanstack/react-query'
import { ArchiveIcon, CircleAlertIcon, CircleCheckIcon, CircleDashedIcon, GlobeIcon, RocketIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { notify } from '@/shared/lib/notify'
import { packageKeys, packagesApi, PackageStatus, type AdminPackage } from '../api/packages.api'

/**
 * Is the package live, and if not, what's stopping it? The problems come
 * from the API (publishProblems = the domain's own GetPublishProblems), so
 * this bar can never disagree with what Publish will actually do.
 *
 * A status banner in the matching tone: green when it's live (or ready to
 * go), amber with a checklist while something is still missing.
 */
export function PackagePublishBar({ pkg }: { pkg: AdminPackage }) {
  const queryClient = useQueryClient()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const run = async (action: (id: string) => Promise<void>, done: { title: string; description: string }) => {
    setBusy(true)
    setError(null)
    try {
      await action(pkg.id)
      // New status everywhere: this page's badge and every list page.
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      notify.success(done.title, { description: done.description })
    } catch (e) {
      setError(toAppError(e).message)
    } finally {
      setBusy(false)
    }
  }

  const isPublished = pkg.status === PackageStatus.Published
  const isArchived = pkg.status === PackageStatus.Archived
  const problems = pkg.publishProblems
  const ready = problems.length === 0
  const tone = isPublished ? 'live' : ready ? 'ready' : 'todo'

  return (
    <section
      aria-label="Publishing"
      className={cn(
        'grid animate-fade-up gap-4 rounded-3xl p-5 ring-1 sm:p-6',
        tone === 'live' && 'bg-forest-50 ring-forest-200/70',
        tone === 'ready' && 'bg-forest-50/60 ring-forest-100',
        tone === 'todo' && 'bg-sun-50 ring-sun-100',
      )}
    >
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex min-w-0 items-center gap-3.5">
          <span
            className={cn(
              'flex size-11 shrink-0 items-center justify-center rounded-2xl [&>svg]:size-5',
              tone === 'live' && 'bg-forest-600 text-white shadow-soft',
              tone === 'ready' && 'bg-forest-100 text-forest-700',
              tone === 'todo' && 'bg-sun-100 text-sun-700',
            )}
          >
            {tone === 'live' ? <GlobeIcon /> : tone === 'ready' ? <CircleCheckIcon /> : <CircleAlertIcon />}
          </span>
          <div className="grid min-w-0 gap-0.5">
            <p className="flex items-center gap-2 font-bold text-ink-900">
              {isPublished ? (
                <>
                  {/* A softly pulsing "on air" dot. */}
                  <span className="relative flex size-2" aria-hidden>
                    <span className="absolute inline-flex size-full animate-ping rounded-full bg-forest-400 opacity-75" />
                    <span className="relative inline-flex size-2 rounded-full bg-forest-500" />
                  </span>
                  Live on the website
                </>
              ) : ready ? (
                'Ready to publish'
              ) : isArchived ? (
                'Archived - hidden from the website'
              ) : (
                'Draft - hidden from the website'
              )}
            </p>
            <p className="text-sm text-ink-600">
              {isPublished
                ? 'Customers can find and book it. Archive it to take it off the website.'
                : ready
                  ? isArchived
                    ? 'Archived - hidden from the website. Publish to bring it back.'
                    : 'Everything is in place - publish to put it on the website.'
                  : `Before publishing, ${problems.length === 1 ? 'one thing is' : `${problems.length} things are`} still missing:`}
            </p>
          </div>
        </div>

        {isPublished ? (
          <Button
            variant="outline"
            disabled={busy}
            onClick={() => run(packagesApi.archive, { title: 'Package archived', description: 'It is hidden from the website now.' })}
          >
            {busy ? <Spinner /> : <ArchiveIcon />}
            Archive
          </Button>
        ) : (
          <Button
            disabled={busy || !ready}
            onClick={() => run(packagesApi.publish, { title: 'Package published', description: 'It is live on the website now.' })}
          >
            {busy ? <Spinner /> : <RocketIcon />}
            Publish
          </Button>
        )}
      </div>

      {!isPublished && !ready && (
        // Lined up under the title text, not under the icon.
        <ul className="grid gap-2 sm:pl-[3.625rem]">
          {problems.map((problem) => (
            <li key={problem} className="flex items-start gap-2.5 rounded-xl bg-card/80 px-3.5 py-2.5 text-sm text-ink-700 ring-1 ring-sun-100">
              <CircleDashedIcon className="mt-0.5 size-4 shrink-0 text-sun-500" aria-hidden />
              {problem}
            </li>
          ))}
        </ul>
      )}

      {error && <FormAlert kind="error">{error}</FormAlert>}
    </section>
  )
}
