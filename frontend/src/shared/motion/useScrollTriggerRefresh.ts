import { useEffect, type RefObject } from 'react'
import { ScrollTrigger } from './gsap'

/**
 * ScrollTrigger measures where each animated section starts once, when it's
 * created. Sections grow when their API data or photos arrive, which moves
 * everything below - so whenever this element's height changes, ask it to
 * measure again (batched: once per burst of changes).
 *
 * Put it on the page's outermost element.
 */
export function useScrollTriggerRefresh(ref: RefObject<HTMLElement | null>) {
  useEffect(() => {
    const page = ref.current
    if (!page) return
    let timer = 0
    const observer = new ResizeObserver(() => {
      window.clearTimeout(timer)
      timer = window.setTimeout(() => ScrollTrigger.refresh(), 150)
    })
    observer.observe(page)
    return () => {
      observer.disconnect()
      window.clearTimeout(timer)
    }
  }, [ref])
}
