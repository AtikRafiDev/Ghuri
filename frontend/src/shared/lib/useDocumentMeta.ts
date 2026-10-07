import { useEffect } from 'react'
import { site } from '@/shared/config/site'

type DocumentMeta = { title: string }

/**
 * The browser-tab title of one page ("Checkout | Ghuri"). The old title
 * comes back when the page closes, so the next page never shows this one's.
 */
export function useDocumentMeta({ title }: DocumentMeta) {
  useEffect(() => {
    const previousTitle = document.title
    document.title = `${title} | ${site.name}`
    return () => {
      document.title = previousTitle
    }
  }, [title])
}
