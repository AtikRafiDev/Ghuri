// The URL name ("slug") rules - the same as the API's Slug.GenerateFrom and
// CatalogSlug.ValidSlugFor, so the form can show the final URL while typing
// and catch a bad one before saving. The API still re-checks everything.

/** "Cox's Bazar!" -> "cox-s-bazar": lowercase; letters/digits kept; every other run of characters -> one hyphen. */
export function slugify(text: string): string {
  let result = ''
  let lastWasHyphen = false
  for (const char of text.trim().toLowerCase()) {
    if (/[\p{L}\p{Nd}]/u.test(char)) {
      result += char
      lastWasHyphen = false
    } else if (!lastWasHyphen && result.length > 0) {
      result += '-'
      lastWasHyphen = true
    }
  }
  return result.replace(/-+$/, '')
}

const englishSlug = /^[a-z0-9]+(-[a-z0-9]+)*$/

/**
 * Null when fine, otherwise the message to show. The slug the admin typed
 * is used, or - if empty - one made from the name. It must end up in plain
 * English letters: the database column can't store "কক্সবাজার".
 */
export function slugProblem(slug: string, name: string, maxLength: number): string | null {
  const source = slug.trim() || name.trim()
  if (!source) return null // the empty name gets its own "required" message
  const generated = slugify(source)
  return generated.length <= maxLength && englishSlug.test(generated)
    ? null
    : `Enter a URL name in English letters, numbers and hyphens (max ${maxLength}), e.g. coxs-bazar.`
}
