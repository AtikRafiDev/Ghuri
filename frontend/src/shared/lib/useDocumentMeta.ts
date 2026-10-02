import { useEffect } from 'react'
import { site } from '@/shared/config/site'

type DocumentMeta = { title: string; description?: string; image?: string | null }

/**
 * The browser-tab title and the description / share-preview meta tags of
 * one page. The old values come back when the page closes, so the next page
 * never shows this one's title.
 *
 * Google runs JavaScript, so it reads these. Facebook and WhatsApp link
 * previews DON'T - they only see index.html. Per-package previews there
 * need the server to write the tags (prerendering, Phase 2).
 */
export function useDocumentMeta({ title, description, image }: DocumentMeta) {
  useEffect(() => {
    const previousTitle = document.title
    document.title = `${title} | ${site.name}`
    const undo = [
      setMeta('name', 'description', description),
      setMeta('property', 'og:title', title),
      setMeta('property', 'og:description', description),
      // Share previews need a full address, but the API may send "/uploads/...".
      setMeta('property', 'og:image', image ? new URL(image, window.location.origin).href : undefined),
    ]
    return () => {
      document.title = previousTitle
      undo.forEach((restore) => restore())
    }
  }, [title, description, image])
}

/** Sets one <meta> tag (creating it if needed) and returns how to put it back. */
function setMeta(attribute: 'name' | 'property', key: string, content: string | undefined): () => void {
  if (!content) return () => {}

  const existing = document.head.querySelector<HTMLMetaElement>(`meta[${attribute}="${key}"]`)
  const meta = existing ?? document.createElement('meta')
  const previous = meta.content
  if (!existing) {
    meta.setAttribute(attribute, key)
    document.head.appendChild(meta)
  }
  meta.content = content

  return () => {
    if (existing) meta.content = previous
    else meta.remove()
  }
}
