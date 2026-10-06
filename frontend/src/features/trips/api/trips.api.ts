import { queryOptions } from '@tanstack/react-query'
import { http } from '@/shared/api/http'

// The customer's custom trips (17-day plan, option B, Days 13-14).
// Backend: CustomTripsController. Enums arrive as numbers (Ghuri.Domain/Enums).

export type TripStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7
export const tripStatusLabels: Record<TripStatus, string> = {
  1: 'Waiting for a quote',
  2: 'Quote ready',
  3: 'Accepted',
  4: 'Paid',
  5: 'Not possible',
  6: 'Quote expired',
  7: 'Cancelled',
}

export type HotelLevel = 1 | 2 | 3
export const hotelLevels: { value: HotelLevel; label: string; hint: string }[] = [
  { value: 1, label: 'Budget', hint: 'Clean and simple' },
  { value: 2, label: 'Standard', hint: '3-star comfort' },
  { value: 3, label: 'Premium', hint: '4-5 star' },
]

export type TransferMode = 1 | 2 | 3 | 4 | 5 | 6
export const transferModes: { value: TransferMode; label: string }[] = [
  { value: 2, label: 'Bus' },
  { value: 3, label: 'Train' },
  { value: 4, label: 'Air' },
  { value: 5, label: 'Private car' },
  { value: 6, label: 'Launch' },
  { value: 1, label: 'I’ll arrange it' },
]
export const transferLabel = (mode: TransferMode) => transferModes.find((m) => m.value === mode)?.label ?? ''

export type QuoteLineCategory = 1 | 2 | 3 | 4 | 5 | 6
export const quoteLineLabels: Record<QuoteLineCategory, string> = {
  1: 'Hotel',
  2: 'Transport',
  3: 'Meals',
  4: 'Guide',
  5: 'Activities',
  6: 'Other',
}

/** POST /api/v1/custom-trips (backend: SubmitCustomTripCommand). */
export type SubmitTripRequest = {
  startDate: string
  adults: number
  children: number
  infants: number
  hotelLevel: HotelLevel
  budgetPerPerson: number | null
  notes: string | null
  legs: { destinationId: string; nights: number; transferToNext: TransferMode }[]
}

export type SubmitTripResponse = { tripNo: string; startDate: string; endDate: string; totalNights: number }

/** One line of "My trips" (backend: CustomTripListItemDto). */
export type TripListItem = {
  tripNo: string
  status: TripStatus
  route: string
  startDate: string
  endDate: string
  totalNights: number
  people: number
  submittedAtUtc: string
  quoteTotal: number | null
  quoteExpiresAtUtc: string | null
}

/** GET /api/v1/custom-trips/{tripNo} (backend: CustomTripDto). */
export type Trip = {
  tripNo: string
  status: TripStatus
  startDate: string
  endDate: string
  totalNights: number
  adults: number
  children: number
  infants: number
  hotelLevel: HotelLevel
  budgetPerPerson: number | null
  notes: string | null
  contactName: string
  contactPhone: string
  contactEmail: string | null
  legs: {
    sequence: number
    destinationId: string
    destinationName: string
    nights: number
    transferToNext: TransferMode
    checkInDate: string
    checkOutDate: string
  }[]
  /** Null until staff send a price. */
  quote: {
    version: number
    itinerary: string
    lines: { category: QuoteLineCategory; description: string; amount: number }[]
    total: number
    currency: string
    quotedAtUtc: string
    expiresAtUtc: string
    /** Past its deadline - even before the hourly job marks it Expired. */
    isExpired: boolean
  } | null
  timeline: {
    submittedAtUtc: string
    quotedAtUtc: string | null
    acceptedAtUtc: string | null
    paidAtUtc: string | null
    expiredAtUtc: string | null
    rejectedAtUtc: string | null
    rejectReason: string | null
    cancelledAtUtc: string | null
    cancelReason: string | null
  }
  canCancel: boolean
  /** The booking made on accepting (Day 15): status 1 = waiting for payment, 2 = paid. Null before accepting. */
  booking: { bookingNo: string; status: 1 | 2 | 3 | 4 | 5 | 6; holdExpiresAtUtc: string | null } | null
}

/** POST /api/v1/custom-trips/{tripNo}/accept (backend: AcceptCustomTripQuoteCommand). */
export type AcceptQuoteRequest = {
  travellers: { type: 1 | 2 | 3; fullName: string; isLead: boolean }[]
  specialRequest: string | null
}

export type AcceptQuoteResponse = { bookingNo: string; totalAmount: number; currency: string; holdExpiresAtUtc: string }

const base = '/api/v1/custom-trips'

export const tripsApi = {
  async submit(request: SubmitTripRequest): Promise<SubmitTripResponse> {
    const { data } = await http.post<SubmitTripResponse>(base, request)
    return data
  },

  async list(): Promise<TripListItem[]> {
    const { data } = await http.get<TripListItem[]>(base)
    return data
  },

  async get(tripNo: string): Promise<Trip> {
    const { data } = await http.get<Trip>(`${base}/${encodeURIComponent(tripNo)}`)
    return data
  },

  /** Makes the booking; pay it on /checkout/{bookingNo} like any booking. A second call while it waits returns the same booking. */
  async accept(tripNo: string, request: AcceptQuoteRequest): Promise<AcceptQuoteResponse> {
    const { data } = await http.post<AcceptQuoteResponse>(`${base}/${encodeURIComponent(tripNo)}/accept`, request)
    return data
  },

  async cancel(tripNo: string, reason: string | null): Promise<void> {
    await http.post(`${base}/${encodeURIComponent(tripNo)}/cancel`, { reason })
  },
}

export const tripKeys = {
  all: ['trips'] as const,
  list: () => [...tripKeys.all, 'list'] as const,
  one: (tripNo: string) => [...tripKeys.all, tripNo] as const,
}

export const myTripsQuery = queryOptions({ queryKey: tripKeys.list(), queryFn: tripsApi.list })
export const myTripQuery = (tripNo: string) => queryOptions({ queryKey: tripKeys.one(tripNo), queryFn: () => tripsApi.get(tripNo) })

/** The same limits the API checks (backend: CustomTrip). */
export const tripLimits = { minLeadDays: 3, maxLegs: 10, maxNightsPerLeg: 30, maxTotalNights: 60, maxPeople: 20 } as const
