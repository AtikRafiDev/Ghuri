import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { CloudOffIcon, FileSearchIcon, SearchIcon, SearchXIcon, SlidersHorizontalIcon, XIcon } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Sheet, SheetClose, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet'
import { cn } from '@/lib/utils'
import { EmptyState } from '@/shared/components/EmptyState'
import { PageHeader } from '@/shared/components/PageHeader'
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
  const total = packages.data?.totalCount

  const filters = (
    <PackageFilters search={search} onChange={update} destinations={destinations} categories={categories} />
  )

  return (
    <div className="grid gap-8">
      <div className="grid gap-5">
        <PageHeader title={heading} description="Group departures on set dates and flexible stays on yours - book and pay online." />
        {/* key: when q changes in the URL (Clear, Back), the box shows the new text. */}
        <SearchBox key={search.q ?? ''} initial={search.q ?? ''} onSearch={(q) => update({ q: q || undefined })} />
      </div>

      {/*
        Two columns on a laptop. Each column starts with a 40px-tall bar (the "Filters" title | the count + sort)
        and then its panel, with the same gap - so the two bars sit on one line and the panels start level.
      */}
      <div className="grid gap-8 lg:grid-cols-[18rem_minmax(0,1fr)] lg:items-start">
        {/* Laptop: filters in a card that stays in view below the sticky site header. */}
        <aside className="hidden lg:sticky lg:top-24 lg:grid lg:gap-4" aria-label="Filters">
          <div className="flex h-10 items-center justify-between gap-3">
            <h2 className="flex items-center gap-2 text-base font-bold text-ink-900">
              <SlidersHorizontalIcon className="size-4 text-forest-600" />
              Filters
              {filterCount > 0 && (
                <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-[0.6875rem] font-bold text-white">
                  {filterCount}
                </span>
              )}
            </h2>
            {filterCount > 0 && (
              <Button variant="link" size="sm" onClick={clearFilters}>
                Clear all
              </Button>
            )}
          </div>
          <div className="rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80">{filters}</div>
        </aside>

        <section className="grid min-w-0 gap-4" aria-label="Results">
          <div className="flex min-h-10 flex-wrap items-center justify-between gap-3">
            <p className="text-sm text-ink-500" aria-live="polite">
              {total !== undefined ? (
                <>
                  <span className="font-bold text-ink-900">{total}</span> package{total === 1 ? '' : 's'}
                </>
              ) : (
                ' '
              )}
            </p>
            <div className="flex items-center gap-2 max-sm:w-full">
              {/* Phone: the same filters in a sheet that slides in from the side. */}
              <Sheet>
                <SheetTrigger asChild>
                  <Button variant="outline" className="max-sm:min-w-0 max-sm:flex-1 lg:hidden">
                    <SlidersHorizontalIcon />
                    Filters
                    {filterCount > 0 && (
                      <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-primary px-1.5 text-[0.6875rem] font-bold text-white">
                        {filterCount}
                      </span>
                    )}
                  </Button>
                </SheetTrigger>
                <SheetContent side="left" className="w-[85%] overflow-y-auto">
                  <SheetHeader className="border-b">
                    <SheetTitle>Filters</SheetTitle>
                    <SheetDescription>The list updates as you choose.</SheetDescription>
                  </SheetHeader>
                  <div className="px-4">{filters}</div>
                  <SheetFooter className="border-t">
                    <SheetClose asChild>
                      <Button>
                        {total !== undefined ? `Show ${total} package${total === 1 ? '' : 's'}` : 'Show results'}
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
                <SelectTrigger className="min-w-0 flex-1 sm:w-60 sm:flex-none" aria-label="Sort by">
                  <span className="text-ink-400 max-sm:hidden">Sort:</span>
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
            <EmptyState icon={CloudOffIcon} title="Packages couldn’t be loaded" text="Check your connection and try again.">
              <Button variant="outline" onClick={() => packages.refetch()}>
                Try again
              </Button>
            </EmptyState>
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
                'stagger grid gap-5 transition-opacity duration-300 sm:grid-cols-2 xl:grid-cols-3',
                packages.isPlaceholderData && 'pointer-events-none opacity-60',
              )}
              aria-busy={packages.isFetching}
            >
              {packages.isPending && Array.from({ length: 6 }, (_, i) => <PackageCardSkeleton key={i} />)}
              {packages.data?.items.map((pkg) => <PackageCard key={pkg.id} pkg={pkg} />)}
            </div>
          )}

          {packages.data && (
            <div className="pt-4">
              <Pagination
                page={search.page}
                totalPages={packages.data.totalPages}
                hrefFor={(page) => `?${toPackageSearch({ ...search, page })}`}
              />
            </div>
          )}
        </section>
      </div>
    </div>
  )
}

/**
 * Free-text search (title or destination). Searches on Enter / the button, not on every key.
 * Field and button are both the standard 40px, so their edges line up.
 */
function SearchBox({ initial, onSearch }: { initial: string; onSearch: (q: string) => void }) {
  const [text, setText] = useState(initial)
  const onSubmit = (event: FormEvent) => {
    event.preventDefault()
    onSearch(text.trim())
  }

  return (
    <form onSubmit={onSubmit} role="search" className="flex gap-2">
      <div className="relative min-w-0 flex-1">
        <SearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-ink-400" />
        <Input
          type="search"
          value={text}
          onChange={(e) => setText(e.target.value)}
          maxLength={100}
          placeholder="Search by place or tour name"
          aria-label="Search packages"
          className="pl-10"
        />
      </div>
      <Button type="submit" className="sm:px-6">
        <SearchIcon className="sm:hidden" />
        <span className="max-sm:sr-only">Search</span>
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
        <li key={chip.label} className="animate-scale-in">
          <button
            type="button"
            onClick={() => onClear(chip.clear)}
            className="group flex h-8 items-center gap-1.5 rounded-full bg-forest-50 pr-1.5 pl-3 text-xs font-semibold text-forest-800 ring-1 ring-forest-200 transition-colors ring-inset hover:bg-forest-100 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none"
            aria-label={`Remove filter: ${chip.label}`}
          >
            {chip.label}
            <span className="flex size-5 items-center justify-center rounded-full bg-card/80 text-forest-700 transition-colors group-hover:bg-primary group-hover:text-white">
              <XIcon className="size-3" />
            </span>
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
      <EmptyState icon={FileSearchIcon} title="There’s nothing on this page" text="The list has fewer pages now - start again from the first one.">
        <Button variant="outline" onClick={onFirstPage}>
          Go to the first page
        </Button>
      </EmptyState>
    )
  }

  return (
    <EmptyState
      icon={SearchXIcon}
      title="No packages found"
      text={hasFilters ? 'Try fewer filters or a different destination.' : 'New packages are on their way - check back soon.'}
    >
      {hasFilters && (
        <Button variant="outline" onClick={onClear}>
          Clear all filters
        </Button>
      )}
    </EmptyState>
  )
}

/** "৳5,000 – ৳15,000", "From ৳5,000", "Up to ৳15,000". */
function describePrice(min: number | undefined, max: number | undefined): string {
  if (min !== undefined && max !== undefined) return `${formatTaka(min)} – ${formatTaka(max)}`
  if (min !== undefined) return `From ${formatTaka(min)}`
  return `Up to ${formatTaka(max ?? 0)}`
}
