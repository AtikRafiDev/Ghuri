// Dates as the API sends them: "yyyy-MM-dd" text, a calendar day with no
// time and no time zone. Every helper here works in UTC so that the
// computer's own time zone can never move a day.

/**
 * Today in Bangladesh as "yyyy-MM-dd" - the same "today" the API uses
 * (UTC+6, no daylight saving), whatever time zone this computer is set to.
 */
export function todayInBangladesh(): string {
  return new Date(Date.now() + 6 * 60 * 60 * 1000).toISOString().slice(0, 10)
}

/** "yyyy-MM-dd" + days → "yyyy-MM-dd". */
export function addDays(date: string, days: number): string {
  const d = new Date(`${date}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() + days)
  return d.toISOString().slice(0, 10)
}

/** "2026-12-20" → "Sun, 20 Dec 2026". */
export function formatDate(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  })
}
