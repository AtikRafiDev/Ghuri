/**
 * A quote's remaining time in words, for a ticking countdown:
 * 200 000 s → "2 days 7 h", 5 400 s → "1 h 30 min", 300 s → "5 min", 0 → "expired".
 * The seconds come from useSecondsLeft - this only words them.
 */
export function formatTimeLeft(totalSeconds: number): string {
  if (totalSeconds <= 0) return 'expired'
  const days = Math.floor(totalSeconds / 86_400)
  const hours = Math.floor((totalSeconds % 86_400) / 3_600)
  const minutes = Math.floor((totalSeconds % 3_600) / 60)
  if (days > 0) return `${days} day${days === 1 ? '' : 's'} ${hours} h`
  if (hours > 0) return `${hours} h ${minutes} min`
  return `${Math.max(1, minutes)} min`
}
