import type { QuoteLineCategory, Trip, TripListItem, TripStatus } from '@/features/trips/api/trips.api'
import { http } from '@/shared/api/http'
import type { Paged } from '@/shared/api/paged'

// Staff work on custom trip requests (17-day plan, Days 13 + 15).
// Backend: AdminCustomTripsController.

export type TripQueueParams = { status: TripStatus | null; search: string; page: number }

/** GET admin/custom-trips/{tripNo} (backend: AdminCustomTripDto). */
export type AdminTrip = {
  trip: Trip
  customer: { id: string; fullName: string; phone: string; email: string | null }
  quotedByName: string | null
  /** Confirmed or completed bookings this customer made before - a returning customer? */
  previousBookings: number
}

export type QuoteLineInput = { category: QuoteLineCategory; description: string; amount: number }

/** POST admin/custom-trips/{tripNo}/quote. validDays null = the default (3). */
export type QuoteRequest = { itinerary: string; lines: QuoteLineInput[]; validDays: number | null }

const base = '/api/v1/admin/custom-trips'

export const adminTripsApi = {
  async queue(p: TripQueueParams): Promise<Paged<TripListItem & { contactName: string; contactPhone: string }>> {
    const params: Record<string, string | number> = { page: p.page }
    if (p.status) params.status = p.status
    if (p.search) params.search = p.search
    const { data } = await http.get(base, { params })
    return data
  },

  async get(tripNo: string): Promise<AdminTrip> {
    const { data } = await http.get<AdminTrip>(`${base}/${encodeURIComponent(tripNo)}`)
    return data
  },

  async quote(tripNo: string, request: QuoteRequest): Promise<{ tripNo: string; quoteVersion: number; total: number; expiresAtUtc: string }> {
    const { data } = await http.post(`${base}/${encodeURIComponent(tripNo)}/quote`, request)
    return data
  },

  async reject(tripNo: string, reason: string): Promise<void> {
    await http.post(`${base}/${encodeURIComponent(tripNo)}/reject`, { reason })
  },
}

export const adminTripKeys = {
  all: ['admin-trips'] as const,
  queue: (p: TripQueueParams) => [...adminTripKeys.all, 'queue', p] as const,
  one: (tripNo: string) => [...adminTripKeys.all, tripNo] as const,
}

/** The roles that may quote or reject - the same as the API's QuoteTrips policy. */
export const quoteRoles = ['SuperAdmin', 'Manager', 'Sales'] as const
