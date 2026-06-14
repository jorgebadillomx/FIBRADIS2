import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { OpsLoginGate } from '@/components/OpsLoginGate'
import { OpsShell } from '@/components/OpsShell'
import { AiConfigPage } from '@/pages/AiConfigPage'
import { AiPromptsPage } from '@/pages/AiPromptsPage'
import { BlocklistPage } from '@/pages/BlocklistPage'
import { CatalogPage } from '@/pages/CatalogPage'
import { ConfigPage } from '@/pages/ConfigPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { DistributionsPage } from '@/pages/DistributionsPage'
import { EditorialPage } from '@/pages/EditorialPage'
import { FundamentalsPage } from '@/pages/FundamentalsPage'
import { SeoOrganizationPage } from '@/pages/SeoOrganizationPage'
import { SeoFaqPage } from '@/pages/SeoFaqPage'
import { SeoRedirectsPage } from '@/pages/SeoRedirectsPage'
import { NewsBodyPage } from '@/pages/NewsBodyPage'
import { AiCallLogsPage } from '@/pages/AiCallLogsPage'
import { PipelineLogsPage } from '@/pages/PipelineLogsPage'
import { UsersPage } from '@/pages/UsersPage'
import './index.css'

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
