import { queryOptions } from '@tanstack/react-query'
import type { BookingStatus } from '@/features/booking/api/bookings.api'
import { downloadBlob } from '@/shared/api/download'
import { http } from '@/shared/api/http'
import type { Paged } from '@/shared/api/paged'

// The admin's daily operations (17-day plan, Day 12): bookings, payments,
// refunds, dashboard. Backend: AdminBookingsController, AdminPaymentsController,
// AdminDashboardController. Enums arrive as numbers (backend: Ghuri.Domain/Enums).

export const bookingStatusLabels: Record<BookingStatus, string> = {
  1: 'Waiting for payment',
  2: 'Confirmed',
  3: 'Partly paid',
  4: 'Completed',
  5: 'Cancelled',
  6: 'Expired',
}

export type BookingType = 1 | 2 | 3
export const bookingTypeLabels: Record<BookingType, string> = { 1: 'Fixed departure', 2: 'Flexible stay', 3: 'Custom trip' }

export type PaymentStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7
export const paymentStatusLabels: Record<PaymentStatus, string> = {
  1: 'Started',
  2: 'Pending',
  3: 'Paid',
  4: 'Failed',
  5: 'Cancelled',
  6: 'Refunded',
  7: 'Partly refunded',
}

export type PaymentProvider = 1 | 2 | 3
export const paymentProviderLabels: Record<PaymentProvider, string> = { 1: 'SSLCommerz', 2: 'Stripe', 3: 'Manual' }

export type RefundStatus = 1 | 2 | 3 | 4 | 5 | 6
export const refundStatusLabels: Record<RefundStatus, string> = {
  1: 'To process',
  2: 'Approved',
  3: 'Rejected',
  4: 'Processing',
  5: 'Refunded',
  6: 'Failed',
}

/** Backend: ManualPaymentMethod. */
export const manualPaymentMethods = [
  { value: 1, label: 'Cash' },
  { value: 2, label: 'Bank transfer' },
  { value: 3, label: 'bKash' },
  { value: 4, label: 'Nagad' },
  { value: 5, label: 'Rocket' },
  { value: 6, label: 'Card (at the office)' },
  { value: 7, label: 'Other' },
] as const

// ---------- Bookings ----------

export type BookingListParams = {
  search: string
  status: BookingStatus | null
  type: BookingType | null
  tripFrom: string | null
  tripTo: string | null
  page: number
}

export type AdminBookingListItem = {
  bookingNo: string
  bookingType: BookingType
  status: BookingStatus
  packageTitle: string | null
  contactName: string
  contactPhone: string
  startDate: string
  endDate: string
  travellers: number
  totalAmount: number
  paidAmount: number
  currency: string
  bookedAtUtc: string
}

/** GET admin/bookings/{bookingNo} (backend: AdminBookingDto). */
export type AdminBooking = {
  bookingNo: string
  bookingType: BookingType
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
  amountDue: number
  currency: string
  holdExpiresAtUtc: string | null
  bookedAtUtc: string
  customer: { id: string; fullName: string; phone: string; email: string | null }
  contactName: string
  contactPhone: string
  contactEmail: string | null
  specialRequest: string | null
  cancelReason: string | null
  cancelledAtUtc: string | null
  canCancel: boolean
  canRecordPayment: boolean
  travellers: { fullName: string; type: 1 | 2 | 3; isLead: boolean; phone: string | null }[]
  history: { from: BookingStatus | null; to: BookingStatus; changedAtUtc: string; changedByName: string | null; note: string | null }[]
  payments: {
    paymentNo: string
    provider: PaymentProvider
    method: string | null
    status: PaymentStatus
    amount: number
    initiatedAtUtc: string
    paidAtUtc: string | null
    reference: string | null
    failureReason: string | null
  }[]
  refunds: {
    refundNo: string
    paymentNo: string
    amount: number
    refundPercent: number
    status: RefundStatus
    reason: string
    requestedByName: string | null
    requestedAtUtc: string
    reference: string | null
    completedAtUtc: string | null
    rejectReason: string | null
  }[]
}

// ---------- Payments & refunds ----------

export type PaymentListParams = {
  search: string
  status: PaymentStatus | null
  provider: PaymentProvider | null
  page: number
}

export type AdminPaymentListItem = {
  paymentNo: string
  bookingNo: string
  provider: PaymentProvider
  method: string | null
  status: PaymentStatus
  amount: number
  currency: string
  initiatedAtUtc: string
  paidAtUtc: string | null
  reference: string | null
  failureReason: string | null
}

export type RefundListParams = { open: boolean; search: string; page: number }

export type AdminRefundListItem = {
  refundNo: string
  bookingNo: string
  paymentNo: string
  /** How the customer paid - send the money back the same way. */
  paymentMethod: string | null
  contactName: string
  contactPhone: string
  amount: number
  refundPercent: number
  currency: string
  reason: string
  status: RefundStatus
  /** null = requested by the system (a late or double payment). */
  requestedByName: string | null
  requestedAtUtc: string
  reference: string | null
  completedAtUtc: string | null
  rejectReason: string | null
}

// ---------- Dashboard ----------

export type AdminDashboard = {
  today: string
  bookingsToday: number
  revenueToday: number
  revenueThisMonth: number
  currency: string
  pendingPayments: number
  refundsToProcess: number
  refundsToProcessAmount: number
  upcomingTripCount: number
  upcomingTrips: { bookingNo: string; packageTitle: string | null; contactName: string; contactPhone: string; startDate: string; travellers: number }[]
  /** Custom trip requests nobody has priced yet. */
  customTripsToQuote: number
  /** One row per Bangladesh day, oldest first, today last - zeros included. */
  last14Days: { date: string; bookings: number; revenue: number }[]
  /** Bookings made in the last 30 days, by what became of them. */
  statusMix: { status: BookingStatus; count: number }[]
  /** Best sellers of the last 30 days (paid or part-paid bookings). */
  topPackages: { title: string; slug: string; bookings: number; amount: number }[]
}

const base = '/api/v1/admin'
const enc = encodeURIComponent

/** Only the filters that are set - empty ones stay out of the URL. */
function query(params: Record<string, string | number | boolean | null>) {
  return Object.fromEntries(Object.entries(params).filter(([, v]) => v !== null && v !== ''))
}

export const operationsApi = {
  async bookings(p: BookingListParams): Promise<Paged<AdminBookingListItem>> {
    const { data } = await http.get<Paged<AdminBookingListItem>>(`${base}/bookings`, {
      params: query({ search: p.search, status: p.status, type: p.type, tripFrom: p.tripFrom, tripTo: p.tripTo, page: p.page }),
    })
    return data
  },

  async booking(bookingNo: string): Promise<AdminBooking> {
    const { data } = await http.get<AdminBooking>(`${base}/bookings/${enc(bookingNo)}`)
    return data
  },

  async cancel(bookingNo: string, reason: string): Promise<{ bookingNo: string; refundAmount: number; refundNo: string | null }> {
    const { data } = await http.post(`${base}/bookings/${enc(bookingNo)}/cancel`, { reason })
    return data
  },

  async recordPayment(bookingNo: string, body: { amount: number; method: number; reference: string | null }) {
    const { data } = await http.post<{ paymentNo: string; bookingNo: string; bookingStatus: BookingStatus }>(
      `${base}/bookings/${enc(bookingNo)}/payments`,
      body,
    )
    return data
  },

  document(bookingNo: string, kind: 'invoice' | 'voucher'): Promise<Blob> {
    return downloadBlob(`${base}/bookings/${enc(bookingNo)}/${kind}`)
  },

  async payments(p: PaymentListParams): Promise<Paged<AdminPaymentListItem>> {
    const { data } = await http.get<Paged<AdminPaymentListItem>>(`${base}/payments`, {
      params: query({ search: p.search, status: p.status, provider: p.provider, page: p.page }),
    })
    return data
  },

  async refunds(p: RefundListParams): Promise<Paged<AdminRefundListItem>> {
    const { data } = await http.get<Paged<AdminRefundListItem>>(`${base}/refunds`, {
      params: query({ open: p.open, search: p.search, page: p.page }),
    })
    return data
  },

  async completeRefund(refundNo: string, reference: string): Promise<void> {
    await http.post(`${base}/refunds/${enc(refundNo)}/complete`, { reference })
  },

  async rejectRefund(refundNo: string, reason: string): Promise<void> {
    await http.post(`${base}/refunds/${enc(refundNo)}/reject`, { reason })
  },

  async dashboard(): Promise<AdminDashboard> {
    const { data } = await http.get<AdminDashboard>(`${base}/dashboard`)
    return data
  },
}

export const operationsKeys = {
  all: ['admin-operations'] as const,
  bookings: (p: BookingListParams) => [...operationsKeys.all, 'bookings', p] as const,
  booking: (bookingNo: string) => [...operationsKeys.all, 'booking', bookingNo] as const,
  payments: (p: PaymentListParams) => [...operationsKeys.all, 'payments', p] as const,
  refunds: (p: RefundListParams) => [...operationsKeys.all, 'refunds', p] as const,
  dashboard: () => [...operationsKeys.all, 'dashboard'] as const,
}

export const dashboardQuery = queryOptions({ queryKey: operationsKeys.dashboard(), queryFn: operationsApi.dashboard })

/** The roles that may move money / cancel - the same as the API's ManageMoney and CancelBookings policies. */
export const moneyRoles = ['SuperAdmin', 'Manager', 'Accounts'] as const
export const cancelRoles = ['SuperAdmin', 'Manager'] as const
