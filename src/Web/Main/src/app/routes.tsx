import type { RouteObject } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { FibraPage } from '@/modules/ficha-publica/FibraPage'
import { NoticiaPage } from '@/modules/noticia/NoticiaPage'
import { NoticiasListPage } from '@/modules/noticias/NoticiasListPage'
import { FundamentalesPage } from '@/modules/fundamentales/FundamentalesPage'
import { CalendarioPage } from '@/modules/calendario/CalendarioPage'
import { ConoceLasFibrasPage } from '@/modules/conoce-las-fibras/ConoceLasFibrasPage'
import { CatalogoPage } from '@/modules/catalogo/CatalogoPage'
import { ComparadorPage } from '@/modules/comparador/ComparadorPage'
import { HerramientasPage } from '@/modules/herramientas/HerramientasPage'
import { CalculadoraPage } from '@/modules/calculadora/CalculadoraPage'
import { PortafolioPage } from '@/modules/portafolio/PortafolioPage'
import { OportunidadesPage } from '@/modules/oportunidades/OportunidadesPage'
import { LoginPage } from '@/modules/auth/LoginPage'
import { ProtectedRoute } from '@/modules/auth/ProtectedRoute'
import { NotFound } from '@/shared/layouts/NotFound'
import { PrivacidadPage } from '@/modules/privacidad/PrivacidadPage'
import { PerfilPage } from '@/modules/perfil/PerfilPage'

export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/catalogo', element: <CatalogoPage /> },
      { path: '/comparar', element: <ComparadorPage /> },
      { path: '/herramientas', element: <HerramientasPage /> },
      { path: '/calculadora', element: <CalculadoraPage /> },
      { path: '/fibras/:slug', element: <FibraPage /> },
      { path: '/noticias', element: <NoticiasListPage /> },
      { path: '/calendario', element: <CalendarioPage /> },
      { path: '/conoce-las-fibras', element: <ConoceLasFibrasPage /> },
      { path: '/noticias/:slug', element: <NoticiaPage /> },
      { path: '/fundamentales', element: <FundamentalesPage /> },
      { path: '/login', element: <LoginPage /> },
      { path: '/privacidad', element: <PrivacidadPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: '/portafolio', element: <PortafolioPage /> },
          { path: '/oportunidades', element: <OportunidadesPage /> },
          { path: '/perfil', element: <PerfilPage /> },
        ],
      },
      { path: '*', element: <NotFound /> },
    ],
  },
]
