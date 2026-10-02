import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { SearchIcon, SlidersHorizontalIcon, XIcon } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Sheet, SheetClose, SheetContent, SheetFooter, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet'
import { cn } from '@/lib/utils'
import { Pagination } from '@/shared/components/Pagination'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { categoriesQuery, destinationsQuery, packagesQuery, type PackageSearchParams, type PackageSort } from '../api/catalog.api'
import { PackageCard, PackageCardSkeleton } from '../components/PackageCard'
import { PackageFilters } from '../components/PackageFilters'
import { countFilters, modeLabels, readPackageSearch, sortLabels, toPackageSearch } from '../lib/packageSearch'

/**
 * The search page, /packages (17-day plan, Day 7). Every filter lives in
 * the URL - /packages?destination=sylhet&mode=FlexibleStay&page=2 - so a
 * refresh, Back, or a link sent on WhatsApp shows exactly the same list.
 * The home page's search, banner, destination and category links all land here.
 */
export function PackagesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const search = readPackageSearch(searchParams)

  // keepPreviousData: while the next page or filter loads, the current cards stay (faded) instead of blanking.
  const packages = useQuery({ ...packagesQuery(search), placeholderData: keepPreviousData })
  const destinations = useQuery(destinationsQuery(false)).data ?? []
  const categories = useQuery(categoriesQuery).data ?? []

  const destinationName = destinations.find((d) => d.slug === search.destination)?.name
  const categoryName = categories.find((c) => c.slug === search.category)?.name
  const heading = destinationName ? `Tours in ${destinationName}` : categoryName ? `${categoryName} tours` : 'Tour packages'
  useDocumentMeta({ title: heading, description: 'Browse tour packages - fixed group departures and flexible stays. Book and pay online.' })

  // Any filter change starts again at page 1: "page 3" of the old results means nothing for the new ones.
  const update = (changes: Partial<PackageSearchParams>) => setSearchParams(toPackageSearch({ ...search, ...changes, page: 1 }))
  const clearFilters = () => setSearchParams(toPackageSearch({ sort: search.sort }))
  const filterCount = countFilters(search)

  const filters = (
    <PackageFilters search={search} onChange={update} destinations={destinations} categories={categories} />
  )

  return (
    <div className="grid gap-6">
      <header className="grid gap-4">
        <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">{heading}</h1>
        {/* key: when q changes in the URL (Clear, Back), the box shows the new text. */}
        <SearchBox key={search.q ?? ''} initial={search.q ?? ''} onSearch={(q) => update({ q: q || undefined })} />
      </header>

      <div className="grid gap-8 lg:grid-cols-[15rem_minmax(0,1fr)] lg:items-start">
        {/* Laptop: filters in a sidebar that stays in view. */}
        <aside className="hidden lg:sticky lg:top-4 lg:grid lg:gap-4">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">Filters</h2>
            {filterCount > 0 && (
              <Button variant="link" size="sm" className="h-auto px-0" onClick={clearFilters}>
                Clear all
              </Button>
            )}
          </div>
          {filters}
        </aside>

        <section className="grid gap-4" aria-label="Results">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-sm text-muted-foreground" aria-live="polite">
              {packages.data ? `${packages.data.totalCount} package${packages.data.totalCount === 1 ? '' : 's'}` : ' '}
            </p>
            <div className="flex items-center gap-2">
              {/* Phone: the same filters in a sheet that slides in from the side. */}
              <Sheet>
                <SheetTrigger asChild>
                  <Button variant="outline" size="sm" className="lg:hidden">
                    <SlidersHorizontalIcon />
                    Filters{filterCount > 0 && ` (${filterCount})`}
                  </Button>
                </SheetTrigger>
                <SheetContent side="left" className="w-[85%] overflow-y-auto">
                  <SheetHeader>
                    <SheetTitle>Filters</SheetTitle>
                  </SheetHeader>
                  <div className="px-4">{filters}</div>
                  <SheetFooter>
                    <SheetClose asChild>
                      <Button>
                        {packages.data ? `Show ${packages.data.totalCount} package${packages.data.totalCount === 1 ? '' : 's'}` : 'Show results'}
                      </Button>
                    </SheetClose>
                    {filterCount > 0 && (
                      <Button variant="ghost" onClick={clearFilters}>
                        Clear all
                      </Button>
                    )}
                  </SheetFooter>
                </SheetContent>
              </Sheet>

              <Select value={search.sort} onValueChange={(v) => update({ sort: v as PackageSort })}>
                <SelectTrigger size="sm" className="w-44" aria-label="Sort by">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent position="popper" align="end">
                  {Object.entries(sortLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <ActiveFilters
            chips={[
              search.q && { label: `“${search.q}”`, clear: { q: undefined } },
              search.destination && { label: destinationName ?? search.destination, clear: { destination: undefined } },
              search.category && { label: categoryName ?? search.category, clear: { category: undefined } },
              search.mode && { label: modeLabels[search.mode], clear: { mode: undefined } },
              (search.minPrice !== undefined || search.maxPrice !== undefined) && {
                label: describePrice(search.minPrice, search.maxPrice),
                clear: { minPrice: undefined, maxPrice: undefined },
              },
            ]}
            onClear={update}
          />

          {packages.isError ? (
            <div className="rounded-lg border py-12 text-center text-sm">
              <p className="text-muted-foreground">Packages couldn’t be loaded.</p>
              <Button variant="outline" size="sm" className="mt-3" onClick={() => packages.refetch()}>
                Try again
              </Button>
            </div>
          ) : packages.data && packages.data.items.length === 0 ? (
            <EmptyResults
              pastLastPage={search.page > 1}
              hasFilters={filterCount > 0}
              onFirstPage={() => setSearchParams(toPackageSearch({ ...search, page: 1 }))}
              onClear={clearFilters}
            />
          ) : (
            <div
              className={cn(
                'grid gap-4 transition-opacity sm:grid-cols-2 xl:grid-cols-3',
                packages.isPlaceholderData && 'pointer-events-none opacity-60',
              )}
              aria-busy={packages.isFetching}
            >
              {packages.isPending && Array.from({ length: 6 }, (_, i) => <PackageCardSkeleton key={i} />)}
              {packages.data?.items.map((pkg) => <PackageCard key={pkg.id} pkg={pkg} />)}
            </div>
          )}

          {packages.data && (
            <Pagination
              page={search.page}
              totalPages={packages.data.totalPages}
              hrefFor={(page) => `?${toPackageSearch({ ...search, page })}`}
            />
          )}
        </section>
      </div>
    </div>
  )
}

/** Free-text search (title or destination). Searches on Enter / the button, not on every key. */
function SearchBox({ initial, onSearch }: { initial: string; onSearch: (q: string) => void }) {
  const [text, setText] = useState(initial)
  const onSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSearch(text.trim())
  }

  return (
    <form onSubmit={onSubmit} role="search" className="flex gap-2">
      <div className="relative flex-1">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          type="search"
          value={text}
          onChange={(e) => setText(e.target.value)}
          maxLength={100}
          placeholder="Search by place or tour name"
          aria-label="Search packages"
          className="h-10 pl-8"
        />
      </div>
      <Button type="submit" size="lg" className="h-10">
        Search
      </Button>
    </form>
  )
}

type Chip = { label: string; clear: Partial<PackageSearchParams> }

/** One removable chip per active filter - on a phone, the only sign of filters hidden in the sheet. */
function ActiveFilters({ chips, onClear }: { chips: (Chip | false | undefined | '')[]; onClear: (changes: Partial<PackageSearchParams>) => void }) {
  const active = chips.filter((chip): chip is Chip => Boolean(chip))
  if (active.length === 0) return null

  return (
    <ul className="flex flex-wrap gap-2" aria-label="Active filters">
      {active.map((chip) => (
        <li key={chip.label}>
          <button
            type="button"
            onClick={() => onClear(chip.clear)}
            className="flex items-center gap-1 rounded-full border bg-muted/50 py-1 pr-2 pl-3 text-xs font-medium hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
            aria-label={`Remove filter: ${chip.label}`}
          >
            {chip.label}
            <XIcon className="size-3.5" />
          </button>
        </li>
      ))}
    </ul>
  )
}

function EmptyResults({
  pastLastPage,
  hasFilters,
  onFirstPage,
  onClear,
}: {
  pastLastPage: boolean
  hasFilters: boolean
  onFirstPage: () => void
  onClear: () => void
}) {
  // E.g. an old link to page 4 after packages were removed: there are results, just not on this page.
  if (pastLastPage) {
    return (
      <div className="rounded-lg border py-12 text-center text-sm">
        <p className="text-muted-foreground">There’s nothing on this page.</p>
        <Button variant="outline" size="sm" className="mt-3" onClick={onFirstPage}>
          Go to the first page
        </Button>
      </div>
    )
  }

  return (
    <div className="rounded-lg border py-12 text-center">
      <p className="font-medium">No packages found</p>
      <p className="mt-1 text-sm text-muted-foreground">
        {hasFilters ? 'Try fewer filters or a different destination.' : 'New packages are on their way - check back soon.'}
      </p>
      {hasFilters && (
        <Button variant="outline" size="sm" className="mt-4" onClick={onClear}>
          Clear all filters
        </Button>
      )}
    </div>
  )
}

/** "৳5,000 – ৳15,000", "From ৳5,000", "Up to ৳15,000". */
function describePrice(min: number | undefined, max: number | undefined): string {
  if (min !== undefined && max !== undefined) return `${formatTaka(min)} – ${formatTaka(max)}`
  if (min !== undefined) return `From ${formatTaka(min)}`
  return `Up to ${formatTaka(max ?? 0)}`
}
