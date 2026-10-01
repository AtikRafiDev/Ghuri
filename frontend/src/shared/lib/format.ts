// Formatting shared by the public site and the admin, so a price or a
// duration looks the same everywhere.

/** "৳8,000" - Bangladeshi digit grouping (en-IN groups thousands the same way). */
export function formatTaka(amount: number): string {
  return `৳${amount.toLocaleString('en-IN', { maximumFractionDigits: 2 })}`
}

/** What describeDuration needs - both the admin list rows and the public cards have it. pricingMode 2 = flexible stay. */
export type DurationFields = {
  pricingMode: number
  durationDays: number
  durationNights: number
  minNights: number | null
  maxNights: number | null
}

/** "3 days / 2 nights" for a fixed package, "2–7 nights" for a flexible stay. */
export function describeDuration(p: DurationFields): string {
  if (p.pricingMode === 2 && p.minNights !== null && p.maxNights !== null) {
    return p.minNights === p.maxNights ? `${p.minNights} nights` : `${p.minNights}–${p.maxNights} nights`
  }
  return `${p.durationDays} days / ${p.durationNights} nights`
}
