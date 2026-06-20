import { useEffect } from 'react'
import { useLocation } from 'react-router'
import { useAuth } from '@/modules/auth/AuthContext'
import { isAdSenseEnabled } from './adsense'

interface AdBannerProps {
  adSlot: string
  adFormat?: string
  style?: React.CSSProperties
}

// Outer component: forces full remount of AdUnit on every route change via key={pathname}.
// This prevents the "All 'ins' elements already have ads" error in SPA navigation.
export function AdBanner(props: AdBannerProps) {
  const { status } = useAuth()
  const { pathname } = useLocation()

  if (!isAdSenseEnabled(status)) return null

  return <AdUnit key={pathname} {...props} />
}

function AdUnit({ adSlot, adFormat = 'auto', style }: AdBannerProps) {
  useEffect(() => {
    try {
      ((window as unknown as { adsbygoogle: unknown[] }).adsbygoogle =
        (window as unknown as { adsbygoogle: unknown[] }).adsbygoogle || []).push({})
    } catch {
      // Script not yet loaded — AdSense retries automatically
    }
  }, [])

  return (
    <ins
      className="adsbygoogle"
      style={{ display: 'block', ...style }}
      data-ad-client="ca-pub-6045003898585028"
      data-ad-slot={adSlot}
      data-ad-format={adFormat}
      data-full-width-responsive="true"
    />
  )
}
