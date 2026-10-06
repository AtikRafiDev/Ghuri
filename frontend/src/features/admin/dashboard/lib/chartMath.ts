// Small maths for the dashboard's hand-drawn SVG charts.

/** A round top for an axis: 87,300 → 100,000; 4.2 → 5. Always above 0 so an empty chart still has a scale. */
export function niceMax(value: number): number {
  if (value <= 0) return 1
  const power = 10 ** Math.floor(Math.log10(value))
  const step = [1, 2, 2.5, 5, 10].find((m) => m * power >= value)!
  return step * power
}

/**
 * Round axis ticks: a "nice" step (1, 2, 2.5 or 5 × a power of ten) giving
 * at most 4 intervals, and a top that is a whole number of steps.
 * 25,000 → step 10,000, ticks 0 / 10k / 20k / 30k (never 6.3k / 13k / 19k).
 */
export function niceTicks(max: number): number[] {
  const top = max > 0 ? max : 1
  const rough = top / 4
  const power = 10 ** Math.floor(Math.log10(rough))
  const step = [1, 2, 2.5, 5, 10].map((m) => m * power).find((s) => s >= rough)!
  const count = Math.ceil(top / step)
  return Array.from({ length: count + 1 }, (_, i) => i * step)
}

/** Axis-sized taka: ৳850 · ৳45k · ৳1.2L (lakh = 1,00,000 - how Bangladesh counts). */
export function compactTaka(amount: number): string {
  const sign = amount < 0 ? '-' : ''
  const a = Math.abs(amount)
  if (a >= 100_000) return `${sign}৳${trim(a / 100_000)}L`
  if (a >= 1_000) return `${sign}৳${trim(a / 1_000)}k`
  return `${sign}৳${Math.round(a)}`
}

function trim(n: number): string {
  return n >= 10 ? Math.round(n).toString() : n.toFixed(1).replace(/\.0$/, '')
}

/**
 * Change from `before` to `now` as a whole percent. 0 → 0 is "no change" (0);
 * 0 → something can't be a percent, so it's null ("new" - up from nothing).
 */
export function percentChange(now: number, before: number): number | null {
  if (before === 0) return now === 0 ? 0 : null
  return Math.round(((now - before) / Math.abs(before)) * 100)
}

/**
 * A smooth line through the points that never overshoots (monotone cubic,
 * Fritsch–Carlson) - so a dip to ৳0 doesn't curve below the axis.
 */
export function monotonePath(points: { x: number; y: number }[]): string {
  const n = points.length
  if (n === 0) return ''
  if (n === 1) return `M${points[0].x},${points[0].y}`

  const dx = points.slice(1).map((p, i) => p.x - points[i].x)
  const slope = points.slice(1).map((p, i) => (p.y - points[i].y) / dx[i])
  const tangent = points.map((_, i) => {
    if (i === 0) return slope[0]
    if (i === n - 1) return slope[n - 2]
    if (slope[i - 1] * slope[i] <= 0) return 0
    return (3 * (dx[i - 1] + dx[i])) / ((2 * dx[i] + dx[i - 1]) / slope[i - 1] + (dx[i] + 2 * dx[i - 1]) / slope[i])
  })

  let d = `M${points[0].x},${points[0].y}`
  for (let i = 0; i < n - 1; i++) {
    const p = points[i]
    const q = points[i + 1]
    const h = dx[i] / 3
    d += `C${p.x + h},${p.y + tangent[i] * h} ${q.x - h},${q.y - tangent[i + 1] * h} ${q.x},${q.y}`
  }
  return d
}

/** "2026-10-06" → "Tue" / "6 Oct" - the dates are calendar days, so read them in UTC. */
export function weekday(date: string, style: 'narrow' | 'short' = 'short'): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', { weekday: style, timeZone: 'UTC' })
}

export function dayMonth(date: string): string {
  return new Date(`${date}T00:00:00Z`).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', timeZone: 'UTC' })
}

/** Midnight in Dhaka (UTC+6) at the start of a "yyyy-MM-dd" day, as epoch ms. */
export function dhakaMidnight(date: string): number {
  return Date.parse(`${date}T00:00:00Z`) - 6 * 60 * 60 * 1000
}
