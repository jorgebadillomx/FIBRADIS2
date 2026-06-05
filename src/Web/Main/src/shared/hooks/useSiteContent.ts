import { useQuery } from '@tanstack/react-query'

interface SiteContent {
  termsEnabled: boolean
  termsText: string | null
  contactEmail: string | null
}

async function fetchSiteContent(): Promise<SiteContent> {
  const res = await fetch('/api/v1/site-content')
  if (!res.ok) return { termsEnabled: false, termsText: null, contactEmail: null }
  return res.json() as Promise<SiteContent>
}

export function useSiteContent() {
  return useQuery({
    queryKey: ['site-content'],
    queryFn: fetchSiteContent,
    staleTime: 5 * 60 * 1000,
    retry: false,
  })
}
