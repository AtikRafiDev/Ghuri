import { keepPreviousData, queryOptions, skipToken } from '@tanstack/react-query'
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

/** GET /api/v1/packages/{slug} (backend: PackageDetailsDto). Departure dates come separately - see AvailableDeparture. */
export type PackageDetails = {
  id: string
  slug: string
  title: string
  summary: string
  description: string | null
  destinationName: string
  destinationSlug: string
  countryName: string
  /** 1 = group, 2 = private, 3 = custom. */
  tourType: 1 | 2 | 3
  categories: { name: string; slug: string; icon: string | null }[]
  inclusions: string[]
  exclusions: string[]
  termsAndPolicy: string | null
  minAge: number | null
  /** 1 = fixed departures, 2 = flexible stay. */
  pricingMode: 1 | 2
  durationDays: number
  durationNights: number
  /** Flexible stays only: the nights range, the base price (covers minNights) and each extra night, per adult. */
  minNights: number | null
  maxNights: number | null
  basePrice: number | null
  extraNightPrice: number | null
  /** Flexible stays only: today + lead days, as "yyyy-MM-dd" - the first date the picker allows. */
  earliestStartDate: string | null
  currency: string
  /** Cover image first. */
  imageUrls: string[]
  itinerary: ItineraryDay[]
  seoTitle: string
  seoDescription: string
}

/** One itinerary day. meals: "B,L,D" flags - breakfast, lunch, dinner included. */
export type ItineraryDay = {
  dayNo: number
  title: string
  description: string
  meals: string | null
  accommodation: string | null
}

/** GET /api/v1/packages/{slug}/departures (backend: AvailableDepartureDto). Dates are "yyyy-MM-dd". seatsLeft 0 = sold out. */
export type AvailableDeparture = {
  id: string
  startDate: string
  endDate: string
  adultPrice: number
  childPrice: number
  infantPrice: number
  singleSupplement: number | null
  seatsLeft: number
  lastBookingDate: string
}

type QuoteTravellers = { adults: number; children?: number; infants?: number; singleRooms?: number }

/** What to price: a fixed package needs a departure, a flexible stay needs a start date ("yyyy-MM-dd") and nights. */
export type QuoteParams =
  | (QuoteTravellers & { departureId: string })
  | (QuoteTravellers & { startDate: string; nights: number })

/** GET /api/v1/packages/{slug}/quote (backend: BookingQuoteDto). endDate = last trip day (fixed) or check-out day (flexible). */
export type BookingQuote = {
  packageId: string
  packageTitle: string
  pricingMode: 1 | 2
  departureId: string | null
  startDate: string
  endDate: string
  nights: number
  adults: number
  children: number
  infants: number
  singleRooms: number
  lines: PriceLine[]
  total: number
  currency: string
}

export type PriceLine = { label: string; unitPrice: number; quantity: number; amount: number }

export const catalogApi = {
  async searchPackages(params: PackageSearchParams): Promise<Paged<PackageCard>> {
    const { data } = await http.get<Paged<PackageCard>>('/api/v1/packages', { params })
    return data
  },

  async packageDetails(slug: string): Promise<PackageDetails> {
    const { data } = await http.get<PackageDetails>(`/api/v1/packages/${encodeURIComponent(slug)}`)
    return data
  },

  async departures(slug: string): Promise<AvailableDeparture[]> {
    const { data } = await http.get<AvailableDeparture[]>(`/api/v1/packages/${encodeURIComponent(slug)}/departures`)
    return data
  },

  async quote(slug: string, params: QuoteParams): Promise<BookingQuote> {
    const { data } = await http.get<BookingQuote>(`/api/v1/packages/${encodeURIComponent(slug)}/quote`, { params })
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
  package: (slug: string) => [...catalogKeys.all, 'package', slug] as const,
  departures: (slug: string) => [...catalogKeys.package(slug), 'departures'] as const,
  quote: (slug: string, params: QuoteParams | null) => [...catalogKeys.package(slug), 'quote', params] as const,
  destinations: (featuredOnly: boolean) => [...catalogKeys.all, 'destinations', { featuredOnly }] as const,
  categories: () => [...catalogKeys.all, 'categories'] as const,
}

const fiveMinutes = 5 * 60_000

export const packagesQuery = (params: PackageSearchParams) =>
  queryOptions({ queryKey: catalogKeys.packages(params), queryFn: () => catalogApi.searchPackages(params), staleTime: fiveMinutes })

export const packageDetailsQuery = (slug: string) =>
  queryOptions({ queryKey: catalogKeys.package(slug), queryFn: () => catalogApi.packageDetails(slug), staleTime: fiveMinutes })

/** Seats change with every booking: always refetch when the page opens (staleTime 0, the default). */
export const departuresQuery = (slug: string) =>
  queryOptions({ queryKey: catalogKeys.departures(slug), queryFn: () => catalogApi.departures(slug) })

/**
 * params null = nothing picked yet: skipToken makes the query wait instead of sending a half-filled request.
 * keepPreviousData: while a new price loads, the old one stays on screen instead of flashing empty.
 */
export const quoteQuery = (slug: string, params: QuoteParams | null) =>
  queryOptions({
    queryKey: catalogKeys.quote(slug, params),
    queryFn: params ? () => catalogApi.quote(slug, params) : skipToken,
    staleTime: 60_000,
    placeholderData: keepPreviousData,
  })

export const destinationsQuery = (featuredOnly: boolean) =>
  queryOptions({ queryKey: catalogKeys.destinations(featuredOnly), queryFn: () => catalogApi.destinations(featuredOnly), staleTime: fiveMinutes })

export const categoriesQuery = queryOptions({ queryKey: catalogKeys.categories(), queryFn: catalogApi.categories, staleTime: fiveMinutes })
