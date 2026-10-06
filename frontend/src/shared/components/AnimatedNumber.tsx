import { useEffect, useRef, useState } from 'react'

const prefersReducedMotion = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches

/**
 * A number that counts up to its value (and glides to a new one when it
 * changes) - for dashboard figures. format turns it into text: taka, %, etc.
 * With "reduce motion" on, it simply shows the value.
 */
export function AnimatedNumber({ value, format = (n) => Math.round(n).toLocaleString('en-IN'), duration = 1000 }: { value: number; format?: (n: number) => string; duration?: number }) {
  const [shown, setShown] = useState(() => (prefersReducedMotion() ? value : 0))
  const from = useRef(shown)

  useEffect(() => {
    if (prefersReducedMotion()) {
      from.current = value
      // A plain jump - scheduled, like every other update here, so the effect never sets state synchronously.
      const frame = requestAnimationFrame(() => setShown(value))
      return () => cancelAnimationFrame(frame)
    }
    const start = performance.now()
    const startValue = from.current
    let frame = 0
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / duration)
      const eased = 1 - Math.pow(1 - t, 4) // easeOutQuart: fast, then settles
      const current = startValue + (value - startValue) * eased
      from.current = current
      setShown(current)
      if (t < 1) frame = requestAnimationFrame(tick)
    }
    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [value, duration])

  return <span aria-label={format(value)}>{format(shown)}</span>
}
