import { lazy, StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { OpsLoginGate } from '@/components/OpsLoginGate'
import { OpsShell } from '@/components/OpsShell'
import './index.css'

const AiConfigPage = lazy(() => import('@/pages/AiConfigPage').then((m) => ({ default: m.AiConfigPage })))
const AiPromptsPage = lazy(() => import('@/pages/AiPromptsPage').then((m) => ({ default: m.AiPromptsPage })))
const BlocklistPage = lazy(() => import('@/pages/BlocklistPage').then((m) => ({ default: m.BlocklistPage })))
const CatalogPage = lazy(() => import('@/pages/CatalogPage').then((m) => ({ default: m.CatalogPage })))
const ConfigPage = lazy(() => import('@/pages/ConfigPage').then((m) => ({ default: m.ConfigPage })))
const DashboardPage = lazy(() => import('@/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })))
const DistributionsPage = lazy(() => import('@/pages/DistributionsPage').then((m) => ({ default: m.DistributionsPage })))
const EditorialPage = lazy(() => import('@/pages/EditorialPage').then((m) => ({ default: m.EditorialPage })))
const FundamentalsPage = lazy(() => import('@/pages/FundamentalsPage').then((m) => ({ default: m.FundamentalsPage })))
const SeoOrganizationPage = lazy(() => import('@/pages/SeoOrganizationPage').then((m) => ({ default: m.SeoOrganizationPage })))
const SeoFaqPage = lazy(() => import('@/pages/SeoFaqPage').then((m) => ({ default: m.SeoFaqPage })))
const SeoPage = lazy(() => import('@/pages/SeoPage').then((m) => ({ default: m.SeoPage })))
const SeoRedirectsPage = lazy(() => import('@/pages/SeoRedirectsPage').then((m) => ({ default: m.SeoRedirectsPage })))
const NewsBodyPage = lazy(() => import('@/pages/NewsBodyPage').then((m) => ({ default: m.NewsBodyPage })))
const AiCallLogsPage = lazy(() => import('@/pages/AiCallLogsPage').then((m) => ({ default: m.AiCallLogsPage })))
const PipelineLogsPage = lazy(() => import('@/pages/PipelineLogsPage').then((m) => ({ default: m.PipelineLogsPage })))
const UsersPage = lazy(() => import('@/pages/UsersPage').then((m) => ({ default: m.UsersPage })))

const queryClient = new QueryClient()
const basename = import.meta.env.PROD ? '/ops' : '/'
const router = createBrowserRouter([
  {
    path: '/',
    element: <OpsShell />,
    children: [
      { index: true, element: <Navigate replace to="/ai-config" /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'distribuciones', element: <DistributionsPage /> },
      { path: 'catalog', element: <CatalogPage /> },
      { path: 'ai-config', element: <AiConfigPage /> },
      { path: 'editorial', element: <EditorialPage /> },
      { path: 'seo/organization', element: <SeoOrganizationPage /> },
      { path: 'seo/faq', element: <SeoFaqPage /> },
      { path: 'seo/robots', element: <SeoPage /> },
      { path: 'seo/redirects', element: <SeoRedirectsPage /> },
      { path: 'noticias', element: <NewsBodyPage /> },
      { path: 'blocklist', element: <BlocklistPage /> },
      { path: 'pipeline-logs', element: <PipelineLogsPage /> },
      { path: 'ai-call-logs', element: <AiCallLogsPage /> },
      { path: 'ai-prompts', element: <AiPromptsPage /> },
      { path: 'fundamentals', element: <FundamentalsPage /> },
      { path: 'config', element: <ConfigPage /> },
      { path: 'users', element: <UsersPage /> },
      { path: '*', element: <Navigate replace to="/ai-config" /> },
    ],
  },
], { basename })

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <OpsLoginGate>
        <RouterProvider router={router} />
      </OpsLoginGate>
    </QueryClientProvider>
  </StrictMode>,
)
