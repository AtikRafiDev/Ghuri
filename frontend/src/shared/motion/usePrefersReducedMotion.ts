import { useSyncExternalStore } from 'react'

const query = window.matchMedia('(prefers-reduced-motion: reduce)')

/**
 * True when the visitor has asked their device for less motion (Windows:
 * "Animation effects" off; iPhone: Reduce Motion). The page then shows
 * everything in its final place, with no movement. It updates live if the
 * setting changes while the page is open.
 */
export function usePrefersReducedMotion(): boolean {
  return useSyncExternalStore(subscribe, () => query.matches)
}

function subscribe(onChange: () => void) {
  query.addEventListener('change', onChange)
  return () => query.removeEventListener('change', onChange)
}
