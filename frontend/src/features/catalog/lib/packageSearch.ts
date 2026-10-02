import type { PackageSearchParams, PackageSort } from '../api/catalog.api'

/** Cards per page: 12 fills 2, 3 or 4 columns evenly. */
export const searchPageSize = 12

const sorts: readonly PackageSort[] = ['Recommended', 'PriceLow', 'PriceHigh', 'Newest']
const modes = ['FixedDepartures', 'FlexibleStay'] as const

/** The search page's state, as read from the URL: page and sort always have a value. */
export type PackageSearch = PackageSearchParams & { page: number; sort: PackageSort }

/**
 * URL → search. The URL is the ONLY place the filters live, so a refresh,
 * the Back button or a shared link always shows the same results. Anything
 * odd in the URL (an old or hand-typed link) is quietly ignored instead of
 * becoming an API error.
 */
export function readPackageSearch(search: URLSearchParams): PackageSearch {
  const text = (key: string) => search.get(key)?.trim() || undefined
  const price = (key: string) => {
    const value = search.get(key)
    return value && /^\d{1,9}$/.test(value) ? Number(value) : undefined
  }

  let minPrice = price('minPrice')
  let maxPrice = price('maxPrice')
  // The API rejects max < min; a swapped pair clearly means the other way round.
  if (minPrice !== undefined && maxPrice !== undefined && maxPrice < minPrice) [minPrice, maxPrice] = [maxPrice, minPrice]

  const page = Number(search.get('page'))
  return {
    q: text('q')?.slice(0, 100),
    destination: text('destination'),
    category: text('category'),
    mode: modes.find((m) => m === search.get('mode')),
    minPrice,
    maxPrice,
    sort: sorts.find((s) => s === search.get('sort')) ?? 'Recommended',
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    pageSize: searchPageSize,
  }
}

/** Search → URL, leaving out the defaults so links stay short: /packages?destination=sylhet */
export function toPackageSearch(search: PackageSearchParams): URLSearchParams {
  const result = new URLSearchParams()
  for (const [key, value] of Object.entries(search)) {
    if (value === undefined || value === '' || key === 'pageSize') continue
    if ((key === 'sort' && value === 'Recommended') || (key === 'page' && value === 1)) continue
    result.set(key, String(value))
  }
  return result
}

/** How many filters are on (the price range counts as one) - for the "Filters (2)" button. */
export function countFilters(search: PackageSearchParams): number {
  return [
    search.q,
    search.destination,
    search.category,
    search.mode,
    search.minPrice ?? search.maxPrice,
  ].filter((value) => value !== undefined).length
}

export const sortLabels: Record<PackageSort, string> = {
  Recommended: 'Recommended',
  PriceLow: 'Price: low to high',
  PriceHigh: 'Price: high to low',
  Newest: 'Newest',
}

export const modeLabels: Record<(typeof modes)[number], string> = {
  FixedDepartures: 'Fixed dates',
  FlexibleStay: 'Flexible stay',
}
