/**
 * Hides index.html's boot splash once the app can show a real page - that
 * is when BOTH are true:
 * - "router": the first page's code has downloaded (pages are lazy), and
 * - "auth": we know whether the visitor is logged in (one /auth/refresh call).
 * Hiding earlier would flash an empty frame, or a header that changes a moment later.
 *
 * The first visit of a browser session keeps the splash up for a short
 * moment even when everything is instant, so the welcome animation is seen
 * rather than flickering past. Later reloads hide it as soon as the app is ready.
 */
type Gate = 'router' | 'auth'

const firstVisitMinMs = 1500
/** Never trap anyone behind the splash, whatever goes wrong. */
const giveUpAfterMs = 8000
const seenKey = 'ghuri.splash-seen'

const pending = new Set<Gate>(['router', 'auth'])
let hidden = false
/** True from the moment the splash starts fading out (or when there never was one). */
let gone = false
const goneListeners = new Set<() => void>()

function minimumMs(): number {
  try {
    if (sessionStorage.getItem(seenKey)) return 0
    sessionStorage.setItem(seenKey, '1')
  } catch {
    // Storage blocked (private mode, strict settings) - just skip the minimum.
    return 0
  }
  return firstVisitMinMs
}

const shownUntil = performance.now() + minimumMs()

function hide() {
  if (hidden) return
  hidden = true
  const splash = document.getElementById('boot-splash')
  if (!splash) {
    markGone()
    return
  }
  window.setTimeout(
    () => {
      splash.classList.add('is-done')
      markGone()
      // Remove it after the fade so its animations stop costing anything.
      splash.addEventListener('transitionend', () => splash.remove(), { once: true })
    },
    Math.max(0, shownUntil - performance.now()),
  )
}

function markGone() {
  gone = true
  goneListeners.forEach((run) => run())
  goneListeners.clear()
}

/**
 * Runs `run` as the splash starts fading out, or straight away if it already
 * has. The home page waits for this before playing its entrance, so the
 * animation isn't spent behind the splash. Returns an "unsubscribe" for cleanup.
 */
export function onBootSplashGone(run: () => void): () => void {
  if (gone || !document.getElementById('boot-splash')) {
    run()
    return () => {}
  }
  goneListeners.add(run)
  return () => goneListeners.delete(run)
}

/** Tell the splash one part of the start-up is done. */
export function markBootReady(gate: Gate) {
  pending.delete(gate)
  if (pending.size === 0) hide()
}

window.setTimeout(hide, giveUpAfterMs)
