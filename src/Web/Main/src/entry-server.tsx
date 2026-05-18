import { createStaticHandler, createStaticRouter, StaticRouterProvider } from 'react-router'
import { renderToString } from 'react-dom/server'
import { QueryClient, QueryClientProvider, dehydrate } from '@tanstack/react-query'
import { routes } from './app/routes'

export async function render(url: string, initialData: Record<string, unknown> = {}) {
  const handler = createStaticHandler(routes)
  const request = new Request(`http://prerender.local${url}`)
  const context = await handler.query(request)

  if (context instanceof Response) throw context

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  Object.entries(initialData).forEach(([key, data]) => {
    queryClient.setQueryData(JSON.parse(key), data)
  })

  const router = createStaticRouter(handler.dataRoutes, context)

  const html = renderToString(
    <QueryClientProvider client={queryClient}>
      <StaticRouterProvider router={router} context={context} />
    </QueryClientProvider>
  )

  return { html, dehydratedState: dehydrate(queryClient) }
}
