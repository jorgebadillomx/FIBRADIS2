import type { MouseEvent } from 'react'
import { ArrowRight, Briefcase, Calculator, FileText, LayoutGrid, Newspaper, Scale, TrendingUp } from 'lucide-react'
import { Link } from 'react-router'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Button } from '@/shared/ui/button'
import { LoginForm } from '@/modules/auth/LoginForm'

// Los anclas internas (#login) no re-scrollean en re-clic cuando la URL ya contiene
// el hash (el router no gestiona hash). Forzamos el scroll en cada clic.
function handleHashAnchorClick(event: MouseEvent<HTMLAnchorElement>) {
  const href = event.currentTarget.getAttribute('href')
  if (!href?.startsWith('#')) return
  const target = document.getElementById(href.slice(1))
  if (!target) return
  event.preventDefault()
  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  target.scrollIntoView({ behavior: prefersReducedMotion ? 'auto' : 'smooth', block: 'start' })
}

const PAGE_TITLE = 'Portafolio de FIBRAs, reportes y login | FIBRADIS'
const PAGE_DESCRIPTION =
  'Tu entrada pública a Fibras Inmobiliarias: explora portafolio, reportes, oportunidades, herramientas, fundamentales, noticias y catálogo, o inicia sesión.'

type CapabilityCard = {
  title: string
  description: string
  href: string
  cta: string
  icon: typeof Briefcase
  privateArea?: boolean
}

const CAPABILITY_CARDS: CapabilityCard[] = [
  {
    title: 'Portafolio',
    description: 'KPIs, calendario de distribuciones y posición consolidada para dar seguimiento a tu inversión.',
    href: '#login',
    cta: 'Iniciar sesión',
    icon: Briefcase,
    privateArea: true,
  },
  {
    title: 'Reportes trimestrales',
    description: 'Fundamentales + análisis IA por FIBRA, con lectura privada y contexto para cada trimestre.',
    href: '#login',
    cta: 'Iniciar sesión',
    icon: FileText,
    privateArea: true,
  },
  {
    title: 'Oportunidades y ranking',
    description: 'Score configurable para comparar oportunidades y detectar FIBRAs con mayor potencial relativo.',
    href: '#login',
    cta: 'Iniciar sesión',
    icon: TrendingUp,
    privateArea: true,
  },
  {
    title: 'Herramientas y calculadora',
    description: 'Calculadora pública para escenarios rápidos y hub de herramientas privadas para profundizar.',
    href: '/calculadora',
    cta: 'Abrir calculadora',
    icon: Calculator,
  },
  {
    title: 'Fundamentales comparativos',
    description: 'Cap Rate, NAV, LTV, NOI Margin y FFO Margin para comparar FIBRAs lado a lado.',
    href: '/fundamentales',
    cta: 'Explorar',
    icon: Scale,
  },
  {
    title: 'Noticias',
    description: 'Cobertura y análisis del mercado con noticias recientes, contexto y seguimiento editorial.',
    href: '/noticias',
    cta: 'Leer noticias',
    icon: Newspaper,
  },
  {
    title: 'Catálogo de FIBRAs',
    description: 'Universo completo de FIBRAs inmobiliarias con búsqueda, fichas públicas y navegación rápida.',
    href: '/fibras',
    cta: 'Ver catálogo',
    icon: LayoutGrid,
  },
]

const PUBLIC_LINKS = [
  { label: 'Catálogo', href: '/fibras' },
  { label: 'Fundamentales', href: '/fundamentales' },
  { label: 'Noticias', href: '/noticias' },
  { label: 'Calculadora', href: '/calculadora' },
] as const

export function PortafolioLanding() {
  usePageTitle(PAGE_TITLE, PAGE_DESCRIPTION, { canonicalPath: '/portafolio' })

  return (
    <div className="relative overflow-hidden bg-[radial-gradient(circle_at_top_left,rgba(194,65,12,0.14),transparent_28%),radial-gradient(circle_at_top_right,rgba(15,118,110,0.12),transparent_24%),linear-gradient(180deg,rgba(10,14,26,0.03),transparent_24%)]">
      <div className="container mx-auto max-w-7xl px-4 py-10 md:py-12">
        <section className="grid gap-8 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)] lg:items-start">
          <div className="space-y-6">
            <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary shadow-sm">
              Portafolio público
            </div>

            <div className="space-y-4">
              <h1 className="max-w-3xl font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
                Todo lo que hace Fibras Inmobiliarias, en una sola puerta de entrada.
              </h1>
              <p className="max-w-3xl text-base leading-7 text-muted-foreground md:text-lg">
                Consulta el catálogo público, fundamentales, noticias y calculadora. Cuando inicias sesión,
                el mismo enlace te lleva al portafolio privado con KPIs, calendario de distribuciones,
                oportunidades, herramientas y reportes trimestrales.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button asChild size="lg">
                <Link to="/fibras">
                  Ver catálogo
                  <ArrowRight className="size-4" />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline">
                <a href="#login" onClick={handleHashAnchorClick}>Iniciar sesión</a>
              </Button>
            </div>

            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              {PUBLIC_LINKS.map((link) => (
                <Link
                  key={link.href}
                  to={link.href}
                  className="group rounded-2xl border border-border bg-background/80 p-4 text-sm text-foreground shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background motion-reduce:transition-none motion-reduce:hover:translate-y-0"
                >
                  <p className="font-semibold">{link.label}</p>
                  <p className="mt-1 text-xs leading-5 text-muted-foreground">
                    Abrir sección pública
                  </p>
                </Link>
              ))}
            </div>
          </div>

          <aside className="rounded-3xl border border-border bg-surface-elevated/95 p-6 shadow-sm backdrop-blur">
            <div className="space-y-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                Resumen
              </p>
              <ul className="space-y-3 text-sm leading-6 text-foreground">
                <li>Una URL indexable para descubrir la plataforma desde Google.</li>
                <li>Login embebido para entrar al dashboard sin cambiar de experiencia.</li>
                <li>Acceso a reportes privados, oportunidades y herramientas tras autenticación.</li>
              </ul>
            </div>
          </aside>
        </section>

        <section className="mt-10">
          <div className="max-w-3xl space-y-3">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
              Todo lo que hace
            </p>
            <h2 className="font-playfair text-3xl font-semibold tracking-tight text-foreground">
              Capacidades principales
            </h2>
            <p className="text-sm leading-6 text-muted-foreground md:text-base">
              La landing describe la plataforma con texto indexable y deja claro qué partes son públicas y
              cuáles aparecen después de iniciar sesión.
            </p>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {CAPABILITY_CARDS.map((card) => {
              const Icon = card.icon
              const isAnchor = card.href.startsWith('#')

              return (
                <article
                  key={card.title}
                  className="group rounded-3xl border border-border bg-background/80 p-5 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md motion-reduce:transition-none motion-reduce:hover:translate-y-0"
                >
                  <div className="flex items-start gap-4">
                    <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl border border-border bg-surface-elevated text-primary">
                      <Icon className="size-5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-playfair text-xl font-semibold text-foreground">
                          {card.title}
                        </h3>
                        {card.privateArea ? (
                          <span className="rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.22em] text-amber-700">
                            Privado
                          </span>
                        ) : null}
                      </div>
                      <p className="mt-2 text-sm leading-6 text-muted-foreground">
                        {card.description}
                      </p>
                    </div>
                  </div>

                  <div className="mt-5">
                    <Button asChild variant="outline" size="lg" className="w-full sm:w-auto">
                      {isAnchor ? (
                        <a href={card.href} onClick={handleHashAnchorClick}>{card.cta}</a>
                      ) : (
                        <Link to={card.href}>{card.cta}</Link>
                      )}
                    </Button>
                  </div>
                </article>
              )
            })}
          </div>
        </section>

        <section id="login" className="mt-10 scroll-mt-24">
          <div className="grid gap-6 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)] lg:items-start">
            <div className="space-y-4 rounded-3xl border border-border bg-background/80 p-6 shadow-sm">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                Acceso privado
              </p>
              <h2 className="font-playfair text-3xl font-semibold tracking-tight text-foreground">
                Entra a tu portafolio sin perder contexto
              </h2>
              <p className="text-sm leading-6 text-muted-foreground md:text-base">
                El formulario reutiliza la misma lógica de sesión que la página de login. Cuando entras aquí,
                el mismo enlace vuelve al dashboard privado sin recargar la página.
              </p>
              <div className="rounded-2xl border border-dashed border-border bg-surface-elevated/70 p-4 text-sm leading-6 text-muted-foreground">
                Si solo quieres explorar la plataforma, usa las secciones públicas de arriba. Si ya tienes
                cuenta, inicia sesión y verás el portafolio privado con las métricas calculadas en backend.
              </div>
            </div>

            <LoginForm redirectTo="/portafolio" titleAs="h2" className="shadow-md" />
          </div>
        </section>
      </div>
    </div>
  )
}
