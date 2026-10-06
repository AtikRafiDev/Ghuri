import { useEffect, useRef, useState } from 'react'
import { useSearchParams } from 'react-router'

/**
 * Filters of an admin table, kept in the URL (blueprint 13.2): refresh, the
 * back button and a shared link all keep them. Gives the raw URL values,
 * a way to change them (resetting to page 1 unless the page itself changes),
 * and a search box that only updates the URL after 300 ms without typing.
 */
export function useListParams() {
  const [searchParams, setSearchParams] = useSearchParams()

  const update = (changes: Record<string, string | null>) =>
    setSearchParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (!('page' in changes)) next.delete('page') // a new filter starts on page 1
        for (const [key, value] of Object.entries(changes)) {
          if (value) next.set(key, value)
          else next.delete(key)
        }
        return next
      },
      { replace: true }, // filtering shouldn't fill the back-button history
    )

  const [searchText, setSearchText] = useState(searchParams.get('q') ?? '')
  const timer = useRef<number | undefined>(undefined)
  useEffect(() => () => window.clearTimeout(timer.current), [])
  const onSearchChange = (text: string) => {
    setSearchText(text)
    window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => update({ q: text.trim() || null }), 300)
  }

  const clear = () => {
    setSearchText('')
    setSearchParams({}, { replace: true })
  }

  return {
    get: (key: string) => searchParams.get(key),
    page: Math.max(1, Number(searchParams.get('page')) || 1),
    search: searchParams.get('q') ?? '',
    searchText,
    onSearchChange,
    update,
    clear,
  }
}

/** A number from the URL, only if it's one of the allowed values. */
export function oneOf<T extends number>(value: string | null, allowed: readonly T[]): T | null {
  const n = Number(value) as T
  return value !== null && allowed.includes(n) ? n : null
}
