import { lazy, Suspense } from 'react'
import type { RouteObject } from 'react-router'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { HomePage } from '@/modules/home/HomePage'
import { ProtectedRoute } from '@/modules/auth/ProtectedRoute'
import { PortafolioRoute } from '@/modules/portafolio/PortafolioRoute'
import { NotFound } from '@/shared/layouts/NotFound'

const FibraPage = lazy(() => import('@/modules/ficha-publica/FibraPage').then(m => ({ default: m.FibraPage })))
const NoticiaPage = lazy(() => import('@/modules/noticia/NoticiaPage').then(m => ({ default: m.NoticiaPage })))
const NoticiasListPage = lazy(() => import('@/modules/noticias/NoticiasListPage').then(m => ({ default: m.NoticiasListPage })))
const FundamentalesPage = lazy(() => import('@/modules/fundamentales/FundamentalesPage').then(m => ({ default: m.FundamentalesPage })))
const CalendarioPage = lazy(() => import('@/modules/calendario/CalendarioPage').then(m => ({ default: m.CalendarioPage })))
const ConoceLasFibrasPage = lazy(() => import('@/modules/conoce-las-fibras/ConoceLasFibrasPage').then(m => ({ default: m.ConoceLasFibrasPage })))
const CatalogoPage = lazy(() => import('@/modules/catalogo/CatalogoPage').then(m => ({ default: m.CatalogoPage })))
const ComparadorPage = lazy(() => import('@/modules/comparador/ComparadorPage').then(m => ({ default: m.ComparadorPage })))
const HerramientasPage = lazy(() => import('@/modules/herramientas/HerramientasPage').then(m => ({ default: m.HerramientasPage })))
const CalculadoraPage = lazy(() => import('@/modules/calculadora/CalculadoraPage').then(m => ({ default: m.CalculadoraPage })))
const OportunidadesPage = lazy(() => import('@/modules/oportunidades/OportunidadesPage').then(m => ({ default: m.OportunidadesPage })))
const LoginPage = lazy(() => import('@/modules/auth/LoginPage').then(m => ({ default: m.LoginPage })))
const PrivacidadPage = lazy(() => import('@/modules/privacidad/PrivacidadPage').then(m => ({ default: m.PrivacidadPage })))
const AcercaPage = lazy(() => import('@/modules/acerca/AcercaPage').then(m => ({ default: m.AcercaPage })))
const ContactoPage = lazy(() => import('@/modules/contacto/ContactoPage').then(m => ({ default: m.ContactoPage })))
const PerfilPage = lazy(() => import('@/modules/perfil/PerfilPage').then(m => ({ default: m.PerfilPage })))
const ReportesPage = lazy(() => import('@/modules/reportes/ReportesPage').then(m => ({ default: m.ReportesPage })))

// Reserva la altura del viewport mientras carga el chunk lazy de la ruta. Con min-h-[40vh] el
// footer quedaba dentro del fold y se desplomaba al renderizar la página (CLS ~0.15 en /fibras/:slug,
// story 12-7). min-h-screen mantiene el footer fuera del fold, así su desplazamiento no cuenta como CLS.
const PageLoader = () => (
  <div className="flex min-h-screen items-start justify-center pt-24">
    <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
  </div>
)

const p = (element: React.ReactElement) => (
  <Suspense fallback={<PageLoader />}>{element}</Suspense>
)

export const routes: RouteObject[] = [
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/fibras', element: p(<CatalogoPage />) },
      { path: '/comparar', element: p(<ComparadorPage />) },
      { path: '/calculadora', element: p(<CalculadoraPage />) },
      { path: '/fibras/:slug', element: p(<FibraPage />) },
      { path: '/noticias', element: p(<NoticiasListPage />) },
      { path: '/calendario', element: p(<CalendarioPage />) },
      { path: '/conoce-las-fibras', element: p(<ConoceLasFibrasPage />) },
      { path: '/noticias/:slug', element: p(<NoticiaPage />) },
      { path: '/fundamentales', element: p(<FundamentalesPage />) },
      { path: '/portafolio', element: <PortafolioRoute /> },
      { path: '/login', element: p(<LoginPage />) },
      { path: '/privacidad', element: p(<PrivacidadPage />) },
      { path: '/acerca', element: p(<AcercaPage />) },
      { path: '/contacto', element: p(<ContactoPage />) },
      {
        element: <ProtectedRoute />,
        children: [
          { path: '/oportunidades', element: p(<OportunidadesPage />) },
          { path: '/herramientas', element: p(<HerramientasPage />) },
          { path: '/perfil', element: p(<PerfilPage />) },
          { path: '/reportes', element: p(<ReportesPage />) },
        ],
      },
      { path: '*', element: <NotFound /> },
    ],
  },
]
