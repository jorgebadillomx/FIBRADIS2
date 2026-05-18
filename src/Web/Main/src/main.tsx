import { StrictMode } from 'react'
import { createRoot, hydrateRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider, hydrate } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { router } from './app/router'
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
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>
)

if (rootEl.hasChildNodes()) {
  hydrateRoot(rootEl, app)
} else {
  createRoot(rootEl).render(app)
}
