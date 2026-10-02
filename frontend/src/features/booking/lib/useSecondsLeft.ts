import { useEffect, useState } from 'react'

/**
 * Seconds until a deadline ("2026-10-02T10:20:00Z"), ticking every second;
 * 0 once it has passed, null without a deadline. The deadline comes from the
 * server - this only counts down to it, it never decides anything.
 */
export function useSecondsLeft(deadlineUtc: string | null): number | null {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (!deadlineUtc) return
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [deadlineUtc])

  if (!deadlineUtc) return null
  return Math.max(0, Math.floor((Date.parse(deadlineUtc) - now) / 1000))
}

/** 754 → "12:34". */
export function formatMinutesSeconds(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}
