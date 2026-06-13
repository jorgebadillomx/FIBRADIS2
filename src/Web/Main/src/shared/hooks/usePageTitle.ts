import { useEffect } from 'react'

function setOrCreateMeta(type: 'name' | 'property', key: string, content: string): void {
  let el = document.querySelector<HTMLMetaElement>(`meta[${type}="${key}"]`)
  if (!el) {
    el = document.createElement('meta')
    el.setAttribute(type, key)
    document.head.appendChild(el)
  }
  el.content = content
}

function setCanonical(href: string): void {
  let el = document.querySelector<HTMLLinkElement>('link[rel="canonical"]')
  if (!el) {
    el = document.createElement('link')
    el.rel = 'canonical'
    document.head.appendChild(el)
  }
  el.href = href
}

export function usePageTitle(title: string, description?: string): void {
  useEffect(() => {
    const url = `${window.location.origin}${window.location.pathname}`

    document.title = title
    setOrCreateMeta('property', 'og:title', title)
    setOrCreateMeta('name', 'twitter:title', title)
    setCanonical(url)
    setOrCreateMeta('property', 'og:url', url)

    if (description) {
      setOrCreateMeta('name', 'description', description)
      setOrCreateMeta('property', 'og:description', description)
      setOrCreateMeta('name', 'twitter:description', description)
    }
  }, [title, description])
}
