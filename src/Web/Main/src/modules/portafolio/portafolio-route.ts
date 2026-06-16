import type { PublicLayoutStatus } from '@/shared/layouts/public-navigation'

export type PortafolioRouteView = 'loading' | 'landing' | 'dashboard'

export function resolvePortafolioRouteView(status: PublicLayoutStatus): PortafolioRouteView {
  if (status === 'checking') return 'loading'
  if (status === 'authenticated') return 'dashboard'
  return 'landing'
}
