import { useEffect } from 'react'
import { injectClarityScript } from './clarity'

export function ClarityLoader() {
  useEffect(() => {
    injectClarityScript()
  }, [])

  return null
}
