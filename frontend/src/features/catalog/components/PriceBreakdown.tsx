import { formatTaka } from '@/shared/lib/format'
import type { BookingQuote } from '../api/catalog.api'

/**
 * The quote line by line - "Adult ৳12,000 × 2 = ৳24,000" - then the total. Package page and checkout.
 * Labels on the left, amounts right-aligned with tabular digits, so the amounts form one clean column
 * and the total sits under them.
 */
export function PriceBreakdown({ quote }: { quote: BookingQuote }) {
  return (
    <dl className="grid gap-3 text-sm">
      {quote.lines.map((line) => (
        <div key={line.label} className="flex items-start justify-between gap-4">
          <dt className="grid gap-0.5">
            <span className="font-medium text-ink-900">{line.label}</span>
            <span className="nums text-xs text-ink-500">
              {formatTaka(line.unitPrice)} × {line.quantity}
            </span>
          </dt>
          <dd className="nums text-right font-semibold text-ink-900">{formatTaka(line.amount)}</dd>
        </div>
      ))}
      <div className="flex items-baseline justify-between gap-4 border-t border-dashed border-ink-200 pt-3">
        <dt className="font-semibold text-ink-700">Total</dt>
        <dd className="nums text-right text-2xl leading-none font-bold tracking-tight text-forest-900">{formatTaka(quote.total)}</dd>
      </div>
    </dl>
  )
}
