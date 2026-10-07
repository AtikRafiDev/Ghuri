/**
 * A faint world map behind every page of the website and the customer's
 * account: coastlines, country borders and dashed routes from Dhaka to a few
 * of the places Ghuri sells (src/shared/assets/world-map.svg, made by
 * scripts/make-world-map.mjs from Natural Earth's public-domain map).
 *
 * A fixed layer behind everything (-z-10): it stays put while the page
 * scrolls, and shows only where the page background does - between the
 * cards, never over the photo heroes or the footer. The colour is the
 * theme's ink at very low strength, so it turns light in dark mode by itself.
 */
export function WorldMapBackdrop() {
  return <div aria-hidden className="bg-world-map pointer-events-none fixed inset-0 -z-10 bg-ink-900 opacity-[0.07] print:hidden" />
}
