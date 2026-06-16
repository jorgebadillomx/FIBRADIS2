import { Suspense, lazy } from 'react'
import { useAuth } from '@/modules/auth/AuthContext'
import { resolvePortafolioRouteView } from './portafolio-route'

const PortafolioPage = lazy(() => import('@/modules/portafolio/PortafolioPage').then((module) => ({ default: module.PortafolioPage })))
const PortafolioLanding = lazy(() => import('@/modules/portafolio/PortafolioLanding').then((module) => ({ default: module.PortafolioLanding })))

function PortafolioRouteLoader() {
  return (
    <div className="container mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-7xl items-start px-4 py-10">
      <div className="w-full rounded-3xl border border-border bg-card p-6 shadow-sm">
        <div className="h-3 w-28 animate-pulse rounded-full bg-muted/70" />
        <div className="mt-4 h-8 w-72 max-w-full animate-pulse rounded-2xl bg-muted/70" />
        <div className="mt-3 h-4 w-full max-w-2xl animate-pulse rounded-full bg-muted/70" />
        <div className="mt-8 grid gap-4 md:grid-cols-2">
          <div className="h-36 animate-pulse rounded-2xl bg-muted/60" />
          <div className="h-36 animate-pulse rounded-2xl bg-muted/60" />
        </div>
      </div>
    </div>
  )
}

export function PortafolioRoute() {
  const { status } = useAuth()
  const view = resolvePortafolioRouteView(status)

  if (view === 'loading') {
    return <PortafolioRouteLoader />
  }

  return (
    <Suspense fallback={<PortafolioRouteLoader />}>
      {view === 'dashboard' ? <PortafolioPage /> : <PortafolioLanding />}
    </Suspense>
  )
}
