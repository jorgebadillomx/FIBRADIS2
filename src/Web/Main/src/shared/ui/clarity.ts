export const CLARITY_SCRIPT_URL = 'https://www.clarity.ms/tag/hd9ip85air'

export interface ClarityDocumentLike {
  querySelector(selectors: string): Element | null
  createElement(tagName: 'script'): HTMLScriptElement
  head: Pick<HTMLHeadElement, 'appendChild'>
}

export function injectClarityScript(doc: ClarityDocumentLike = document): void {
  const selector = `script[src="${CLARITY_SCRIPT_URL}"]`
  if (doc.querySelector(selector)) return

  const script = doc.createElement('script')
  script.async = true
  script.src = CLARITY_SCRIPT_URL
  doc.head.appendChild(script)
}
