import { queryOptions } from '@tanstack/react-query'
import { http } from '@/shared/api/http'
import type { Paged } from '@/shared/api/paged'

// The API sends these enums as numbers (backend: Ghuri.Domain/Enums).
export const PricingMode = { FixedDepartures: 1, FlexibleStay: 2 } as const
export type PricingMode = (typeof PricingMode)[keyof typeof PricingMode]

export const PackageStatus = { Draft: 1, Published: 2, Archived: 3 } as const
export type PackageStatus = (typeof PackageStatus)[keyof typeof PackageStatus]

export const TourType = { Group: 1, Private: 2, Custom: 3 } as const
export type TourType = (typeof TourType)[keyof typeof TourType]

export const packageStatusLabels: Record<PackageStatus, string> = { 1: 'Draft', 2: 'Published', 3: 'Archived' }
export const tourTypeLabels: Record<TourType, string> = { 1: 'Group tour', 2: 'Private tour', 3: 'Custom tour' }

/** One row of GET /api/v1/admin/packages (backend: AdminPackageListItemDto). */
export type AdminPackageListItem = {
  id: string
  packageCode: string
  title: string
  slug: string
  destinationName: string
  pricingMode: PricingMode
  status: PackageStatus
  durationDays: number
  durationNights: number
  /** Flexible only. */
  minNights: number | null
  maxNights: number | null
  /** Per adult. 0 for a fixed package until it has departures (Day 5). */
  priceFrom: number
  currency: string
  isFeatured: boolean
  coverImageUrl: string | null
  publishedAtUtc: string | null
}

/** The form fields - the body of POST and PUT (backend: Create/UpdatePackageCommand). */
export type PackageRequest = {
  destinationId: string
  title: string
  /** null = made from the title. */
  slug: string | null
  summary: string
  description: string | null
  tourType: TourType
  categoryIds: string[]
  inclusions: string[]
  exclusions: string[]
  termsAndPolicy: string | null
  minAge: number | null
  isFeatured: boolean
  seoTitle: string | null
  seoDescription: string | null
  pricingMode: PricingMode
  // Fixed departures:
  durationDays: number | null
  durationNights: number | null
  // Flexible stay:
  minNights: number | null
  maxNights: number | null
  /** Per adult, covers minNights. */
  basePrice: number | null
  /** Per adult, each night after minNights. */
  extraNightPrice: number | null
  minLeadDays: number | null
}

/** GET /api/v1/admin/packages/{id} (backend: AdminPackageDto). */
export type AdminPackage = Omit<PackageRequest, 'durationDays' | 'durationNights' | 'slug'> & {
  id: string
  packageCode: string
  status: PackageStatus
  slug: string
  durationDays: number
  durationNights: number
  priceFrom: number
  currency: string
  publishedAtUtc: string | null
  /** In display order - the first is the cover. */
  images: { fileId: string; url: string }[]
  itineraryDays: { dayNo: number; title: string; description: string; meals: string | null; accommodation: string | null }[]
  /** What still stops it going live; empty = ready to publish. */
  publishProblems: string[]
}

/** One day in PUT .../itinerary (backend: ItineraryDayInput). */
export type ItineraryDayRequest = { title: string; description: string; meals: string | null; accommodation: string | null }

/** Must match the API's TourPackage.MaxImages. */
export const maxPackageImages = 15

/** Must match the API's PackagePricing.MaxDays. */
export const maxItineraryDays = 60

/** The lists the form's dropdowns need (public endpoints). */
export type DestinationOption = { id: string; name: string; countryName: string; isInternational: boolean }
export type CategoryOption = { id: string; name: string; icon: string | null }

export type PackageListParams = {
  search: string
  status: PackageStatus | null
  pricingMode: PricingMode | null
  page: number
}

export const packagesPageSize = 20
const base = '/api/v1/admin/packages'

export const packagesApi = {
  async list({ search, status, pricingMode, page }: PackageListParams): Promise<Paged<AdminPackageListItem>> {
    const { data } = await http.get<Paged<AdminPackageListItem>>(base, {
      // undefined = left out of the URL entirely
      params: {
        search: search || undefined,
        status: status ?? undefined,
        pricingMode: pricingMode ?? undefined,
        page,
        pageSize: packagesPageSize,
      },
    })
    return data
  },

  async get(id: string): Promise<AdminPackage> {
    const { data } = await http.get<AdminPackage>(`${base}/${id}`)
    return data
  },

  async create(body: PackageRequest): Promise<string> {
    const { data } = await http.post<{ id: string }>(base, body)
    return data.id
  },

  async update(id: string, body: PackageRequest): Promise<void> {
    await http.put(`${base}/${id}`, body)
  },

  /** The whole gallery, in display order - the first is the cover. */
  async setImages(id: string, imageFileIds: string[]): Promise<void> {
    await http.put(`${base}/${id}/images`, { imageFileIds })
  },

  /** The whole itinerary - the first day in the list is Day 1. */
  async saveItinerary(id: string, days: ItineraryDayRequest[]): Promise<void> {
    await http.put(`${base}/${id}/itinerary`, { days })
  },

  async publish(id: string): Promise<void> {
    await http.post(`${base}/${id}/publish`)
  },

  async archive(id: string): Promise<void> {
    await http.post(`${base}/${id}/archive`)
  },

  async destinations(): Promise<DestinationOption[]> {
    const { data } = await http.get<DestinationOption[]>('/api/v1/destinations')
    return data
  },

  async categories(): Promise<CategoryOption[]> {
    const { data } = await http.get<CategoryOption[]>('/api/v1/categories')
    return data
  },
}

/**
 * Cache keys. Everything starts with ['admin', 'packages'], so ONE
 * invalidate after a save refreshes every list page and the open package.
 */
export const packageKeys = {
  all: ['admin', 'packages'] as const,
  list: (params: PackageListParams) => [...packageKeys.all, 'list', params] as const,
  detail: (id: string) => [...packageKeys.all, 'detail', id] as const,
}

export const packageQueryOptions = (id: string) =>
  queryOptions({ queryKey: packageKeys.detail(id), queryFn: () => packagesApi.get(id) })

// The form's dropdown lists change rarely - a minute of caching is plenty.
export const destinationOptionsQuery = queryOptions({
  queryKey: ['package-form', 'destinations'],
  queryFn: packagesApi.destinations,
  staleTime: 60_000,
})

export const categoryOptionsQuery = queryOptions({
  queryKey: ['package-form', 'categories'],
  queryFn: packagesApi.categories,
  staleTime: 60_000,
})

/** "৳8,000" - Bangladeshi digit grouping (en-IN groups thousands the same way). */
export function formatTaka(amount: number): string {
  return `৳${amount.toLocaleString('en-IN', { maximumFractionDigits: 2 })}`
}

/** "3 days / 2 nights" or "2–7 nights". */
export function describeDuration(p: Pick<AdminPackageListItem, 'pricingMode' | 'durationDays' | 'durationNights' | 'minNights' | 'maxNights'>): string {
  if (p.pricingMode === PricingMode.FlexibleStay && p.minNights !== null && p.maxNights !== null) {
    return p.minNights === p.maxNights ? `${p.minNights} nights` : `${p.minNights}–${p.maxNights} nights`
  }
  return `${p.durationDays} days / ${p.durationNights} nights`
}
