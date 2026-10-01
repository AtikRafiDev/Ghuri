import { queryOptions } from '@tanstack/react-query'
import { http } from '@/shared/api/http'
import type { Paged } from '@/shared/api/paged'

// The PUBLIC catalogue - no login needed (backend: PackagesController,
// CatalogController). The admin has its own, richer types.

/** One package card (backend: PackageCardDto). pricingMode 1 = fixed departures, 2 = flexible stay. */
export type PackageCard = {
  id: string
  slug: string
  title: string
  summary: string
  destinationName: string
  coverImageUrl: string | null
  pricingMode: 1 | 2
  durationDays: number
  durationNights: number
  minNights: number | null
  maxNights: number | null
  /** Per adult, worked out live by the API. null = a fixed package with no upcoming dates. */
  priceFrom: number | null
  currency: string
  isFeatured: boolean
}

/** GET /api/v1/destinations (backend: DestinationDto). */
export type DestinationSummary = {
  id: string
  name: string
  slug: string
  summary: string | null
  countryName: string
  isInternational: boolean
  imageUrl: string | null
  isFeatured: boolean
}

/** GET /api/v1/categories (backend: CategoryDto). */
export type CategorySummary = { id: string; name: string; slug: string; icon: string | null }

export type PackageSort = 'Recommended' | 'PriceLow' | 'PriceHigh' | 'Newest'

export type PackageSearchParams = {
  q?: string
  destination?: string
  category?: string
  minPrice?: number
  maxPrice?: number
  mode?: 'FixedDepartures' | 'FlexibleStay'
  sort?: PackageSort
  page?: number
  pageSize?: number
}

export const catalogApi = {
  async searchPackages(params: PackageSearchParams): Promise<Paged<PackageCard>> {
    const { data } = await http.get<Paged<PackageCard>>('/api/v1/packages', { params })
    return data
  },

  async destinations(featuredOnly: boolean): Promise<DestinationSummary[]> {
    const { data } = await http.get<DestinationSummary[]>('/api/v1/destinations', {
      params: featuredOnly ? { featured: true } : undefined,
    })
    return data
  },

  async categories(): Promise<CategorySummary[]> {
    const { data } = await http.get<CategorySummary[]>('/api/v1/categories')
    return data
  },
}

/** Cache keys. Public lists change rarely: a few minutes of caching keeps the site fast. */
export const catalogKeys = {
  all: ['catalog'] as const,
  packages: (params: PackageSearchParams) => [...catalogKeys.all, 'packages', params] as const,
  destinations: (featuredOnly: boolean) => [...catalogKeys.all, 'destinations', { featuredOnly }] as const,
  categories: () => [...catalogKeys.all, 'categories'] as const,
}

const fiveMinutes = 5 * 60_000

export const packagesQuery = (params: PackageSearchParams) =>
  queryOptions({ queryKey: catalogKeys.packages(params), queryFn: () => catalogApi.searchPackages(params), staleTime: fiveMinutes })

export const destinationsQuery = (featuredOnly: boolean) =>
  queryOptions({ queryKey: catalogKeys.destinations(featuredOnly), queryFn: () => catalogApi.destinations(featuredOnly), staleTime: fiveMinutes })

export const categoriesQuery = queryOptions({ queryKey: catalogKeys.categories(), queryFn: catalogApi.categories, staleTime: fiveMinutes })
