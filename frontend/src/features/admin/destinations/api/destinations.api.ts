import { queryOptions } from '@tanstack/react-query'
import { http } from '@/shared/api/http'
import type { Paged } from '@/shared/api/paged'

export type DestinationScope = 'national' | 'international'

/** One row of GET /api/v1/admin/destinations (backend: AdminDestinationDto). */
export type AdminDestination = {
  id: string
  name: string
  slug: string
  summary: string | null
  countryId: number
  countryName: string
  countryIsoCode: string
  isInternational: boolean
  /** The photo gallery in display order - the first is the cover. */
  images: { fileId: string; url: string }[]
  isFeatured: boolean
  sortOrder: number
  seoTitle: string | null
  seoDescription: string | null
  /** Above 0 = the API refuses to delete it. */
  packageCount: number
}

/** GET /api/v1/countries (backend: CountryDto). */
export type Country = { id: number; name: string; isoCode: string }

/** Body of POST and PUT (backend: Create/UpdateDestinationCommand). */
export type DestinationRequest = {
  countryId: number
  name: string
  /** null = made from the name. */
  slug: string | null
  summary: string | null
  /** Ids of already-uploaded files, in display order - the first is the cover. */
  imageFileIds: string[]
  isFeatured: boolean
  sortOrder: number
  seoTitle: string | null
  seoDescription: string | null
}

/** Must match the API's Destination.MaxImages. */
export const maxDestinationImages = 10

export type DestinationListParams = { search: string; scope: DestinationScope | null; page: number }

export const destinationsPageSize = 20
const base = '/api/v1/admin/destinations'

export const destinationsApi = {
  async list({ search, scope, page }: DestinationListParams): Promise<Paged<AdminDestination>> {
    const { data } = await http.get<Paged<AdminDestination>>(base, {
      // undefined = left out of the URL entirely
      params: { search: search || undefined, scope: scope ?? undefined, page, pageSize: destinationsPageSize },
    })
    return data
  },

  async create(body: DestinationRequest): Promise<string> {
    const { data } = await http.post<{ id: string }>(base, body)
    return data.id
  },

  async update(id: string, body: DestinationRequest): Promise<void> {
    await http.put(`${base}/${id}`, body)
  },

  async remove(id: string): Promise<void> {
    await http.delete(`${base}/${id}`)
  },

  async countries(): Promise<Country[]> {
    const { data } = await http.get<Country[]>('/api/v1/countries')
    return data
  },
}

/**
 * Cache keys. Everything starts with ['admin', 'destinations'], so ONE
 * invalidate after a save/delete refreshes every page and filter at once.
 */
export const destinationKeys = {
  all: ['admin', 'destinations'] as const,
  list: (params: DestinationListParams) => [...destinationKeys.all, 'list', params] as const,
}

/** The country list practically never changes - fetch it once per visit. */
export const countriesQueryOptions = queryOptions({
  queryKey: ['countries'],
  queryFn: destinationsApi.countries,
  staleTime: Infinity,
})
