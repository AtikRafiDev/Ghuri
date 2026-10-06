/**
 * Hands a downloaded file to the browser as a normal "save" - for files the
 * API only gives with a login token (a plain <a href> can't send one).
 */
export function saveFile(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  // Give the browser a moment to start the download before freeing the memory.
  setTimeout(() => URL.revokeObjectURL(url), 10_000)
}
