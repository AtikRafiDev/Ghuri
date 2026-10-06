import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// React Router v8: the DOM-specific RouterProvider comes from "react-router/dom".
import { RouterProvider } from 'react-router/dom'
import { Providers } from './app/providers'
import { router } from './app/router'
import { markBootReady } from './shared/loader/bootSplash'
import './index.css'

// The boot splash's other "ready" signal: the first page's (lazy) code has arrived.
if (router.state.initialized) {
  markBootReady('router')
} else {
  const unsubscribe = router.subscribe((state) => {
    if (state.initialized) {
      unsubscribe()
      markBootReady('router')
    }
  })
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Providers>
      <RouterProvider router={router} />
    </Providers>
  </StrictMode>,
)
