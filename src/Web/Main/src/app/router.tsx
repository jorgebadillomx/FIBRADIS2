import { createBrowserRouter } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FichaPlaceholder } from '@/modules/ficha-publica/FichaPlaceholder'
import { NotFound } from '@/shared/layouts/NotFound'

export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/fibras/:ticker', element: <FichaPlaceholder /> },
      { path: '*', element: <NotFound /> },
    ],
  },
])
