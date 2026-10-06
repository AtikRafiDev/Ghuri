import { useCallback, useState } from 'react'

/**
 * The live pixel width of an element - so an SVG chart can draw at its real
 * size (crisp 2px lines, readable labels) instead of being stretched.
 * Use: const [ref, width] = useElementWidth(); <div ref={ref}>…
 */
export function useElementWidth<T extends HTMLElement>(): [(node: T | null) => void, number] {
  const [width, setWidth] = useState(0)
  // A callback ref: runs when the element appears, and its returned function when it goes away (React 19).
  const ref = useCallback((node: T | null) => {
    if (!node) return
    const observer = new ResizeObserver(([entry]) => setWidth(Math.round(entry.contentRect.width)))
    observer.observe(node)
    return () => observer.disconnect()
  }, [])
  return [ref, width]
}
