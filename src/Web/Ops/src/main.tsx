import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { OpsLoginGate } from '@/components/OpsLoginGate'
import { OpsShell } from '@/components/OpsShell'
import { AiConfigPage } from '@/pages/AiConfigPage'
import { AiPromptsPage } from '@/pages/AiPromptsPage'
import { BlocklistPage } from '@/pages/BlocklistPage'
import { DashboardPage } from '@/pages/DashboardPage'
import { FundamentalsPage } from '@/pages/FundamentalsPage'
import { NewsBodyPage } from '@/pages/NewsBodyPage'
import { PipelineLogsPage } from '@/pages/PipelineLogsPage'
import './index.css'

const queryClient = new QueryClient()
const router = createBrowserRouter([
  {
    path: '/',
    element: <OpsShell />,
    children: [
      { index: true, element: <Navigate replace to="/ai-config" /> },
      { path: 'dashboard', element: <DashboardPage /> },
      { path: 'ai-config', element: <AiConfigPage /> },
      { path: 'noticias', element: <NewsBodyPage /> },
      { path: 'blocklist', element: <BlocklistPage /> },
      { path: 'pipeline-logs', element: <PipelineLogsPage /> },
      { path: 'ai-prompts', element: <AiPromptsPage /> },
      { path: 'fundamentals', element: <FundamentalsPage /> },
      { path: '*', element: <Navigate replace to="/ai-config" /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <OpsLoginGate>
        <RouterProvider router={router} />
      </OpsLoginGate>
    </QueryClientProvider>
  </StrictMode>,
)
