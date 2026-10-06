import { downloadBlob } from '@/shared/api/download'
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
  /** Can it be cancelled NOW, and what would come back (shown before the customer decides). */
  cancellation: CancellationQuote
  /** The newest refund, once one exists. */
  refund: { refundNo: string; amount: number; status: RefundStatus } | null
  cancelledAtUtc: string | null
}

/** Backend: CancellationQuote. canCancel false → reason says why. */
export type CancellationQuote = {
  canCancel: boolean
  daysBeforeStart: number
  refundPercent: number
  refundAmount: number
  reason: string | null
}

/** 1 requested · 2 approved · 3 rejected · 4 processing · 5 completed · 6 failed (backend: RefundStatus). */
export type RefundStatus = 1 | 2 | 3 | 4 | 5 | 6

/** One line of "My bookings" (backend: MyBookingSummaryDto). */
export type MyBookingSummary = {
  bookingNo: string
  bookingType: 1 | 2 | 3
  status: BookingStatus
  packageTitle: string | null
  packageSlug: string | null
  startDate: string
  endDate: string
  nights: number
  travellers: number
  totalAmount: number
  currency: string
  holdExpiresAtUtc: string | null
}

/** 200 from POST /api/v1/bookings/{bookingNo}/cancel. refundNo null = nothing refunded. */
export type CancelBookingResponse = { bookingNo: string; refundPercent: number; refundAmount: number; refundNo: string | null }

/** POST /api/v1/bookings/{bookingNo}/payments: where to send the browser. */
export type StartPaymentResponse = { paymentNo: string; paymentPageUrl: string }

/** 1 initiated · 2 pending · 3 succeeded · 4 failed · 5 cancelled · 6 refunded · 7 partly refunded (backend: PaymentStatus). */
export type PaymentStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7

/**
 * GET /api/v1/payments/{paymentNo} (backend: PaymentResultDto). status 2 =
 * still waiting for SSLCommerz's confirmation. bookingStatus says what the
 * money did: 2 confirmed, or still 6 expired / 5 cancelled if it came too late.
 */
export type PaymentResult = {
  paymentNo: string
  status: PaymentStatus
  amount: number
  currency: string
  bookingNo: string
  bookingStatus: BookingStatus
}

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

  async list(): Promise<MyBookingSummary[]> {
    const { data } = await http.get<MyBookingSummary[]>('/api/v1/bookings')
    return data
  },

  async cancel(bookingNo: string, reason: string | null): Promise<CancelBookingResponse> {
    const { data } = await http.post<CancelBookingResponse>(`/api/v1/bookings/${encodeURIComponent(bookingNo)}/cancel`, { reason })
    return data
  },

  /**
   * The invoice or e-voucher PDF. Fetched through the API client (not a
   * plain link) because it needs the login token; the caller saves the Blob.
   */
  document(bookingNo: string, kind: 'invoice' | 'voucher'): Promise<Blob> {
    return downloadBlob(`/api/v1/bookings/${encodeURIComponent(bookingNo)}/${kind}`)
  },

  async mine(bookingNo: string): Promise<MyBooking> {
    const { data } = await http.get<MyBooking>(`/api/v1/bookings/${encodeURIComponent(bookingNo)}`)
    return data
  },

  async startPayment(bookingNo: string): Promise<StartPaymentResponse> {
    const { data } = await http.post<StartPaymentResponse>(`/api/v1/bookings/${encodeURIComponent(bookingNo)}/payments`)
    return data
  },

  async paymentResult(paymentNo: string): Promise<PaymentResult> {
    const { data } = await http.get<PaymentResult>(`/api/v1/payments/${encodeURIComponent(paymentNo)}`)
    return data
  },
}

export const bookingKeys = {
  all: ['bookings'] as const,
  list: () => [...bookingKeys.all, 'list'] as const,
  mine: (bookingNo: string) => [...bookingKeys.all, bookingNo] as const,
  payment: (paymentNo: string) => [...bookingKeys.all, 'payment', paymentNo] as const,
}

export const myBookingsQuery = queryOptions({ queryKey: bookingKeys.list(), queryFn: bookingsApi.list })

/** Always fresh: a booking's status changes (paid, expired) while the page is open. */
export const myBookingQuery = (bookingNo: string) =>
  queryOptions({ queryKey: bookingKeys.mine(bookingNo), queryFn: () => bookingsApi.mine(bookingNo) })
