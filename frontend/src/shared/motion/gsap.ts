import { useGSAP } from '@gsap/react'
import gsap from 'gsap'
import { ScrollTrigger } from 'gsap/ScrollTrigger'

/**
 * GSAP, set up once. Import it from HERE, never straight from 'gsap', so
 * the plugins are always registered before anything uses them.
 *
 * - gsap: the animation engine (tweens and timelines).
 * - ScrollTrigger: ties an animation to the scroll position.
 * - useGSAP: React's way in. Like useLayoutEffect, but every animation and
 *   ScrollTrigger made inside it is undone when the component unmounts, so
 *   nothing keeps running on the next page.
 *
 * Only the pages that import this file download GSAP (~45 KB gzipped).
 * Every page is lazy (app/router.tsx), so the rest of the site never loads it.
 */
gsap.registerPlugin(useGSAP, ScrollTrigger)

export { gsap, ScrollTrigger, useGSAP }
