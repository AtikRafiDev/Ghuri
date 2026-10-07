import { useMatches } from 'react-router'

/**
 * Extra settings a public route can carry in its `handle` (app/router.tsx).
 *
 * fullBleed: the page draws edge to edge (no centred column, no top
 * padding) and slides up UNDER the header, so a photo hero can fill the
 * top of the screen. The header then turns see-through over it until the
 * page scrolls. The home page uses this, and so does every page that opens
 * with a PageHero (shared/components/PageHero) or the photo sign-in frame -
 * the header's light text needs their dark top behind it.
 */
export type PublicRouteHandle = { fullBleed?: boolean }

/** True when the page on screen asked for the full-bleed layout. */
export function useFullBleed(): boolean {
  return useMatches().some((match) => (match.handle as PublicRouteHandle | undefined)?.fullBleed === true)
}
