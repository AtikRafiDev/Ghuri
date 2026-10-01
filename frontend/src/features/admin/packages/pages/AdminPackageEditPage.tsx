import { useQuery } from '@tanstack/react-query'
import { ArrowLeftIcon } from 'lucide-react'
import { Link, useLocation, useParams, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { FormAlert } from '@/shared/components/FormAlert'
import { toAppError } from '@/shared/api/problem'
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
  // Set by the form after "Add package", which then opens this same page for the new id.
  const justCreated = (useLocation().state as { created?: boolean } | null)?.created === true

  const existing = useQuery({ ...packageQueryOptions(packageId ?? ''), enabled: packageId !== undefined })

  const backLink = (
    <Button asChild variant="ghost" size="sm" className="-ml-2 w-fit">
      <Link to="/admin/packages">
        <ArrowLeftIcon />
        All packages
      </Link>
    </Button>
  )

  if (packageId === undefined) {
    return (
      <div className="grid gap-6">
        <div className="grid gap-1">
          {backLink}
          <h1 className="text-2xl font-semibold">Add package</h1>
          <p className="text-muted-foreground">It starts as a draft - nobody sees it until it's published.</p>
        </div>
        <PackageForm />
      </div>
    )
  }

  if (existing.isPending) {
    return (
      <div className="grid gap-4">
        {backLink}
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (existing.isError) {
    const notFound = toAppError(existing.error).status === 404
    return (
      <div className="grid gap-4">
        {backLink}
        <FormAlert kind="error">{notFound ? 'This package does not exist or was deleted.' : existing.error.message}</FormAlert>
        {!notFound && (
          <Button variant="outline" className="w-fit" onClick={() => existing.refetch()}>
            Try again
          </Button>
        )}
      </div>
    )
  }

  const pkg = existing.data
  const isFixed = pkg.pricingMode === PricingMode.FixedDepartures
  return (
    <div className="grid gap-6">
      <div className="grid gap-1">
        {backLink}
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold">{pkg.title}</h1>
          <PackageStatusBadge status={pkg.status} />
        </div>
        <p className="text-muted-foreground">
          {pkg.packageCode} · /packages/{pkg.slug}
        </p>
      </div>

      {justCreated && (
        <FormAlert kind="success">
          Package created as a draft. Next: add photos and the itinerary in the tabs below, then publish.
        </FormAlert>
      )}

      <PackagePublishBar pkg={pkg} />

      {/* The open tab lives in the URL (?tab=photos): refresh and the back button keep it. */}
      <Tabs
        // A flexible package has no Departures tab - an old ?tab=departures link falls back to Details.
        value={tab === 'departures' && !isFixed ? 'details' : tab}
        onValueChange={(value) => setSearchParams({ tab: value }, { replace: true })}
      >
        <TabsList>
          <TabsTrigger value="details">Details</TabsTrigger>
          <TabsTrigger value="photos">Photos ({pkg.images.length})</TabsTrigger>
          <TabsTrigger value="itinerary">Itinerary ({pkg.itineraryDays.length})</TabsTrigger>
          {isFixed && <TabsTrigger value="departures">Departures</TabsTrigger>}
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

const tabs = ['details', 'photos', 'itinerary', 'departures'] as const
type Tab = (typeof tabs)[number]

function toTab(value: string | null): Tab {
  return tabs.includes(value as Tab) ? (value as Tab) : 'details'
}
