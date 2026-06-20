import { useEffect } from 'react'
import { useAuth } from '@/modules/auth/AuthContext'
import { hasSessionCookie } from '@/modules/auth/mainAuth'
import { shouldLoadAdSense, syncAdSenseScript } from './adsense'

export function AdSenseLoader() {
  const { status } = useAuth()

  useEffect(() => {
    if (!shouldLoadAdSense(status, hasSessionCookie())) {
      if (status === 'authenticated') {
        syncAdSenseScript(status)
      }
      return
    }

    syncAdSenseScript(status)
  }, [status])

  return null
}
