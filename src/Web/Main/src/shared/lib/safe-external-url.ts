const ALLOWED_PROTOCOLS = new Set(['http:', 'https:'])

export function getSafeExternalUrl(url: string | null | undefined): string | null {
  if (!url) {
    return null
  }

  const trimmedUrl = url.trim()

  if (!trimmedUrl) {
    return null
  }

  try {
    const parsedUrl = new URL(trimmedUrl)
    return ALLOWED_PROTOCOLS.has(parsedUrl.protocol) ? parsedUrl.toString() : null
  } catch {
    return null
  }
}
