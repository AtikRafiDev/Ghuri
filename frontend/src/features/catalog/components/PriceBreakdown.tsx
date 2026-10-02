import { Separator } from '@/components/ui/separator'
import { formatTaka } from '@/shared/lib/format'
import type { BookingQuote } from '../api/catalog.api'

/** The quote line by line - "Adult ৳12,000 × 2 = ৳24,000" - then the total. Package page and checkout. */
export function PriceBreakdown({ quote }: { quote: BookingQuote }) {
  return (
    <dl className="grid gap-1.5 text-sm">
      {quote.lines.map((line) => (
        <div key={line.label} className="flex items-start justify-between gap-4">
          <dt className="grid">
            <span>{line.label}</span>
            <span className="text-xs text-muted-foreground">
              {formatTaka(line.unitPrice)} × {line.quantity}
            </span>
          </dt>
          <dd className="tabular-nums">{formatTaka(line.amount)}</dd>
        </div>
      ))}
      <Separator className="my-1" />
      <div className="flex items-center justify-between gap-4 text-base font-semibold">
        <dt>Total</dt>
        <dd className="tabular-nums">{formatTaka(quote.total)}</dd>
      </div>
    </dl>
  )
}
