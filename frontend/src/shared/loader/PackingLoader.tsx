import type { CSSProperties } from 'react'
import { cn } from '@/lib/utils'
// The same markup the boot splash uses (vite.config.ts inlines it into
// index.html); its CSS comes in through index.css. "?raw" = the file's text.
import sceneHtml from './packingScene.html?raw'

/**
 * The "packing your bags" animation for anything that loads a whole page
 * or panel. It fades in after a short delay, so a fast answer never
 * flashes it; reduce-motion users see one still, finished frame.
 *
 * dangerouslySetInnerHTML is safe here: the HTML is our own file, bundled
 * at build time - nothing from a user or the API ever reaches it.
 */
export function PackingLoader({ label = 'Loading', size = 168, className }: { label?: string; size?: number; className?: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className={cn('animate-[fade-in_0.4s_ease-out_0.2s_backwards]', className)}
      style={{ '--pk-size': `${size}px` } as CSSProperties}
      dangerouslySetInnerHTML={{ __html: sceneHtml }}
    />
  )
}
