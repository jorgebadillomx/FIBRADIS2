import type { RouteObject } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FibraPage } from '@/modules/ficha-publica/FibraPage'
import { NoticiaPage } from '@/modules/noticia/NoticiaPage'
import { CatalogoPage } from '@/modules/catalogo/CatalogoPage'
import { NotFound } from '@/shared/layouts/NotFound'

export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/catalogo', element: <CatalogoPage /> },
      { path: '/fibras/:ticker', element: <FibraPage /> },
      { path: '/noticias/:id', element: <NoticiaPage /> },
      { path: '*', element: <NotFound /> },
    ],
  },
]
