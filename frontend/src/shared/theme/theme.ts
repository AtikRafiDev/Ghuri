import { useSyncExternalStore } from 'react'

/**
 * Light or dark. The choice is remembered in this browser (localStorage);
 * until someone picks, the site follows the device's own setting.
 *
 * public/theme-init.js applies it BEFORE the first paint (no white flash
 * on a dark phone); this module keeps it in sync afterwards and lets the
 * toggle change it. The look itself is all CSS: index.css swaps the
 * palette under the "dark" class on <html>.
 */
export type Theme = 'light' | 'dark'

const storageKey = 'ghuri.theme' // the same key theme-init.js reads
const listeners = new Set<() => void>()
const systemDark = window.matchMedia('(prefers-color-scheme: dark)')

function saved(): Theme | null {
  try {
    const value = localStorage.getItem(storageKey)
    return value === 'light' || value === 'dark' ? value : null
  } catch {
    return null // storage blocked - follow the device
  }
}

function current(): Theme {
  return saved() ?? (systemDark.matches ? 'dark' : 'light')
}

function apply(theme: Theme) {
  const root = document.documentElement
  root.classList.toggle('dark', theme === 'dark')
  // The browser's own bars (Android Chrome, Safari) match the page.
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme === 'dark' ? '#0b1310' : '#17583f')
  listeners.forEach((notify) => notify())
}

/** Pick a theme and remember it. */
export function setTheme(theme: Theme) {
  try {
    localStorage.setItem(storageKey, theme)
  } catch {
    // Not remembered (private mode) - it still applies for this visit.
  }
  // A short-lived class turns transitions off, so every colour swaps at once instead of fading at different speeds.
  document.documentElement.classList.add('theme-switching')
  apply(theme)
  window.setTimeout(() => document.documentElement.classList.remove('theme-switching'), 50)
}

// No choice made yet: follow the device when its setting changes.
systemDark.addEventListener('change', () => {
  if (!saved()) apply(current())
})

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

/** The theme on screen now - re-renders when it changes. */
export function useTheme(): Theme {
  return useSyncExternalStore(subscribe, current)
}
