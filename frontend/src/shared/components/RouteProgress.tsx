import { useNavigation } from 'react-router'

/**
 * The slim bar along the top while the next page's code downloads (every
 * page is lazy-loaded). Pure CSS (index.css, .route-progress): it rushes
 * forward, crawls while waiting, then completes and fades once the page is in.
 */
export function RouteProgress() {
  const busy = useNavigation().state !== 'idle'
  return <div aria-hidden className="route-progress" data-busy={busy} />
}
