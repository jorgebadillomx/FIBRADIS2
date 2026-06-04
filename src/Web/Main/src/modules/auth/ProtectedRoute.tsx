import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './AuthContext'

export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'checking') {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="rounded-2xl border border-border bg-card p-6 shadow-sm">
          <div className="h-4 w-32 animate-pulse rounded bg-muted" />
          <div className="mt-4 h-8 w-56 animate-pulse rounded bg-muted/80" />
        </div>
      </div>
    )
  }

  if (status === 'anonymous') {
    return (
      <Navigate
        to={`/login?redirect=${encodeURIComponent(location.pathname)}`}
        replace
      />
    )
  }

  return <Outlet />
}
