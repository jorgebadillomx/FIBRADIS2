import type { RouteObject } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FibraPage } from '@/modules/ficha-publica/FibraPage'
import { NoticiaPage } from '@/modules/noticia/NoticiaPage'
import { NoticiasListPage } from '@/modules/noticias/NoticiasListPage'
import { FundamentalesPage } from '@/modules/fundamentales/FundamentalesPage'
import { ConoceLasFibrasPage } from '@/modules/conoce-las-fibras/ConoceLasFibrasPage'
import { CatalogoPage } from '@/modules/catalogo/CatalogoPage'
import { PortafolioPage } from '@/modules/portafolio/PortafolioPage'
import { NotFound } from '@/shared/layouts/NotFound'

export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/catalogo', element: <CatalogoPage /> },
      { path: '/fibras/:ticker', element: <FibraPage /> },
      { path: '/noticias', element: <NoticiasListPage /> },
      { path: '/conoce-las-fibras', element: <ConoceLasFibrasPage /> },
      { path: '/noticias/:id', element: <NoticiaPage /> },
      { path: '/fundamentales', element: <FundamentalesPage /> },
      { path: '/portafolio', element: <PortafolioPage /> },
      { path: '*', element: <NotFound /> },
    ],
  },
]
