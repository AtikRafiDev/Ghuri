// Runs before the page is drawn (a plain <script> in index.html's <head>):
// a dark theme must be on from the very first frame, or a dark-mode phone
// flashes white. A file rather than inline code, so a strict
// Content-Security-Policy (script-src 'self') can allow it.
// Keep in step with src/shared/theme/theme.ts (same storage key, same rule).
;(function () {
  var theme = null
  try {
    theme = localStorage.getItem('ghuri.theme')
  } catch (e) {
    // storage blocked - follow the device
  }
  var dark = theme === 'dark' || (theme !== 'light' && window.matchMedia('(prefers-color-scheme: dark)').matches)
  if (dark) {
    document.documentElement.classList.add('dark')
    document.querySelector('meta[name="theme-color"]').setAttribute('content', '#0b1310')
  }
})()
