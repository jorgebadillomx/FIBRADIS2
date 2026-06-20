export type AdSenseAuthStatus = 'checking' | 'anonymous' | 'authenticated'

export const AD_SENSE_SCRIPT_URL =
  'https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-6045003898585028'

export interface AdSenseDocumentLike {
  querySelector(selectors: string): Element | null
  createElement(tagName: 'script'): HTMLScriptElement
  head: Pick<HTMLHeadElement, 'appendChild'>
}

export function isAdSenseEnabled(status: AdSenseAuthStatus): boolean {
  return status !== 'authenticated'
}

export function shouldLoadAdSense(status: AdSenseAuthStatus, hasSessionCookie: boolean): boolean {
  if (status === 'authenticated') return false
  if (status === 'checking' && hasSessionCookie) return false
  return true
}

export function syncAdSenseScript(
  status: AdSenseAuthStatus,
  doc: AdSenseDocumentLike = document,
): void {
  const selector = `script[src="${AD_SENSE_SCRIPT_URL}"]`
  const existingScript = doc.querySelector(selector)

  if (!isAdSenseEnabled(status)) {
    existingScript?.remove()
    return
  }

  if (existingScript) return

  const script = doc.createElement('script')
  script.async = true
  script.crossOrigin = 'anonymous'
  script.src = AD_SENSE_SCRIPT_URL
  doc.head.appendChild(script)
}
