// What the checkout remembers for THIS browser tab (sessionStorage): it
// survives a refresh and the trip to the payment page and back, but not
// closing the tab. Every access is wrapped: storage can be blocked
// (private mode, strict settings) - then the page still works, just
// without the memory.

const keyPrefix = 'ghuri.checkout.key.'
const lastPaymentKey = 'ghuri.checkout.lastPayment'

/**
 * The Idempotency-Key for booking this exact trip choice. Created once and
 * reused until the booking succeeds - so a double-click, or a refresh while
 * the request is still on its way, returns the SAME booking instead of a
 * second one.
 */
export function idempotencyKeyFor(selection: string): string {
  const storageKey = keyPrefix + selection
  const existing = read(storageKey)
  if (existing) return existing

  const key = crypto.randomUUID()
  write(storageKey, key)
  return key
}

/** The booking exists now (its number is in the URL) - a later checkout of the same trip is a NEW booking. */
export function forgetIdempotencyKey(selection: string) {
  try {
    sessionStorage.removeItem(keyPrefix + selection)
  } catch {
    // storage blocked - nothing to forget
  }
}

/**
 * Which booking a payment attempt was for. SSLCommerz sends the customer
 * back with only OUR payment number, so the result page looks the booking
 * up here (e.g. for "Try again").
 */
export function rememberPaymentAttempt(paymentNo: string, bookingNo: string) {
  write(lastPaymentKey, JSON.stringify({ paymentNo, bookingNo }))
}

export function bookingForPayment(paymentNo: string): string | null {
  try {
    const saved = JSON.parse(read(lastPaymentKey) ?? 'null') as { paymentNo: string; bookingNo: string } | null
    return saved?.paymentNo === paymentNo ? saved.bookingNo : null
  } catch {
    return null
  }
}

function read(key: string): string | null {
  try {
    return sessionStorage.getItem(key)
  } catch {
    return null
  }
}

function write(key: string, value: string) {
  try {
    sessionStorage.setItem(key, value)
  } catch {
    // storage blocked - the page works without it
  }
}
