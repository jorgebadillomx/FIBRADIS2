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

function removeMeta(type: 'name' | 'property', key: string): void {
  document.querySelector<HTMLMetaElement>(`meta[${type}="${key}"]`)?.remove()
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

export interface PageTitleOptions {
  canonicalPath?: string
  robotsDirectives?: string | null
}

export function usePageTitle(title: string, description?: string, options?: PageTitleOptions): void {
  useEffect(() => {
    const canonicalPath = options?.canonicalPath ?? window.location.pathname
    const url = `${window.location.origin}${canonicalPath}`

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

    if (options?.robotsDirectives) {
      setOrCreateMeta('name', 'robots', options.robotsDirectives)
    } else {
      removeMeta('name', 'robots')
    }
  }, [title, description, options?.canonicalPath, options?.robotsDirectives])
}
