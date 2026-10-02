import type { QuoteParams } from '../api/catalog.api'

/**
 * A package plus what the customer picked on its page. It travels in the
 * URL - package page → (login) → checkout - so a refresh, the login detour
 * or a shared link never loses it.
 */
export type BookingSelection = { slug: string; params: QuoteParams }

/** → "?package=cox-s-bazar&adults=2&children=0&infants=0&departureId=..." */
export function toSelectionSearch({ slug, params }: BookingSelection): string {
  const search = new URLSearchParams({ package: slug })
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) search.set(key, String(value))
  }
  return `?${search}`
}

/**
 * Back from the URL. null = something is missing or isn't a number (an
 * old or hand-edited link). Only the SHAPE is checked here - the quote API
 * still checks every rule.
 */
export function readSelection(search: URLSearchParams): BookingSelection | null {
  const slug = search.get('package')
  const adults = toCount(search.get('adults'))
  if (!slug || adults === null) return null

  const travellers = {
    adults,
    children: toCount(search.get('children')) ?? 0,
    infants: toCount(search.get('infants')) ?? 0,
  }

  const departureId = search.get('departureId')
  if (departureId) return { slug, params: { ...travellers, departureId } }

  const startDate = search.get('startDate')
  const nights = toCount(search.get('nights'))
  if (startDate && /^\d{4}-\d{2}-\d{2}$/.test(startDate) && nights !== null) {
    return { slug, params: { ...travellers, startDate, nights } }
  }
  return null
}

function toCount(value: string | null): number | null {
  return value !== null && /^\d{1,3}$/.test(value) ? Number(value) : null
}
