import { queryOptions } from '@tanstack/react-query'
import { http } from '@/shared/api/http'

// The customer's own bookings and paying for them (backend: BookingsController).
// All need a login.

/** 1 = adult, 2 = child, 3 = infant (backend: TravellerType). */
export type TravellerType = 1 | 2 | 3

/** POST /api/v1/bookings (backend: CreateBookingCommand). The price is never sent - the server works it out. */
export type CreateBookingRequest = {
  packageSlug: string
  departureId?: string
  startDate?: string
  nights?: number
  travellers: { type: TravellerType; fullName: string; isLead: boolean }[]
  contactName: string
  contactPhone: string
  contactEmail: string
  specialRequest: string | null
}

/** 201 from POST /api/v1/bookings. holdExpiresAtUtc = when the seats are released if unpaid. */
export type CreateBookingResponse = {
  bookingId: string
  bookingNo: string
  totalAmount: number
  currency: string
  holdExpiresAtUtc: string
}

/** 1 pending payment · 2 confirmed · 3 partly paid · 4 completed · 5 cancelled · 6 expired (backend: BookingStatus). */
export type BookingStatus = 1 | 2 | 3 | 4 | 5 | 6

/** GET /api/v1/bookings/{bookingNo} (backend: MyBookingDto). Dates "yyyy-MM-dd". */
export type MyBooking = {
  id: string
  bookingNo: string
  /** 1 fixed departure · 2 flexible stay · 3 custom trip. */
  bookingType: 1 | 2 | 3
  status: BookingStatus
  packageTitle: string | null
  packageSlug: string | null
  startDate: string
  endDate: string
  nights: number
  adults: number
  children: number
  infants: number
  adultPrice: number
  childPrice: number
  infantPrice: number
  totalAmount: number
  paidAmount: number
  currency: string
  /** Only while status is 1 (pending payment) - the countdown's deadline. */
  holdExpiresAtUtc: string | null
  contactName: string
  contactPhone: string
  contactEmail: string | null
  specialRequest: string | null
  travellers: { fullName: string; type: TravellerType; isLead: boolean }[]
}

/** POST /api/v1/bookings/{bookingNo}/payments: where to send the browser. */
export type StartPaymentResponse = { paymentNo: string; paymentPageUrl: string }

export const bookingsApi = {
  /**
   * The Idempotency-Key makes a double-click or a retry safe: the same key
   * returns the SAME booking instead of booking twice.
   */
  async create(request: CreateBookingRequest, idempotencyKey: string): Promise<CreateBookingResponse> {
    const { data } = await http.post<CreateBookingResponse>('/api/v1/bookings', request, {
      headers: { 'Idempotency-Key': idempotencyKey },
    })
    return data
  },

  async mine(bookingNo: string): Promise<MyBooking> {
    const { data } = await http.get<MyBooking>(`/api/v1/bookings/${encodeURIComponent(bookingNo)}`)
    return data
  },

  async startPayment(bookingNo: string): Promise<StartPaymentResponse> {
    const { data } = await http.post<StartPaymentResponse>(`/api/v1/bookings/${encodeURIComponent(bookingNo)}/payments`)
    return data
  },
}

export const bookingKeys = {
  all: ['bookings'] as const,
  mine: (bookingNo: string) => [...bookingKeys.all, bookingNo] as const,
}

/** Always fresh: a booking's status changes (paid, expired) while the page is open. */
export const myBookingQuery = (bookingNo: string) =>
  queryOptions({ queryKey: bookingKeys.mine(bookingNo), queryFn: () => bookingsApi.mine(bookingNo) })
