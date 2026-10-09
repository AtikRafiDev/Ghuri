import { Badge } from '@/components/ui/badge'
import { paymentStatusLabels, refundStatusLabels, type PaymentStatus, type RefundStatus } from '../api/operations.api'

type Tone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

// 1 started · 2 pending · 3 paid · 4 failed · 5 cancelled · 6 refunded · 7 partly refunded
const paymentTones: Record<PaymentStatus, Tone> = { 1: 'neutral', 2: 'warning', 3: 'success', 4: 'danger', 5: 'neutral', 6: 'info', 7: 'info' }

// 1 to process · 2 approved · 3 rejected · 4 with SSLCommerz · 5 refunded · 6 failed (still owed)
const refundTones: Record<RefundStatus, Tone> = { 1: 'warning', 2: 'info', 3: 'neutral', 4: 'info', 5: 'success', 6: 'danger' }

/** A payment's status pill: "Paid" green, "Failed" terracotta, "Pending" amber... */
export function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
  return (
    <Badge variant={paymentTones[status]} dot>
      {paymentStatusLabels[status]}
    </Badge>
  )
}

/** A refund's status pill: "To process" amber (staff must act), "Refunded" green... */
export function RefundStatusBadge({ status }: { status: RefundStatus }) {
  return (
    <Badge variant={refundTones[status]} dot>
      {refundStatusLabels[status]}
    </Badge>
  )
}
