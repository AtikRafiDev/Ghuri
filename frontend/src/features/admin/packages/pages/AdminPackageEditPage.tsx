import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon, CalendarDaysIcon, FileTextIcon, ImagesIcon, ListOrderedIcon } from 'lucide-react'
import { useEffect, type ReactNode } from 'react'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { toAppError } from '@/shared/api/problem'
import { notify } from '@/shared/lib/notify'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { packageQueryOptions, PricingMode } from '../api/packages.api'
import { PackageDeparturesTab } from '../components/PackageDeparturesTab'
import { PackageForm } from '../components/PackageForm'
import { PackageItineraryTab } from '../components/PackageItineraryTab'
import { PackagePhotosTab } from '../components/PackagePhotosTab'
import { PackagePublishBar } from '../components/PackagePublishBar'
import { PackageStatusBadge } from '../components/PackageStatusBadge'

/**
 * /admin/packages/new and /admin/packages/:packageId - a full page, not a
 * dialog: the package form is too long for a pop-up. An existing package
 * gets the publish bar and the Details / Photos / Itinerary tabs
 * (Departures joins them for fixed packages).
 */
export function AdminPackageEditPage() {
  const { packageId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const tab = toTab(searchParams.get('tab'))
  const navigate = useNavigate()
  const { pathname, search, state } = useLocation()
  // Set by the form after "Add package", which then opens this same page for the new id.
  const justCreated = (state as { created?: boolean } | null)?.created === true

  const existing = useQuery({ ...packageQueryOptions(packageId ?? ''), enabled: packageId !== undefined })
  const loaded = existing.isSuccess
  useDocumentMeta({ title: packageId === undefined ? 'Add package' : (existing.data?.title ?? 'Package') })

  // "Created!" is a toast, shown once the new package is on screen. Then the
  // flag is wiped from the history entry, so a refresh doesn't say it again.
  // (The fixed id stops a double toast when React runs effects twice in development.)
  useEffect(() => {
    if (!justCreated || !loaded) return
    notify.success('Package created as a draft', {
      id: 'package-created',
      description: 'Next: add photos and the itinerary in the tabs below, then publish.',
    })
    navigate(pathname + search, { replace: true, state: null })
  }, [justCreated, loaded, navigate, pathname, search])

  const backLink = (
    <Button asChild variant="ghost" size="sm" className="-ml-3 text-ink-500 hover:text-forest-700">
      <Link to="/admin/packages">
        <ArrowLeftIcon className="group-hover/button:-translate-x-0.5" />
        All packages
      </Link>
    </Button>
  )

  if (packageId === undefined) {
    return (
      <div className="grid gap-6">
        <PageHeader eyebrow={backLink} title="Add package" description="It starts as a draft - nobody sees it until it's published." />
        <PackageForm />
      </div>
    )
  }

  if (existing.isPending) {
    return (
      <div className="grid gap-6" role="status" aria-label="Loading the package">
        <div className="grid gap-3">
          {backLink}
          <Skeleton className="h-8 w-80 max-w-full" />
          <Skeleton className="h-4 w-64 max-w-full" />
        </div>
        <Skeleton className="h-24 w-full rounded-3xl" />
        <Skeleton className="h-11 w-96 max-w-full rounded-xl" />
        <Skeleton className="h-96 w-full rounded-3xl" />
      </div>
    )
  }

  if (existing.isError) {
    const notFound = toAppError(existing.error).status === 404
    return (
      <div className="grid gap-6">
        <PageHeader eyebrow={backLink} title={notFound ? 'Package not found' : 'Could not load the package'} />
        <div className="grid justify-items-start gap-3">
          <FormAlert kind="error">{notFound ? 'This package does not exist or was deleted.' : toAppError(existing.error).message}</FormAlert>
          {!notFound && (
            <Button variant="outline" onClick={() => existing.refetch()}>
              Try again
            </Button>
          )}
        </div>
      </div>
    )
  }

  const pkg = existing.data
  const isFixed = pkg.pricingMode === PricingMode.FixedDepartures
  return (
    <div className="grid gap-6">
      <PageHeader
        eyebrow={backLink}
        title={pkg.title}
        titleAside={<PackageStatusBadge status={pkg.status} />}
        description={
          <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <span className="rounded-md bg-ink-100 px-1.5 py-0.5 font-mono text-xs font-semibold text-ink-600">{pkg.packageCode}</span>
            <span>/packages/{pkg.slug}</span>
          </span>
        }
      />

      <PackagePublishBar pkg={pkg} />

      {/* The open tab lives in the URL (?tab=photos): refresh and the back button keep it. */}
      <Tabs
        // A flexible package has no Departures tab - an old ?tab=departures link falls back to Details.
        value={tab === 'departures' && !isFixed ? 'details' : tab}
        onValueChange={(value) => setSearchParams({ tab: value }, { replace: true })}
        // min-w-0: lets the tab strip scroll on a phone instead of widening the page.
        className="min-w-0 gap-5"
      >
        <TabsList className="w-full justify-start sm:w-fit">
          <TabsTrigger value="details" className={tabTrigger}>
            <FileTextIcon className="hidden sm:block" />
            Details
          </TabsTrigger>
          <TabsTrigger value="photos" className={tabTrigger}>
            <ImagesIcon className="hidden sm:block" />
            Photos <TabCount>{pkg.images.length}</TabCount>
          </TabsTrigger>
          <TabsTrigger value="itinerary" className={tabTrigger}>
            <ListOrderedIcon className="hidden sm:block" />
            Itinerary <TabCount>{pkg.itineraryDays.length}</TabCount>
          </TabsTrigger>
          {isFixed && (
            <TabsTrigger value="departures" className={tabTrigger}>
              <CalendarDaysIcon className="hidden sm:block" />
              Departures
            </TabsTrigger>
          )}
        </TabsList>

        {/* forceMount + hidden: every tab stays mounted, so switching tabs never
            throws away unsaved typing. "key" starts each tab fresh when a
            DIFFERENT package is opened. */}
        <TabsContent value="details" forceMount className="data-[state=inactive]:hidden">
          <PackageForm key={pkg.id} pkg={pkg} />
        </TabsContent>
        <TabsContent value="photos" forceMount className="data-[state=inactive]:hidden">
          <PackagePhotosTab key={pkg.id} pkg={pkg} />
        </TabsContent>
        <TabsContent value="itinerary" forceMount className="data-[state=inactive]:hidden">
          <PackageItineraryTab key={pkg.id} pkg={pkg} />
        </TabsContent>
        {isFixed && (
          // Not forceMount: it has no unsaved typing (edits happen in a dialog),
          // so it can load its list only when opened.
          <TabsContent value="departures">
            <PackageDeparturesTab pkg={pkg} />
          </TabsContent>
        )}
      </Tabs>
    </div>
  )
}

// A little less padding on a phone, so all four tabs fit across the screen.
const tabTrigger = 'group/tab px-2 sm:px-3.5'

/** The small count pill on a tab - green on the open tab. */
function TabCount({ children }: { children: ReactNode }) {
  return (
    <span className="nums inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-ink-200/70 px-1.5 text-[0.6875rem] font-bold text-ink-600 transition-colors group-data-[state=active]/tab:bg-forest-100 group-data-[state=active]/tab:text-forest-800">
      {children}
    </span>
  )
}

const tabs = ['details', 'photos', 'itinerary', 'departures'] as const
type Tab = (typeof tabs)[number]

function toTab(value: string | null): Tab {
  return tabs.includes(value as Tab) ? (value as Tab) : 'details'
}
