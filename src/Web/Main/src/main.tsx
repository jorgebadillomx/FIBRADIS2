import { StrictMode } from 'react'
import { createRoot, hydrateRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider, hydrate } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { router } from './app/router'
import { AuthProvider } from './modules/auth/AuthContext'
import './index.css'

declare global {
  interface Window {
    __QUERY_INITIAL_DATA__?: unknown
  }
}

const queryClient = new QueryClient()

if (window.__QUERY_INITIAL_DATA__) {
  hydrate(queryClient, window.__QUERY_INITIAL_DATA__)
}

const rootEl = document.getElementById('root')!

const app = (
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>
)

if (rootEl.hasChildNodes()) {
  hydrateRoot(rootEl, app)
} else {
  createRoot(rootEl).render(app)
}
